using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Pooling;
using Primary.Timing;
using System.Diagnostics;
using System.Numerics;

namespace Primary.Profiling
{
    public sealed class DebugProfiler : IImGuiDrawer
    {
        private CapturedFrame _frame;
        private bool _captureNewFrames;

        private CircularBuffer<CapturedTimings> _timings;

        private ObjectPool<CapturedThread> _capturedThreadPool;

        private float _viewOffset;
        private float _viewZoom;

        public DebugProfiler()
        {
            _frame = new CapturedFrame(new List<CapturedThread>(), 0.0f);
            _captureNewFrames = true;

            _timings = new CircularBuffer<CapturedTimings>(ProfilingManager.Instance.HistorySize);

            _capturedThreadPool = new ObjectPool<CapturedThread>(new CapturedThread.Policy());

            _viewOffset = 0.0f;
            _viewZoom = 1.0f;
        }

        public void Draw()
        {
            if (IMGUI.BeginWindow("Mini profiler"))
            {
                if (Time.FrameIndex != 0 && _captureNewFrames)
                    CaptureTimestamps(ref _frame);

                if (IMGUI.BeginMenuBar())
                {
                    if (IMGUI.Button("CAlloc", Flags.HasFlag(ProfilingManager.Options, ProfilingOptions.CollectAllocation)))
                        ProfilingManager.Options = Flags.ChangeFlags(ProfilingManager.Options, ProfilingOptions.CollectAllocation);
                    if (IMGUI.Button("CStacktrace", Flags.HasFlag(ProfilingManager.Options, ProfilingOptions.CollectStacktrace)))
                        ProfilingManager.Options = Flags.ChangeFlags(ProfilingManager.Options, ProfilingOptions.CollectStacktrace);
                    if (IMGUI.Button("Collect", _captureNewFrames))
                        _captureNewFrames = !_captureNewFrames;

                    IMGUI.EndMenuBar();
                }

                ImGuiDrawList drawList = IMGUI.CurrentWindow!.DrawList;
                ImGuiStyle style = IMGUI.Style;

                Vector2 cursorPos = IMGUI.CursorPosition;
                Vector2 availSize = IMGUI.AvailableSize;

                uint borderColor = style.Border.ABGR;

                float sideBarWidth = float.Truncate(availSize.X * 0.2f);
                float blockWidth = availSize.X - sideBarWidth - 3.0f;

                const float GraphHeight = 120.0f;

                {
                    uint renderColor = new Color32(49, 204, 57).ABGR;
                    uint gcColor = new Color32(82, 92, 51).ABGR;

                    const float FramerateTarget = 60.0f;

                    Boundaries detailBounds = new Boundaries(cursorPos, cursorPos + new Vector2(sideBarWidth + 1.0f, GraphHeight + 1.0f));
                    Boundaries graphBounds = new Boundaries(new Vector2(cursorPos.X + sideBarWidth, cursorPos.Y), cursorPos + new Vector2(availSize.X, GraphHeight + 1.0f));

                    drawList.DrawFilledRect(detailBounds.Minimum, detailBounds.Maximum, 0xf0101010);
                    drawList.DrawFilledRect(graphBounds.Minimum, graphBounds.Maximum, 0xf0101010);

                    drawList.DrawRect(detailBounds.Minimum, detailBounds.Maximum, borderColor);
                    drawList.DrawRect(graphBounds.Minimum, graphBounds.Maximum, borderColor);

                    {
                        Vector2 textCursor = cursorPos + new Vector2(4.0f, 3.0f);

                        drawList.DrawFilledRect(textCursor + Vector2.One, textCursor + new Vector2(ImGuiFont.FontVisualHeight - 1.0f), renderColor);
                        drawList.DrawText(textCursor + new Vector2(ImGuiFont.FontVisualHeight + 2.0f, 2.0f), "Rendering");

                        textCursor.Y += ImGuiFont.FontVisualHeight + 2.0f;

                        drawList.DrawFilledRect(textCursor + Vector2.One, textCursor + new Vector2(ImGuiFont.FontVisualHeight - 1.0f), gcColor);
                        drawList.DrawText(textCursor + new Vector2(ImGuiFont.FontVisualHeight + 2.0f, 2.0f), "Garbage collector");
                    }

                    {
                        float stepSize = Math.Max(_timings.Count / blockWidth, 1.0f);
                        float barWidth = Math.Max(blockWidth / _timings.Count, 1.0f);

                        float localX = graphBounds.Minimum.X + 1.0f;

                        float target = 2.0f / FramerateTarget;

                        for (float j = 0; j < _timings.Count; j += stepSize)
                        {
                            int i = (int)float.Truncate(j);

                            float xPos = j / (float)_timings.Count * blockWidth + localX;
                            float yPos = 0.0f;

                            CapturedTimings timings = _timings[i];

                            yPos = GraphHeight + cursorPos.Y - timings.RenderTime / target * GraphHeight;
                            drawList.DrawFilledRect(new Vector2(xPos, GraphHeight + cursorPos.Y), new Vector2(xPos + barWidth, yPos), renderColor);

                            float prevYPos = yPos;

                            yPos -= timings.GCTime / target * GraphHeight;
                            drawList.DrawFilledRect(new Vector2(xPos, prevYPos), new Vector2(xPos + stepSize, yPos), gcColor);
                        }

                        ProfilingManager profiler = ProfilingManager.Instance;

                        float pulseTime = (float)Math.Sin((Time.TimestampForActiveFrame / (double)Stopwatch.Frequency * 4.0) % Math.PI);

                        foreach (TrackedGCMarker marker in profiler.GCProfiler.Markers)
                        {
                            int frameDiff = Time.GetFrameDifference(Time.FrameIndex, marker.FrameIndex);
                            if (frameDiff < profiler.HistorySize)
                            {
                                float xPos = (profiler.HistorySize - frameDiff) / (float)_timings.Count * blockWidth + localX;

                                Vector2 center = new Vector2(xPos, cursorPos.Y + 5.0f);

                                uint color = marker.Generation switch
                                {
                                    0 => 0xff00ffff,
                                    1 => 0xff0080ff,
                                    2 => 0xff0000ff,
                                    _ => 0xffff00ff
                                };

                                if (marker.Generation == 2)
                                {
                                    drawList.DrawFilledCircle(center + new Vector2(0.0f, 20.0f), 4.0f + pulseTime * 2.0f, color & ~0x80000000);
                                }

                                drawList.DrawFilledRect(center - new Vector2(3.0f), center + new Vector2(3.0f), color);
                                drawList.DrawFilledTriangle(center + new Vector2(-3.0f, 3.0f), center + new Vector2(0.0f, 7.0f), center + new Vector2(3.0f), color);
                                drawList.DrawLine(center, center + new Vector2(0.0f, GraphHeight), 0x20ffffff);
                            }
                        }
                    }

                    cursorPos.Y += GraphHeight;
                }

                int id = IMGUI.GetId(100);
                Boundaries bb = new Boundaries(new Vector2(cursorPos.X + sideBarWidth + 1.0f, cursorPos.Y), cursorPos + availSize);

                IMGUI.ButtonBehaviour(id, bb, MouseButton.Left);
                IMGUI.AddItemId(id, bb);

                if (IMGUI.IsLastItemHeld() || IMGUI.IsLastItemHovered())
                {
                    if (InputSystem.Pointer.WheelDelta.Y != 0.0f)
                    {
                        float oldZoom = _viewZoom;
                        _viewZoom *= MathF.Exp(float.Sign(InputSystem.Pointer.WheelDelta.Y) * 0.2f);

                        float w = 1.0f * oldZoom;
                        float x = (1.0f - w) * 0.5f + _viewOffset;
                        float originX = ((InputSystem.Pointer.MousePosition.X - bb.Minimum.X) / (bb.Maximum.X - bb.Minimum.X)) - x - w * 0.5f;

                        float xOrg = originX / oldZoom;
                        float xNew = xOrg * _viewZoom;
                        float xDiff = originX - xNew;

                        _viewOffset += xDiff;
                    }
                    else if (IMGUI.IsMouseDragging(MouseButton.Right))
                    {
                        _viewOffset += (InputSystem.Pointer.MouseDelta.X / (bb.Maximum.X - bb.Minimum.X));
                    }
                }

                _viewZoom = MathF.Max(_viewZoom, 1.0f);
                _viewOffset = MathF.Min(MathF.Max(_viewOffset, -_viewZoom * 0.5f + 0.5f), _viewZoom * 0.5f - 0.5f);

                float localViewOffset = _viewOffset / _viewZoom - 0.5f;

                float blockWidthMultiplier = blockWidth * _viewZoom;
                float blockHalfAvailWidth = blockWidth * 0.5f;

                {
                    float delta = _frame.DeltaTime * 1000.0f;

                    float fittedAvailWidth = blockWidth - IMGUI.CalculateTextSize($"{delta:f2}ms").X - 1.0f;
                    float deltaMultiplier = fittedAvailWidth / blockWidth;

                    float scaledWidth = fittedAvailWidth * _viewZoom;
                    float halfAvailWidth = fittedAvailWidth * 0.5f + sideBarWidth + 2.0f;

                    float stepLinesOffset = blockHalfAvailWidth + sideBarWidth + 2.0f;

                    Boundaries backgroundBounds = new Boundaries(cursorPos, cursorPos + new Vector2(availSize.X, ImGuiFont.FontVisualHeight + 5.0f));

                    drawList.DrawFilledRect(backgroundBounds.Minimum + Vector2.One, backgroundBounds.Maximum - Vector2.One, 0xf0101010);

                    drawList.DrawRect(backgroundBounds.Minimum, backgroundBounds.Maximum, borderColor);
                    drawList.DrawLine(new Vector2(cursorPos.X + sideBarWidth + 1.0f, backgroundBounds.Minimum.Y), new Vector2(cursorPos.X + sideBarWidth + 1.0f, backgroundBounds.Maximum.Y), borderColor);

                    drawList.PushClip(new Vector4(cursorPos.X + sideBarWidth + 1.0f, backgroundBounds.Minimum.Y, backgroundBounds.Maximum.X - 1.0f, backgroundBounds.Maximum.Y));

                    for (int i = 0; i < 61; i++)
                    {
                        float step = i / 60.0f;
                        float x = float.Truncate(cursorPos.X + (step + (float)localViewOffset) * blockWidthMultiplier + stepLinesOffset);

                        bool isSpecial = (i % 10) == 0;

                        drawList.DrawLine(new Vector2(x, backgroundBounds.Maximum.Y - (isSpecial ? 9.0f : 4.0f)), new Vector2(x, backgroundBounds.Maximum.Y), isSpecial ? 0xfff0f0f0 : 0xff808080);
                    
                        if (isSpecial && i > 0)
                        {
                            string label = $"{(delta * step * deltaMultiplier):f2}ms";

                            Vector2 measured = IMGUI.CalculateTextSize(label);
                            Vector2 textPos = new Vector2(x - measured.X - 3.0f, cursorPos.Y + 3.0f);

                            drawList.DrawText(textPos, label);
                        }
                    }

                    drawList.PopClip();
                }

                Vector2 localCursor = new Vector2(cursorPos.X, cursorPos.Y + ImGuiFont.FontVisualHeight + 4.0f);
                foreach (CapturedThread thread in _frame.Threads)
                {
                    string headerText = $"{thread.ThreadName} (id:{thread.ThreadId})";
                    Vector2 headerSize = IMGUI.CalculateTextSize(headerText);

                    float areaHeight = (thread.MaxDepth + 1) * 24.0f + 1.0f;

                    Vector2 headerAreaSize = new Vector2(sideBarWidth, areaHeight) + Vector2.One;

                    drawList.DrawFilledRect(localCursor + Vector2.One, localCursor + headerAreaSize - Vector2.One, 0x80000000);
                    drawList.DrawRect(localCursor, localCursor + headerAreaSize, borderColor);

                    if (headerSize.X > sideBarWidth - 5.0f)
                    {
                        Vector2 max = localCursor + headerAreaSize;

                        drawList.PushClip(new Vector4(localCursor.X, localCursor.Y, max.X - 4.0f, max.Y), true);
                        drawList.DrawText(localCursor + new Vector2(3.0f, 5.0f), headerText);
                        drawList.PopClip();
                    }
                    else
                        drawList.DrawText(localCursor + new Vector2(3.0f, 5.0f), headerText);

                    localCursor.Y += areaHeight;
                }

                float blockBackgroundWidth = blockWidthMultiplier * (1.0f / 6.0f * 0.5f);

                localCursor = new Vector2(cursorPos.X + sideBarWidth + 2.0f, cursorPos.Y + ImGuiFont.FontVisualHeight + 4.0f);
                foreach (CapturedThread thread in _frame.Threads)
                {
                    float cursorStartX = localCursor.X;
                    float areaHeight = (thread.MaxDepth + 1) * 24.0f + 1.0f;

                    double captureLength = thread.CaptureLength;

                    Boundaries outerBoundaries = new Boundaries(new Vector2(localCursor.X - 2.0f, localCursor.Y), new Vector2(cursorPos.X + availSize.X, localCursor.Y + areaHeight + 1.0f));
                    
                    drawList.DrawRect(outerBoundaries.Minimum, outerBoundaries.Maximum, borderColor);
                    drawList.DrawFilledRect(outerBoundaries.Minimum + Vector2.One, outerBoundaries.Maximum - Vector2.One, 0xf0101010);

                    drawList.PushClip(outerBoundaries.AsVector4(), true);

                    for (int i = 0; i < 7; i++)
                    {
                        float step = i / 6.0f;
                        float x = localCursor.X + (step + (float)localViewOffset) * blockWidthMultiplier + blockHalfAvailWidth - 1.0f;

                        drawList.DrawLine(new Vector2(x, outerBoundaries.Minimum.Y + 1.0f), new Vector2(x + blockBackgroundWidth, outerBoundaries.Maximum.Y - 1.0f), 0xff202020);
                    }

                    Vector2 blockCursor = new Vector2(localCursor.X, localCursor.Y + 1.0f);
                    foreach (CapturedTimestamp timestamp in thread.Timestamps)
                    {
                        Color32 solidColor = new Color32((uint)(timestamp.Name.GetDjb2HashCode() | 0xff000000), true);
                        Color32 lineColor = new Color32((byte)(solidColor.R / 2), (byte)(solidColor.G / 2), (byte)(solidColor.B / 2));
                        uint textColor = 0xffffffff;

                        int rgbBig = Math.Max(solidColor.R, Math.Max(solidColor.G, solidColor.B));
                        int average = (int)((solidColor.R + solidColor.G + solidColor.B) * (1.0f / 3.0f));

                        if (average > 90 && rgbBig > 214)
                            textColor = 0xff000000;

                        float startX = float.Truncate(((float)(timestamp.StartTimestamp / captureLength) + localViewOffset) * blockWidthMultiplier + blockHalfAvailWidth);
                        float endX = float.Truncate(((float)(timestamp.EndTimestamp / captureLength) + localViewOffset) * blockWidthMultiplier + blockHalfAvailWidth);

                        float y = timestamp.Depth * 24.0f;

                        Boundaries bounds = new Boundaries(new Vector2(startX, y), new Vector2(endX, y + 24.0f));
                        bounds = Boundaries.Offset(bounds, blockCursor);

                        drawList.DrawFilledRect(bounds.Minimum, bounds.Maximum, solidColor.ABGR);
                        drawList.DrawRect(bounds.Minimum, bounds.Maximum, lineColor.ABGR);

                        if (bounds.Maximum.X - bounds.Minimum.X > 12.0f)
                        {
                            float timeTaken = (float)((timestamp.EndTimestamp - timestamp.StartTimestamp) / captureLength);
                            Vector4 clipRect = new Vector4(bounds.Minimum.X, bounds.Minimum.Y, bounds.Maximum.X - 8.0f, bounds.Maximum.Y - 2.0f);

                            drawList.PushClip(clipRect, true);

                            drawList.DrawText(bounds.Minimum + new Vector2(4.0f, 3.0f), timeTaken > 0.09f ? $"{timestamp.Name} ({(int)(timeTaken * 100.0f)}%)" : timestamp.Name, textColor);
                            if (timestamp.BytesAllocated > 0)
                                drawList.DrawText(bounds.Minimum + new Vector2(4.0f, ImGuiFont.FontVisualHeight + 2.0f), FileUtility.FormatSize(timestamp.BytesAllocated, "f3"), textColor);

                            drawList.PopClip();
                        }

                        if (bounds.IsWithin(InputSystem.Pointer.MousePosition))
                        {
                            if (IMGUI.BeginTooltip())
                            {
                                IMGUI.Text(timestamp.Name);
                                IMGUI.Text($"Time: {((timestamp.EndTimestamp - timestamp.StartTimestamp) / (double)Stopwatch.Frequency * 1000.0):f4}ms");
                                IMGUI.Text($"Frame used: {((timestamp.EndTimestamp - timestamp.StartTimestamp) / captureLength * 100.0):f1}%");
                                IMGUI.Text($"Bytes allocated: {FileUtility.FormatSize(timestamp.BytesAllocated)}");

                                IMGUI.TextColored($"Id: Thread:{timestamp.Id.ThreadId} Timestamp:{timestamp.Id.TimestampId} Hash:{timestamp.Id.Hash}", 0x80ffffff);
                                IMGUI.TextColored(timestamp.Stacktrace ?? "null", 0x80ffffff);

                                IMGUI.EndTooltip();
                            }
                        }
                    }

                    drawList.PopClip();

                    localCursor.X = cursorStartX;
                    localCursor.Y += areaHeight;
                }

                IMGUI.AddItemSize(new Vector2(blockWidthMultiplier, localCursor.Y - cursorPos.Y));

                IMGUI.EndWindow();
            }
        }

        private void CaptureTimestamps(ref CapturedFrame frame)
        {
            ProfilingManager profiler = ProfilingManager.Instance;
            GCProfiler gcProfiler = profiler.GCProfiler;

            int prevFrameIndex = Time.FrameIndex - 1;

            if (frame.Threads.Count < profiler.Timestamps.Count)
            {
                for (int i = frame.Threads.Count; i < profiler.Timestamps.Count; ++i)
                {
                    frame.Threads.Add(_capturedThreadPool.Get());
                }
            }
            else
            {
                while (frame.Threads.Count > profiler.Timestamps.Count)
                {
                    _capturedThreadPool.Return(frame.Threads[^1]);
                }
            }

            frame.DeltaTime = Time.DeltaTime;

            float renderTime = 0.0f;
            float gcTime = 0.0f;

            int index = 0;
            Span<CapturedThread> threads = frame.Threads.AsSpan();

            foreach (var kvp in profiler.Timestamps)
            {
                ThreadProfilingTimestamps threadProfiling = kvp.Value;

                ref CapturedThread thread = ref threads[index++];
                thread.ThreadId = threadProfiling.ThreadId;
                thread.ThreadName = threadProfiling.ThreadName;

                thread.CaptureStart = threadProfiling.StartTimestamp;
                thread.CaptureLength = Time.TimestampForActiveFrame - thread.CaptureStart;
                thread.TotalAllocated = 0;
                thread.MaxDepth = 0;

                thread.Timestamps.Clear();
                thread.Timestamps.EnsureCapacity(threadProfiling.Timestamps.Count);

                Span<ProfilingTimestamp> timestamps = threadProfiling.Timestamps.Span;
                for (int i = 0; i < timestamps.Length; i++)
                {
                    ref ProfilingTimestamp timestamp = ref timestamps[i];

                    //thread.CaptureStart = Math.Min(thread.CaptureStart, timestamp.StartTimestamp);
                    //thread.CaptureLength = Math.Max(thread.CaptureLength, timestamp.EndTimestamp);
                    thread.TotalAllocated += timestamp.Allocated;
                    thread.MaxDepth = Math.Max(thread.MaxDepth, timestamp.Depth);

                    if (timestamp.Depth == 0)
                    {
                        int threadDjbName = timestamp.Name.GetDjb2HashCode();
                        if (threadDjbName == s_renderTimeHashName)
                        {
                            renderTime = (float)((timestamp.EndTimestamp - timestamp.StartTimestamp) / (double)Stopwatch.Frequency);
                        }
                    }
                }

                for (int i = 0; i < timestamps.Length; i++)
                {
                    ref ProfilingTimestamp timestamp = ref timestamps[i];

                    thread.Timestamps.Add(new CapturedTimestamp(
                        timestamp.Id,
                        timestamp.Depth,
                        timestamp.Allocated,
                        timestamp.StartTimestamp,
                        timestamp.EndTimestamp,
                        timestamp.Name,
                        timestamp.Stacktrace));
                }
            }

            ReadOnlySpan<TrackedGCMarker> gcMarkers = gcProfiler.Markers.AsSpan();
            for (int i = gcProfiler.Markers.Count - 1; i >= 0; --i)
            {
                ref readonly TrackedGCMarker marker = ref gcMarkers[i];
                if (marker.FrameIndex != prevFrameIndex)
                    break;
                else
                    gcTime += (float)(marker.Duration / (double)Stopwatch.Frequency);
            }

            if (_timings.Capacity != profiler.HistorySize)
            {
                _timings.ResizeCapacity(profiler.HistorySize);
            }

            _timings.PushBack(new CapturedTimings(renderTime, gcTime));
        }

        private record struct CapturedFrame(List<CapturedThread> Threads, float DeltaTime);
        private record struct CapturedThread(int ThreadId, string ThreadName, List<CapturedTimestamp> Timestamps, long CaptureStart, long CaptureLength, long TotalAllocated, int MaxDepth)
        {
            public record class Policy : IObjectPoolPolicy<CapturedThread>
            {
                public CapturedThread Create() => new CapturedThread(-1, string.Empty, new List<CapturedTimestamp>(), -1, -1, 0, 0);
                public bool Return(ref CapturedThread obj)
                {
                    obj.ThreadName = string.Empty;
                    obj.Timestamps.Clear();
                    return true;
                }
            }
        }

        private readonly record struct CapturedTimestamp(ProfilingId Id, int Depth, int BytesAllocated, long StartTimestamp, long EndTimestamp, string Name, string? Stacktrace);
        private readonly record struct CapturedTimings(float RenderTime, float GCTime);

        private static readonly int s_renderTimeHashName = "Render".GetDjb2HashCode();
    }
}
