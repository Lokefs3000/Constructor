using CommunityToolkit.HighPerformance;
using Editor.Gui;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Pooling;
using Primary.Profiling;
using Primary.Timing;
using SDL;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.DearImGui
{
    internal sealed class ProfilerView2
    {
        private readonly DynamicIconSet _iconSet;

        private CaptureDisplayMode _displayMode;
        private ProfileDataOptions _dataOptions;
        private int _maxHistorySize;
        private float _sampleTimeout;

        private bool _sidePanelActive;
        private float _sidePanelSize;

        private ProfilingCapture[] _captures;
        private int _captureHead;
        private float _captureTimer;

        private float _viewOffset;
        private float _viewZoom;

        private DisplayTimestamp[] _displayTimestamps;

        private Dictionary<int, ValueTuple<double, double>> _progressiveAverageDict;

        internal ProfilerView2(DynamicAtlasManager atlasManager)
        {
            _iconSet = atlasManager.CreateIconSet(s_requiredIcons);

            _displayMode = CaptureDisplayMode.Raw;
            _dataOptions = ProfileDataOptions.None;
            _maxHistorySize = 30;
            _sampleTimeout = 0.0f;

            _sidePanelActive = false;
            _sidePanelSize = 100.0f;

            _captures = Array.Empty<ProfilingCapture>();
            _captureHead = 0;
            _captureTimer = 0.0f;

            _viewOffset = 0.0f;
            _viewZoom = 1.0f;

            _displayTimestamps = Array.Empty<DisplayTimestamp>();

            _progressiveAverageDict = new Dictionary<int, (double, double)>();

            ResizeProfilingCaptures(_maxHistorySize);
        }

        internal void Render()
        {
            using (new ProfilingScope("Profiler2"))
            {
                if (_sampleTimeout > 1.0f)
                    _captureTimer = 0.0f;
                else
                {
                    _captureTimer += Time.DeltaTime;
                    if (_captureTimer >= _sampleTimeout)
                    {
                        _captureTimer = 0.0f;
                        CaptureLastFrame();
                    }
                }

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                bool begin = ImGui.Begin("Profiler2"u8);
                ImGui.PopStyleVar();

                if (begin)
                {
                    _sidePanelSize = MathF.Min(ImGui.GetContentRegionAvail().X * 0.3f, 500.0f);
                    if (ImGui.BeginChild("PRIMARY"u8, _sidePanelActive ? new Vector2(ImGui.GetContentRegionAvail().X - _sidePanelSize, 0.0f) : Vector2.Zero, ImGuiWindowFlags.MenuBar))
                    {
                        if (ImGui.BeginMenuBar())
                        {
                            if (ImGui.BeginMenu("View"u8))
                            {
                                if (ImGui.BeginMenu("Display mode"u8))
                                {
                                    for (int i = 0; i < s_displayModeEnum.Length; i++)
                                    {
                                        if (ImGui.MenuItem(s_displayModeEnum[i], i == (int)_displayMode))
                                            _displayMode = (CaptureDisplayMode)i;
                                    }

                                    ImGui.EndMenu();
                                }

                                if (ImGui.BeginMenu("Data collection"u8))
                                {
                                    ProfilingOptions options = ProfilingManager.Options;

                                    ImGui.MenuItem("Timing"u8, true, false);

                                    if (ImGui.MenuItem("Allocation rate"u8, FlagUtility.HasFlag(options, ProfilingOptions.CollectAllocation)))
                                        ProfilingManager.Options |= ProfilingOptions.CollectAllocation;

                                    ImGui.EndMenu();
                                }

                                ImGui.EndMenu();
                            }

                            DrawSampleTimeoutSlider();
                            DrawZoomWidget();
                            DrawSidePanelCollapser();

                            ImGui.EndMenuBar();
                        }

                        HandleUserInput();
                        DrawBlocks();
                    }
                    ImGui.EndChild();

                    if (_sidePanelActive)
                    {
                        ImGui.SameLine();
                        if (ImGui.BeginChild("SIDEPANEL"u8, new Vector2(_sidePanelSize, 0.0f)))
                        {

                        }
                        ImGui.EndChild();
                    }
                }
                ImGui.End();
            }
        }

        private void DrawSampleTimeoutSlider()
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            ref ImGuiStyle style = ref context.Style;
            ref ImGuiWindowPtr window = ref context.CurrentWindow;

            uint id = ImGui.GetID("##TIMEOUT_SLIDER"u8);
            uint id2 = ImGui.GetID("##TIMEOUT_SLIDER_DRAG"u8);

            Vector2 popupPosition = new Vector2(window.DC.CursorPos.X - 1.0f - float.Truncate(style.ItemSpacing.X * 0.5f), window.DC.CursorPos.Y - style.FramePadding.Y + window.MenuBarHeight);

            window.DC.CursorPos.X += float.Truncate(style.ItemSpacing.X * 0.5f);
            Vector2 cursorPos = window.DC.CursorPos + new Vector2(window.DC.MenuColumns.OffsetLabel, window.DC.CurrLineTextBaseOffset);

            ImRect bb = new ImRect(cursorPos, cursorPos + new Vector2(64.0f, context.FontSize));
            ImRect bar_bb = new ImRect(bb.Min + new Vector2(3.0f, 4.0f), bb.Max - new Vector2(3.0f, 4.0f));

            //bool hovered = false, held = false;
            //bool pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held);
            ImGui.PushStyleVarX(ImGuiStyleVar.ItemSpacing, style.ItemSpacing.X * 2.0f);
            bool pressed = false;
            if (context.LastActiveId != id2)
                pressed = ImGui.Selectable("##TIMEOUT_SLIDER"u8, false, ImGuiSelectableFlags.None, new Vector2(64.0f, 0.0f));
            ImGui.PopStyleVar();

            window.DC.CursorPos.X += float.Truncate(style.ItemSpacing.X * (-1.0f + -0.5f));

            if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                if (ImGui.IsItemHovered())
                {
                    EdLog.Gui.Information("new");

                    ImGuiP.SetActiveID(id2, window);
                    ImGuiP.FocusWindow(window);
                }

                if (context.LastActiveId == id2)
                {
                    EdLog.Gui.Information("use");

                }
            }
            else if (context.LastActiveId == id2)
            {
                ImGuiP.ClearActiveID();
            }

            drawList.AddRect(bb.Min + new Vector2(2.0f, 3.0f), bb.Max - new Vector2(2.0f, 3.0f), 0x40ffffff);
            drawList.AddRectFilled(bar_bb.Min, new Vector2(float.Lerp(bar_bb.Min.X, bar_bb.Max.X, _sampleTimeout), bar_bb.Max.Y), 0x20ffffff);

            if (_sampleTimeout == 1.01f)
                drawList.AddText(Vector2.Lerp(bb.Min, bb.Max, 0.5f) - ImGui.CalcTextSize("Never"u8) * 0.5f, 0xffffffff, "Never"u8);
            else if (_sampleTimeout > 0.0f)
            {
                string str = _sampleTimeout.ToString("F2", CultureInfo.InvariantCulture);

                Span<byte> letters = [(byte)str[0], (byte)'.', (byte)str[2], (byte)str[3], (byte)'s'];
                drawList.AddText(Vector2.Lerp(bb.Min, bb.Max, 0.5f) - ImGui.CalcTextSize(letters) * 0.5f, 0xffffffff, letters);
            }
            else if (_sampleTimeout == 0.0f)
                drawList.AddText(Vector2.Lerp(bb.Min, bb.Max, 0.5f) - ImGui.CalcTextSize("Always"u8) * 0.5f, 0xffffffff, "Always"u8);

            bool menuIsOpen = ImGui.IsPopupOpen("##TIMEOUT_SLIDER"u8);

            if (pressed && !menuIsOpen && context.OpenPopupStack.Size > context.BeginPopupStack.Size)
                ImGui.OpenPopup("##TIMEOUT_SLIDER"u8);
            else if (pressed)
            {
                menuIsOpen = true;
                ImGui.OpenPopup("##TIMEOUT_SLIDER"u8, ImGuiPopupFlags.NoReopen);
            }

            if (menuIsOpen)
            {
                ImGuiWindowFlags windowFlags = ImGuiWindowFlags.ChildMenu | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNavFocus;
                if (FlagUtility.HasFlag(window.Flags, ImGuiWindowFlags.ChildMenu))
                    windowFlags |= ImGuiWindowFlags.ChildWindow;

                ref ImGuiLastItemData lastItemInParent = ref context.LastItemData;
                ImGui.SetNextWindowPos(popupPosition, ImGuiCond.Always);
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, style.PopupRounding);
                menuIsOpen = ImGuiP.BeginPopupMenuEx(id, "##TIMEOUT_SLIDER"u8, windowFlags);
                ImGui.PopStyleVar();
                if (menuIsOpen)
                {
                    context.LastItemData = lastItemInParent;
                    if (context.HoveredWindow == window)
                        context.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredWindow;

                    if (ImGui.SliderFloat("##"u8, ref _sampleTimeout, 0.0f, 1.01f, _sampleTimeout == 0.0f ? "Always"u8 : (_sampleTimeout == 1.01f ? "Never"u8 : "%.2fs"u8)))
                        _sampleTimeout = float.Truncate(_sampleTimeout * 100.0f) * 0.01f;

                    ImGui.EndMenu();
                }
            }
            else
            {
                ImGuiP.ClearFlags(ref context.NextWindowData);
            }
        }

        private void DrawZoomWidget()
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            ref ImGuiStyle style = ref context.Style;
            ref ImGuiWindowPtr window = ref context.CurrentWindow;

            if (ImGui.MenuItem("z+"u8))
                _viewZoom += 1.0f;
            if (ImGui.MenuItem("z-"u8))
                _viewZoom = MathF.Max(_viewZoom - 1.0f, 1.0f);

            ImGui.SetNextItemWidth(80.0f);
            ImGui.DragFloat("##Z"u8, ref _viewZoom, 0.1f);
            ImGui.SetNextItemWidth(80.0f);
            ImGui.DragFloat("##O"u8, ref _viewOffset, 0.1f);
        }

        private void DrawSidePanelCollapser()
        {
            if (ImGui.ArrowButton("##SIDE_PANEL_COLLAPSE"u8, _sidePanelActive ? ImGuiDir.Right : ImGuiDir.Left))
                _sidePanelActive = !_sidePanelActive;
        }

        private void HandleUserInput()
        {
            uint id = ImGui.GetID("##BLOCKS"u8);
            ImRect bb = new ImRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + ImGui.GetContentRegionAvail());

            bool hovered = false, held = false;
            ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held);

            ImGuiP.ItemAdd(bb, id);

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ref ImGuiIO io = ref context.IO;

            if (io.MouseWheel != 0.0f)
            {
                float oldZoom = _viewZoom;
                _viewZoom *= MathF.Exp(float.Sign(io.MouseWheel) * 0.2f);

                float w = 1.0f * oldZoom;
                float x = (1.0f - w) * 0.5f + _viewOffset;
                float originX = ((io.MousePos.X - bb.Min.X) / (bb.Max.X - bb.Min.X)) - x - w * 0.5f;

                float xOrg = originX / oldZoom;
                float xNew = xOrg * _viewZoom;
                float xDiff = originX - xNew;

                _viewOffset += xDiff;
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                _viewOffset += (io.MouseDelta.X / (bb.Max.X - bb.Min.X));
            }
        }

        private void DrawBlocks()
        {
            Vector2 size = ImGui.GetContentRegionAvail();

            Vector2 baseScreenOffset = Vector2.Zero;
            Vector2 cursorPos = ImGui.GetCursorScreenPos() + baseScreenOffset;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            //bg
            {
                int columns = (int)MathF.Ceiling(size.Y / BlockHeight2x);
                float offset = MathF.Truncate(baseScreenOffset.Y / BlockHeight2x) * BlockHeight;

                for (int y = 0; y < columns; y++)
                {
                    Vector2 min = cursorPos + new Vector2(0.0f, y * BlockHeight2x);
                    drawList.AddRectFilled(min, min + new Vector2(size.X, BlockHeight), 0x10ffffff);
                }
            }

            {
                ref ProfilingCapture capture = ref _captures[_captureHead];
                if (_displayTimestamps.Length < capture.Timestamps.Count)
                    _displayTimestamps = new DisplayTimestamp[capture.Timestamps.Count * 2];

                RetrieveDisplayTimestamps(ref capture, _displayTimestamps.AsSpan(0, capture.Timestamps.Count));
                DrawProfilerBlocks(drawList, cursorPos, size, ref capture, _displayTimestamps.AsSpan(0, capture.Timestamps.Count));
            }
        }

        private void DrawProfilerBlocks(ImDrawListPtr drawList, Vector2 cursorPos, Vector2 size, ref ProfilingCapture capture, Span<DisplayTimestamp> timestamps)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            _viewZoom = MathF.Max(_viewZoom, 1.0f);
            _viewOffset = MathF.Min(MathF.Max(_viewOffset, -_viewZoom * 0.5f + 0.5f), _viewZoom * 0.5f - 0.5f);

            double mulFrametime = 1.0 / capture.Frametime * size.X * _viewZoom;
            double offset = (_viewOffset / _viewZoom + -0.5) * capture.Frametime;

            float halfSize = size.X * 0.5f;

            int lastThreadId = -1;
            int lastDepth = 0;

            Vector2 baseCursor = cursorPos;
            float yOffset = cursorPos.Y;
            int maxDepth = 0;

            int elementHovered = ImGui.IsItemHovered() ? -1 : -2;

            for (int i = 0; i < timestamps.Length; i++)
            {
                ref readonly DisplayTimestamp display = ref timestamps[i];
                ProfilingTimestamp timestamp = display.Timestamp;

                if (display.Thread != lastThreadId)
                {
                    if (lastThreadId != -1)
                        yOffset += BlockHeight * (maxDepth + 1);
                    baseCursor = cursorPos + new Vector2(0.0f, yOffset);
                    maxDepth = 0;

                    lastThreadId = display.Thread;
                }

                if (timestamp.Depth != lastDepth)
                {
                    if (timestamp.Depth > lastDepth)
                        baseCursor.Y = yOffset + timestamp.Depth * BlockHeight;
                    else
                        baseCursor.Y = yOffset + (timestamp.Depth > 0 ? timestamp.Depth * BlockHeight : 0.0f);
                    lastDepth = timestamp.Depth;
                }

                maxDepth = Math.Max(maxDepth, timestamp.Depth);

                Color32 solidColor = new Color32((uint)(timestamp.Name.GetDjb2HashCode() | 0xff000000), true);
                Color32 lineColor = new Color32((byte)(solidColor.R / 2), (byte)(solidColor.G / 2), (byte)(solidColor.B / 2));
                uint textColor = 0xffffffff;

                int rgbBig = Math.Max(solidColor.R, Math.Max(solidColor.G, solidColor.B));
                int average = (int)((solidColor.R + solidColor.G + solidColor.B) * 0.33333f);

                if (average > 90 && rgbBig > 214)
                    textColor = 0xff000000;

                double start = (display.Start + offset) * mulFrametime;
                double end = (display.End + offset) * mulFrametime;

                Vector2 min = baseCursor + new Vector2((float)start + halfSize, 0.0f);
                Vector2 max = baseCursor + new Vector2((float)end + halfSize, BlockHeight);

                drawList.AddRectFilled(min, max, solidColor.ARGB);
                drawList.AddRect(min, max, lineColor.ARGB);

                float percentage = (float)((display.End - display.Start) / capture.Frametime);
                if (max.X - min.X > 12.0f)
                {
                    unsafe
                    {
                        Vector4 clipRect = new Vector4(min.X, min.Y, max.X - 4.0f, max.Y - 2.0f);
                        drawList.AddText(null, 0.0f, min + new Vector2(4.0f, 2.0f), textColor, percentage > 0.09f ? $"{timestamp.Name} ({float.Truncate(percentage * 100.0f).ToString()}%)" : timestamp.Name, ref clipRect);
                    }
                }

                if (elementHovered == -1 && new Boundaries(min, max).IsWithin(io.MousePos))
                {
                    elementHovered = i;
                }
            }

            if (elementHovered >= 0)
            {
                if (ImGui.BeginTooltip())
                {
                    ref readonly DisplayTimestamp display = ref timestamps[elementHovered];
                    ProfilingTimestamp timestamp = display.Timestamp;

                    double duration = display.End - display.Start;

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
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), (duration * 1000.0).ToString("F5", CultureInfo.InvariantCulture));
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                    ImGui.TextUnformatted("ms");

                    ImGui.TextUnformatted($"Frame usage: ");
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), (duration / capture.Frametime * 100.0).ToString("F3", CultureInfo.InvariantCulture));
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                    ImGui.TextUnformatted("%");

                    if (FlagUtility.HasFlag(ProfilingManager.Options, ProfilingOptions.CollectAllocation))
                    {
                        ImGui.TextUnformatted($"Allocated: ");
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 7.0f);
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), FileUtility.FormatSize(timestamp.Allocated, "F2", CultureInfo.InvariantCulture));
                    }

                    ImGui.Unindent();
                    ImGui.EndTooltip();
                }
            }
        }

        private void RetrieveDisplayTimestamps(ref ProfilingCapture capture, Span<DisplayTimestamp> timestamps)
        {
            switch (_displayMode)
            {
                case CaptureDisplayMode.Raw:
                    {
                        foreach (var kvp in capture.Threads)
                        {
                            ThreadCapture thread = kvp.Value;
                            for (int i = 0; i < thread.Count; i++)
                            {
                                ref ProfilingTimestamp timestamp = ref thread.Timestamps[i];
                                timestamps[i] = new DisplayTimestamp(kvp.Key, timestamp, timestamp.StartTimestamp / (double)Stopwatch.Frequency, timestamp.EndTimestamp / (double)Stopwatch.Frequency);
                            }
                        }

                        break;
                    }
                case CaptureDisplayMode.ProgressiveAveraged:
                    {
                        double time = 0.001;
                        foreach (var kvp in capture.Threads)
                        {
                            ThreadCapture thread = kvp.Value;
                            for (int i = 0; i < thread.Count; i++)
                            {
                                ref ProfilingTimestamp timestamp = ref thread.Timestamps[i];

                                double start = timestamp.StartTimestamp / (double)Stopwatch.Frequency;
                                double value = (timestamp.EndTimestamp - timestamp.StartTimestamp) / (double)Stopwatch.Frequency;

                                int id = HashCode.Combine(timestamp.Id.ThreadId, timestamp.Id.Hash);

                                if (_progressiveAverageDict.TryGetValue(id, out ValueTuple<double, double> average))
                                    average = new ValueTuple<double, double>(double.Lerp(average.Item1, start, time), double.Lerp(average.Item2, value, time));
                                else
                                    average = new ValueTuple<double, double>(start, value);

                                timestamps[i] = new DisplayTimestamp(kvp.Key, timestamp, average.Item1, average.Item1 + average.Item2);
                                _progressiveAverageDict[id] = average;
                            }
                        }

                        break;
                    }
                case CaptureDisplayMode.Averaged:
                    {
                        int head = _captureHead;
                        for (int j = 0; j < _captures.Length - 1; j++)
                        {
                            head++;
                            if (head >= _captures.Length)
                                head = 0;

                            ref ProfilingCapture subCapture = ref _captures[j];
                            foreach (var kvp in subCapture.Threads)
                            {
                                ThreadCapture thread = kvp.Value;
                                for (int i = 0; i < thread.Count; i++)
                                {
                                    ref ProfilingTimestamp timestamp = ref thread.Timestamps[i];
                                    if (head != _captureHead)
                                        timestamps[i] = new DisplayTimestamp(kvp.Key, timestamp, timestamps[i].Start + timestamp.StartTimestamp / (double)Stopwatch.Frequency, timestamps[i].End + timestamp.EndTimestamp / (double)Stopwatch.Frequency);
                                    else
                                        timestamps[i] = new DisplayTimestamp(kvp.Key, timestamp, timestamp.StartTimestamp / (double)Stopwatch.Frequency, timestamp.EndTimestamp / (double)Stopwatch.Frequency);
                                }
                            }
                        }

                        for (int i = 0; i < timestamps.Length; i++)
                        {
                            ref DisplayTimestamp display = ref timestamps[i];
                            timestamps[i] = new DisplayTimestamp(display.Thread, display.Timestamp, display.Start / (double)_captures.Length, display.End / (double)_captures.Length);
                        }

                        break;
                    }
                case CaptureDisplayMode.Maximum:
                    break;
                case CaptureDisplayMode.Minimum:
                    break;
            }
        }

        private void CaptureLastFrame()
        {
            using (new ProfilingScope("Capture"))
            {
                _captureHead = (_captureHead >= _captures.Length - 1 ? 0 : _captureHead + 1);
                ref ProfilingCapture capture = ref _captures[_captureHead];

                capture.Dispose();
                capture.Frametime = Time.DeltaTimeDouble;

                ProfilingManager profiler = Editor.GlobalSingleton.ProfilingManager;
                foreach (var kvp in profiler.Timestamps)
                {
                    ref ThreadProfilingTimestamps timestamps = ref CollectionsMarshal.GetValueRefOrNullRef(profiler.Timestamps, kvp.Key);
                    Debug.Assert(!Unsafe.IsNullRef(ref timestamps));

                    ProfilingTimestamp[] array = ArrayPool<ProfilingTimestamp>.Shared.Rent(timestamps.Timestamps.Count);
                    timestamps.Timestamps.CopyTo(array);

                    Span<ProfilingTimestamp> span = timestamps.Timestamps.Span;
                    for (int i = 0; i < span.Length; i++)
                    {
                        ref readonly ProfilingTimestamp timestamp = ref span[i];
                        capture.Timestamps.Add(timestamp.Id, i);
                    }

                    capture.Threads.Add(timestamps.ThreadId, new ThreadCapture(array, timestamps.Timestamps.Count));
                }
            }
        }

        private void ResizeProfilingCaptures(int historySize)
        {
            _captureHead = 0;

            if (_captures.Length > 0)
            {
                foreach (ProfilingCapture capture in _captures)
                {
                    capture.Dispose();
                }
            }

            if (historySize == 0)
            {
                _captures = Array.Empty<ProfilingCapture>();
                return;
            }

            _captures = new ProfilingCapture[historySize];
            for (int i = 0; i < _captures.Length; i++)
            {
                _captures[i] = new ProfilingCapture(0.0, new Dictionary<int, ThreadCapture>(), new Dictionary<ProfilingId, int>());
            }

            _displayTimestamps = Array.Empty<DisplayTimestamp>();
        }

        private static readonly byte[][] s_displayModeEnum = [
            "Raw"u8.ToArray(),
            "Progressive average"u8.ToArray(),
            "Averaged"u8.ToArray(),
            "Maximum"u8.ToArray(),
            "Minimum"u8.ToArray(),
            ];

        private static readonly string[] s_requiredIcons = [
            "ZoomIn.png",
            "ZoomOut.png"
            ];

        private static readonly int[] s_requiredIconIds = s_requiredIcons.Select((x) => DynamicIconSet.GetIdForString(x)).ToArray();

        private const float BlockHeight = 24.0f;
        private const float BlockHeight2x = BlockHeight * 2.0f;

        private record struct ProfilingCapture(double Frametime, Dictionary<int, ThreadCapture> Threads, Dictionary<ProfilingId, int> Timestamps) : IDisposable
        {
            public void Dispose()
            {
                foreach (var kvp in Threads)
                {
                    kvp.Value.Dispose();
                }

                Frametime = 0.0;

                Threads.Clear();
                Timestamps.Clear();
            }
        }

        private readonly record struct ThreadCapture(ProfilingTimestamp[] Timestamps, int Count) : IDisposable
        {
            public void Dispose()
            {
                if (Timestamps.Length > 0)
                {
                    ArrayPool<ProfilingTimestamp>.Shared.Return(Timestamps, true);
                }
            }
        }

        private readonly record struct DisplayTimestamp(int Thread, ProfilingTimestamp Timestamp, double Start, double End);

        private enum CaptureDisplayMode : byte
        {
            Raw = 0,
            ProgressiveAveraged,
            Averaged,
            Maximum,
            Minimum
        }

        private enum ProfileDataOptions : byte
        {
            None = 0,

            Allocation = 1 << 0,

        }
    }
}
