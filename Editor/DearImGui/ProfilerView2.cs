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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.DearImGui
{
    internal sealed class ProfilerView2
    {
        private CaptureDisplayMode _displayMode;
        private int _maxHistorySize;

        private ProfilingCapture[] _captures;
        private int _captureHead;

        internal ProfilerView2()
        {
            _displayMode = CaptureDisplayMode.Raw;
            _maxHistorySize = 30;

            _captures = Array.Empty<ProfilingCapture>();
            _captureHead = 0;

            ResizeProfilingCaptures(_maxHistorySize);
        }

        internal void Render()
        {
            CaptureLastFrame();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            bool begin = ImGui.Begin("Profiler2"u8, ImGuiWindowFlags.MenuBar);
            ImGui.PopStyleVar();

            if (begin)
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("View"u8))
                    {
                        if (ImGui.BeginMenu("Display mode"))
                        {
                            for (int i = 0; i < s_displayModeEnum.Length; i++)
                            {
                                if (ImGui.MenuItem(s_displayModeEnum[i], i == (int)_displayMode))
                                    _displayMode = (CaptureDisplayMode)i;
                            }

                            ImGui.EndMenu();
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenuBar();
                }

                DrawBlocks();
            }
            ImGui.End();
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
        }

        private void CaptureLastFrame()
        {
            ref ProfilingCapture capture = ref _captures[_captureHead];
            _captureHead = (_captureHead >= _captures.Length - 1 ? 0 : _captureHead + 1);

            capture.Dispose();
            capture.Frametime = Time.DeltaTimeDouble;

            ProfilingManager profiler = Editor.GlobalSingleton.ProfilingManager;
            foreach (var kvp in profiler.Timestamps)
            {
                ref ThreadProfilingTimestamps timestamps = ref CollectionsMarshal.GetValueRefOrNullRef(profiler.Timestamps, kvp.Key);
                Debug.Assert(!Unsafe.IsNullRef(ref timestamps));

                ProfilingTimestamp[] array = ArrayPool<ProfilingTimestamp>.Shared.Rent(timestamps.Timestamps.Count);
                capture.Threads.Add(timestamps.ThreadId, new ThreadCapture(array, timestamps.Timestamps.Count));
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
                _captures[i] = new ProfilingCapture(0.0, new Dictionary<int, ThreadCapture>());
            }
        }

        private static byte[][] s_displayModeEnum = [
            "Raw"u8.ToArray(),
            "Averaged"u8.ToArray(),
            "Maximum"u8.ToArray(),
            "Minimum"u8.ToArray(),
            ];

        private const float BlockHeight = 24.0f;
        private const float BlockHeight2x = BlockHeight * 2.0f;

        private record struct ProfilingCapture(double Frametime, Dictionary<int, ThreadCapture> Threads) : IDisposable
        {
            public void Dispose()
            {
                foreach (var kvp in Threads)
                {
                    kvp.Value.Dispose();
                }

                Frametime = 0.0;
                Threads.Clear();
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

        private enum CaptureDisplayMode : byte
        {
            Raw = 0,
            Averaged,
            Maximum,
            Minimum
        }
    }
}
