using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Microsoft.Extensions.ObjectPool;
using Primary.Common;
using Primary.Profiling;
using Primary.Timing;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Editor.DearImGui
{
    internal unsafe sealed class ProfilerView : IDisposable
    {
        private bool _isPaused;
        private double _slowAveraged;

        private Vector2 _offsetX;
        private float _zoomScale;

        private ObjectPool<PooledList<ProfilingTimestamp>> _profilingTimestampPool;

        private CapturedProfilingTimestamp[] _timestamps;
        private int _head;

        private bool _disposedValue;

        internal ProfilerView()
        {
            _slowAveraged = -1.0;
            _isPaused = false;

            _offsetX = Vector2.Zero;
            _zoomScale = 1.0f;

            _profilingTimestampPool = ObjectPool.Create(new ListProfilingTimestampPolicy());

            _timestamps = new CapturedProfilingTimestamp[30];
            _head = 0;

            for (int i = 0; i < _timestamps.Length; i++)
            {
                _timestamps[i] = new CapturedProfilingTimestamp();
            }
        }

        internal void Render()
        {
            if (!_isPaused)
            {
                CaptureTimestampsForLastFrame();
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            bool beginWindow = ImGui.Begin("Profiler", ImGuiWindowFlags.MenuBar);

            ImGui.PopStyleVar();

            if (beginWindow)
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.MenuItem("Paused", _isPaused))
                        _isPaused = !_isPaused;
                    if (ImGui.MenuItem("Include stacktrace", ProfilingManager.IncludeStacktrace))
                        ProfilingManager.IncludeStacktrace = !ProfilingManager.IncludeStacktrace;
                    ImGui.EndMenuBar();
                }

                Vector2 screenCursor = ImGui.GetCursorScreenPos();
                Vector2 windowPosition = ImGui.GetCursorScreenPos();
                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                {
                    int segments = (int)MathF.Ceiling(contentAvail.Y / 48.0f);
                    float y = 0.0f;

                    for (int i = 0; i < segments; i++)
                    {
                        Vector2 cursor = new Vector2(screenCursor.X, screenCursor.Y - y + i * 48.0f);
                        drawList.AddRectFilled(cursor, cursor + new Vector2(contentAvail.X, 24.0f), 0x10ffffff);
                    }
                }

                double relative = _timestamps.Average((x) => x.Frametime);
                if (!_isPaused)
                {
                    if (_slowAveraged < 0.0)
                        _slowAveraged = relative;
                    else
                        _slowAveraged = double.Lerp(_slowAveraged, relative, Time.DeltaTimeDouble);
                }

                relative = _slowAveraged * _zoomScale;
                screenCursor += new Vector2(contentAvail.X, contentAvail.Y) * _offsetX;

                int iterations = 0;
                int index = _head;

                while (iterations < _timestamps.Length)
                {
                    DrawProfilingTimestamp(index, relative, ref screenCursor, windowPosition, contentAvail, drawList);

                    index++;
                    if (index >= _timestamps.Length)
                        index = 0;
                    iterations++;
                }

                ImGuiIOPtr io = ImGui.GetIO();
                if (io.MouseWheel != 0.0f)
                {
                    float zoomDiff = -io.MouseWheel * _zoomScale * 0.1f;
                    _zoomScale += zoomDiff;

                    float offsetX = (io.MousePos.X - windowPosition.X) / contentAvail.X;
                    //_offsetX.X += _offsetX.X * zoomDiff;
                }
                if (ImGui.IsWindowFocused() && ImGui.IsWindowHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Right) && io.MouseDelta != Vector2.Zero)
                {
                    _offsetX = _offsetX + io.MouseDelta * (Vector2.One / contentAvail);
                    _offsetX.Y = MathF.Min(_offsetX.Y, 0.0f);
                }
            }
            ImGui.End();
        }

        private void DrawProfilingTimestamp(int index, double relative, ref Vector2 screenCursor, Vector2 windowPosition, Vector2 contentAvail, ImDrawListPtr drawList)
        {
            CapturedProfilingTimestamp capture = _timestamps[index];

            float local = (float)(capture.Frametime / relative);
            float width = local * contentAvail.X;

            if (screenCursor.X >= windowPosition.X - width && screenCursor.X <= windowPosition.X + width * 2.0f)
            {
                drawList.AddLine(screenCursor, new Vector2(screenCursor.X, screenCursor.Y + contentAvail.Y), 0xffffffff);
                drawList.AddLine(new Vector2(screenCursor.X + width, screenCursor.Y), new Vector2(screenCursor.X + width, screenCursor.Y + contentAvail.Y), 0xffffffff);

                double tickToSeconds = 1.0 / Stopwatch.Frequency;
                double secondsToTick = 1.0 / Stopwatch.Frequency / relative;
                double tickToPercentage = secondsToTick * 100.0;

                ImGuiIOPtr io = ImGui.GetIO();
                ImFontPtr font = io.Fonts.Fonts[0];

                Vector2 localCursor = screenCursor;
                Vector2 localRegion = contentAvail - new Vector2(2.0f, 0.0f);

                Vector2 screenMax = windowPosition + contentAvail;

                ImGui.PushID(index);

                Span<ThreadProfilingTimestamps> subTimestampsCollection = capture.Timestamps.AsSpan();
                for (int i = 0; i < subTimestampsCollection.Length; i++)
                {
                    ref ThreadProfilingTimestamps timestampCollection = ref subTimestampsCollection[i];

                    Span<ProfilingTimestamp> timestamps = timestampCollection.Timestamps.Span;

                    int maxDepthReached = 0;
                    for (int j = 0; j < timestamps.Length; j++)
                    {
                        ref ProfilingTimestamp timestamp = ref timestamps[j];
                        maxDepthReached = Math.Max(maxDepthReached, timestamp.Depth);

                        float posStart = (float)(timestamp.StartTimestamp * secondsToTick) * localRegion.X;
                        float posEnd = (float)(timestamp.EndTimestamp * secondsToTick) * localRegion.X;

                        float yValue = timestamp.Depth * 24.0f;

                        Vector2 cursorStart = localCursor + new Vector2(posStart, yValue);
                        Vector2 cursorEnd = localCursor + new Vector2(posEnd, yValue + 24.0f);

                        if (!(cursorStart.X >= windowPosition.X || cursorEnd.X <= screenMax.X || cursorStart.Y >= windowPosition.Y || cursorEnd.Y <= screenMax.Y))
                            continue;

                        double duration = (timestamp.EndTimestamp - timestamp.StartTimestamp) * tickToSeconds;
                        int percentage = (int)(duration / capture.Frametime * 100.0);

                        Color32 solidColor = new Color32((uint)(timestamp.Name.GetDjb2HashCode() | 0xff000000), true);
                        Color32 lineColor = new Color32((byte)(solidColor.R / 2), (byte)(solidColor.G / 2), (byte)(solidColor.B / 2));
                        uint textColor = 0xffffffff;

                        int rgbBig = Math.Max(solidColor.R, Math.Max(solidColor.G, solidColor.B));
                        int average = (int)((solidColor.R + solidColor.G + solidColor.B) * 0.33333f);

                        if (average > 90 && rgbBig > 214)
                            textColor = 0xff000000;

                        drawList.AddRectFilled(cursorStart, cursorEnd, solidColor.ARGB);
                        drawList.AddRect(cursorStart, cursorEnd, lineColor.ARGB);

                        Vector4 clipRect = new Vector4(cursorStart.X, cursorStart.Y, cursorEnd.X - 4.0f, cursorEnd.Y - 2.0f);
                        drawList.AddText(null, 0.0f, cursorStart + new Vector2(4.0f, 2.0f), textColor, percentage > 9 ? $"{timestamp.Name} ({percentage.ToString()}%)" : timestamp.Name, ref clipRect);

                        ImGui.SetCursorScreenPos(cursorStart);
                        ImGui.Dummy(cursorEnd - cursorStart);

                        if (ImGui.BeginItemTooltip())
                        {
                            ImGui.TextUnformatted(timestamp.Name);
                            ImGui.Indent();

                            if (timestamp.Stacktrace != null)
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), timestamp.Stacktrace);
                            }

                            ImGui.Separator();

                            ImGui.TextUnformatted($"Miliseconds: ");
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), (duration * 1000.0).ToString(CultureInfo.InvariantCulture));
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                            ImGui.TextUnformatted("ms");

                            ImGui.TextUnformatted($"Frame usage: ");
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), (duration / capture.Frametime * 100.0).ToString(CultureInfo.InvariantCulture));
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                            ImGui.TextUnformatted("%");


                            ImGui.Unindent();
                            ImGui.EndTooltip();
                        }
                    }

                    drawList.AddText(localCursor + new Vector2(4.0f), 0xffffffff, timestampCollection.ThreadId.ToString());

                    localCursor.Y += maxDepthReached * 24.0f + 48.0f;
                }

                ImGui.PopID();
            }

            screenCursor.X += width + 1.0f;
        }

        private void CaptureTimestampsForLastFrame()
        {
            _head = (_head + 1) % _timestamps.Length;

            ProfilingManager profiler = ProfilingManager.Instance;

            CapturedProfilingTimestamp timestamp = _timestamps[_head];
            for (int i = 0; i < timestamp.Timestamps.Count; i++)
            {
                _profilingTimestampPool.Return(timestamp.Timestamps[i].Timestamps);
            }

            timestamp.Timestamps.Clear();

            timestamp.Frametime = Time.DeltaTimeDouble;
            timestamp.TimestampStart = profiler.StartTimestamp;

            foreach (var kvp in profiler.Timestamps)
            {
                ThreadProfilingTimestamps capture = new ThreadProfilingTimestamps
                {
                    ThreadId = kvp.Value.ThreadId,
                    Timestamps = _profilingTimestampPool.Get()
                };

                capture.Timestamps.AddRange(kvp.Value.Timestamps);
                timestamp.Timestamps.Add(capture);
            }

            timestamp.Timestamps.Sort((a, b) => a.ThreadId - b.ThreadId);
        }

        private class CapturedProfilingTimestamp
        {
            public double Frametime;
            public long TimestampStart;

            public List<ThreadProfilingTimestamps> Timestamps = new List<ThreadProfilingTimestamps>();
        }

        private record struct ListProfilingTimestampPolicy : IPooledObjectPolicy<PooledList<ProfilingTimestamp>>
        {
            public PooledList<ProfilingTimestamp> Create()
            {
                return new PooledList<ProfilingTimestamp>();
            }

            public bool Return(PooledList<ProfilingTimestamp> obj)
            {
                obj.Clear();
                obj.TrimExcess();

                return true;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    ((IDisposable)_profilingTimestampPool).Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
