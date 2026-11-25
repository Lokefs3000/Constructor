using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Threading;
using Primary.Timing;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class ProfilerWindow : IEditorWindow
    {
        private readonly ArrayPool<CapturedTimestamp> _timestampPool;

        private Vector256<double>[] _measurements;
        private double[] _frameTimes;
        private int _measurementsHead;

        private double _smoothMaxFrametime;

        private CapturedFrame _frame;

        internal ProfilerWindow()
        {
            _timestampPool = ArrayPool<CapturedTimestamp>.Create();

            _measurements = new Vector256<double>[ProfilingManager.Instance.HistorySize];
            _frameTimes = new double[ProfilingManager.Instance.HistorySize];
            _measurementsHead = 0;

            _frame = default;

            Array.Fill(_measurements, Vector256<double>.Zero);
        }

        public unsafe bool UpdateAndRender()
        {
            TakeProfilingSnapshot(ref _frame);

            bool isWindowOpen = true;
            if (ImGui.Begin("Profiler"u8, ref isWindowOpen))
            {
                ImGuiContextPtr context = EditorManager.Instance.ContextManager.ImGuiCtx;
                ImGuiWindowPtr window = context.CurrentWindow;

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                if (ImGui.BeginTable("##"u8, 2, ImGuiTableFlags.BordersInner))
                {
                    ImGui.TableSetupColumn((byte*)null, ImGuiTableColumnFlags.WidthFixed, Math.Min(ImGui.GetContentRegionAvail().X, 150.0f));
                    ImGui.TableNextRow();

                    // Graph
                    {
                        GetAveragedMeasurements(out Vector256<double> measurements, out double maxFrametime);

                        if (_smoothMaxFrametime < maxFrametime)
                            _smoothMaxFrametime = maxFrametime;
                        else
                            _smoothMaxFrametime = double.Lerp(_smoothMaxFrametime, maxFrametime + 1 / 60.0, Time.DeltaTimeDouble);

                        if (ImGui.TableNextColumn())
                        {
                            DrawSideItem(drawList, "Render"u8, measurements[0]);
                            DrawSideItem(drawList, "GfxPlayback"u8, measurements[1]);
                        }

                        if (ImGui.TableNextColumn())
                        {
                            const int NumMarkersTracked = 2;

                            Vector2 availSpace = ImGui.GetContentRegionAvail();
                            availSpace.Y = 150.0f;

                            Vector2 min = new Vector2(window.DC.CursorPos.X, window.DC.CursorPos.Y + availSpace.Y);
                            Vector2 max = new Vector2(window.DC.CursorPos.X + availSpace.X, window.DC.CursorPos.Y);

                            Vector128<float> minY128 = Vector128.Create(availSpace.Y);
                            Vector128<float> maxY128 = Vector128.Create(0.0f);

                            float invLength = 1.0f / (_measurements.Length - 1);
                            double invFrametime = 1.0 / _smoothMaxFrametime;

                            using RentedArray<Vector2> points = new RentedArray<Vector2>(NumMarkersTracked * (_measurements.Length + 2));
                            for (int i = 0; i < _measurements.Length; ++i)
                            {
                                Vector256<double> frameMeasurement = _measurements[(i + _measurementsHead) % _measurements.Length] * invFrametime;

                                float valueX = float.Lerp(min.X, max.X, i * invLength);
                                float valueY = 0.0f;

                                Vector128<float> valuesY = Vector128.Lerp(minY128, maxY128, Avx.ConvertToVector128Single(frameMeasurement));

                                for (int j = 0; j < NumMarkersTracked; ++j)
                                {
                                    double timeValue = frameMeasurement[j];

                                    points[i + j * (_measurements.Length + 2)] = new Vector2(valueX, valueY + valuesY[j] + window.DC.CursorPos.Y);
                                    valueY -= availSpace.Y - valuesY[j];
                                }
                            }

                            for (int i = 0; i < NumMarkersTracked; ++i)
                            {
                                points[i * (_measurements.Length + 2) + _measurements.Length] = min.WithElement(0, max.X);
                                points[i * (_measurements.Length + 2) + _measurements.Length + 1] = min;
                            }

                            drawList.AddConcavePolyFilled(ref points[1 * (_measurements.Length + 2)], _measurements.Length + 2, GetMarkerColor("GfxPlayback"u8));
                            drawList.AddConcavePolyFilled(ref points[0 * (_measurements.Length + 2)], _measurements.Length + 2, GetMarkerColor("Render"u8));

                            GCProfiler gc = ProfilingManager.Instance.GCProfiler;
                            foreach (TrackedGCMarker marker in gc.Markers)
                            {
                                int frameDiff = Time.GetFrameDifference(marker.FrameIndex, Time.FrameIndex);
                                if (frameDiff >= _measurements.Length)
                                    continue;

                                Vector2 pos = window.DC.CursorPos;
                                pos.X += float.Lerp(availSpace.X, 0.0f, frameDiff * invLength);
                                pos.Y += 10.0f;

                                Vector2 v0 = pos;
                                Vector2 v1 = pos; v1.X -= 4.0f; v1.Y += 4.0f;
                                Vector2 v2 = pos; v2.Y += 8.0f;
                                Vector2 v3 = pos; v3.X += 4.0f; v3.Y += 4.0f;

                                uint markerColor = marker.Generation switch
                                {
                                    0 => 0xff00ffff,
                                    1 => 0xff0080ff,
                                    2 => 0xff0000ff,
                                    _ => 0xffff00ff
                                };

                                drawList.AddQuadFilled(v0, v1, v2, v3, markerColor);
                            }

                            ImGui.Dummy(availSpace);
                        }
                    }

                    ImGui.EndTable();
                }
            }
            ImGui.End();

            return isWindowOpen;
        }

        private void TakeProfilingSnapshot(ref CapturedFrame frame)
        {
            if (frame.Threads == null)
            {
                frame = new CapturedFrame(
                    new Dictionary<int, CapturedThread>());
            }
            else
            {
                foreach (var (_, thread) in frame.Threads)
                {
                    _timestampPool.Return(thread.Timestamps, true);
                }

                frame.Threads.Clear();
            }

            Vector256<double> measurementValue = Vector256.Create(-1.0);

            foreach (var (threadId, collections) in ProfilingManager.Instance.Timestamps)
            {
                CapturedTimestamp[] timestamps = _timestampPool.Rent(collections.Timestamps.Count);
                
                for (int i = 0; i < collections.Timestamps.Count; ++i)
                {
                    ProfilingTimestamp timestampToCapture = collections.Timestamps[i];

                    double startTime = (timestampToCapture.StartTimestamp - collections.StartTimestamp) / (double)Stopwatch.Frequency;
                    double endTime = (timestampToCapture.EndTimestamp - collections.StartTimestamp) / (double)Stopwatch.Frequency;

                    timestamps[i] = new CapturedTimestamp(
                        timestampToCapture.Name,
                        timestampToCapture.Stacktrace,
                        timestampToCapture.Allocated,
                        timestampToCapture.Depth,
                        startTime,
                        endTime);

                    if (collections.ThreadId == ThreadHelper.MainThreadId)
                    {
                        if (measurementValue[0] == -1.0 && timestampToCapture.Name.Equals("Render", StringComparison.Ordinal))
                        {
                            measurementValue = measurementValue.WithElement(0, endTime - startTime);
                        }
                    }
                    else if (measurementValue[1] == -1.0 && timestampToCapture.Name.Equals("ExecutePlayback", StringComparison.Ordinal))
                    {
                        measurementValue = measurementValue.WithElement(1, endTime - startTime);
                    }
                }

                Debug.Assert(!frame.Threads.ContainsKey(threadId));
                frame.Threads[threadId] = new CapturedThread(collections.ThreadName, timestamps, collections.Timestamps.Count);
            }

            _frameTimes[_measurementsHead] = Time.DeltaTimeDouble;
            _measurements[_measurementsHead] = Vector256.Max(Vector256<double>.Zero, measurementValue);

            _measurementsHead = (_measurementsHead + 1) % _measurements.Length;
        }

        private void GetAveragedMeasurements(out Vector256<double> averaged, out double maxFrametime)
        {
            averaged = Vector256<double>.Zero;
            foreach (Vector256<double> value in _measurements)
            {
                averaged += value;
            }

            averaged /= _measurements.Length;

            maxFrametime = 0.0;

            int achievableWithSimd = _frameTimes.Length / Vector256<double>.Count;
            if (achievableWithSimd > 1)
            {
                Vector256<double> firstItem = Vector256.LoadUnsafe(ref _frameTimes[0]);
                for (int i = 1; i < achievableWithSimd; ++i)
                {
                    Vector256<double> currentItem = Vector256.LoadUnsafe(ref _frameTimes[i * Vector256<double>.Count]);
                    firstItem = Vector256.Max(firstItem, currentItem);
                }

                maxFrametime = Math.Max(
                    Math.Max(firstItem[0], firstItem[1]),
                    Math.Max(firstItem[2], firstItem[3]));
            }

            for (int i = achievableWithSimd * Vector256<double>.Count; i < _frameTimes.Length; ++i)
            {
                maxFrametime = Math.Max(maxFrametime, _frameTimes[i]);
            }
        }

        /////////////////////// WIDGETS

        private static unsafe void DrawSideItem(ImDrawListPtr drawList, ReadOnlySpan<byte> markerName, double timeValue)
        {
            ImGuiContextPtr context = EditorManager.Instance.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            Boundaries colorRect = new Boundaries(
                window.DC.CursorPos + new Vector2(context.Style.FramePadding.Y),
                window.DC.CursorPos + new Vector2(context.FontSize - context.Style.FramePadding.Y));

            Span<byte> timeValueFormat = stackalloc byte[18];
            (timeValue * 1000.0).TryFormat(timeValueFormat[..^4], out int bytesWritten, "F2");
            timeValueFormat[bytesWritten] = (byte)'m';
            timeValueFormat[bytesWritten + 1] = (byte)'s';
            timeValueFormat[bytesWritten + 2] = (byte)'\0';

            uint markerColor = GetMarkerColor(markerName);

            Vector2 textSize = ImGui.CalcTextSize(markerName);
            Vector2 timeValueTextSize = ImGui.CalcTextSize(timeValueFormat);

            float maxCursorX = ImGui.GetContentRegionAvail().X + window.DC.CursorPos.X;

            drawList.AddRectFilled(colorRect.Minimum, colorRect.Maximum, 0xff000000);
            drawList.AddRectFilled(colorRect.Minimum + Vector2.One, colorRect.Maximum - Vector2.One, markerColor);

            ImGuiP.RenderTextClipped(
                window.DC.CursorPos.WithElement(0, window.DC.CursorPos.X + context.FontSize),
                new Vector2(maxCursorX - timeValueTextSize.X - context.Style.FramePadding.X, window.DC.CursorPos.Y + context.FontSize),
                markerName,
                (byte*)null,
                (Vector2*)null);

            drawList.AddText(window.DC.CursorPos.WithElement(0, maxCursorX - timeValueTextSize.X), ImGui.GetColorU32(ImGuiCol.Text), timeValueFormat);

            ImGuiP.ItemSize(new ImRect(
                window.DC.CursorPos,
                new Vector2(maxCursorX, window.DC.CursorPos.Y + context.FontSize)),
                context.FontSize);

            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text(markerName);
                ImGui.EndTooltip();
            }
        }

        private static uint GetMarkerColor(ReadOnlySpan<byte> markerName) => (uint)markerName.GetDjb2HashCode() | 0xff000000;

        private readonly record struct CapturedFrame(Dictionary<int, CapturedThread> Threads);
        private readonly record struct CapturedThread(string ThreadName, CapturedTimestamp[] Timestamps, int ActualLength);
        private readonly record struct CapturedTimestamp(string Name, string? Stacktrace, int BytesAllocated, int Depth, double StartTime, double EndTime);
    }
}
