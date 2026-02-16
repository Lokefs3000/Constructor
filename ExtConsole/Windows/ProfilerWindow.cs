using CircularBuffer;
using CommunityToolkit.HighPerformance;
using ExtConsole.Communication;
using ExtConsole.Communication.Messages;
using ExtConsole.Communication.Messages.Profiler;
using ExtConsole.Communication.Serialization;
using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace ExtConsole.Windows
{
    internal sealed class ProfilerWindow : IWindow
    {
        private bool _sampleNewTimestamps;

        private List<ProfilingTimestamp> _timestamps;
        private CircularBuffer<double> _frames;

        private int _activeHash;

        private float _viewOffset;
        private float _viewZoom;

        private float _detailsPaneHeight;

        private MsgSetProfilerFeatures.ProfilerFeatures _features;

        internal ProfilerWindow()
        {
            _sampleNewTimestamps = true;

            _timestamps = new List<ProfilingTimestamp>();
            _frames = new CircularBuffer<double>(100);

            _activeHash = int.MinValue;

            _viewOffset = 0.0f;
            _viewZoom = 1.0f;

            _detailsPaneHeight = 50.0f;

            _features = 0;
        }

        public void Render(ExtConsoleHost host)
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();

            {
                Vector2 avail = ImGui.GetContentRegionAvail();
                if (_detailsPaneHeight > avail.Y - 40.0f)
                    _detailsPaneHeight = avail.Y - 40.0f;
                _detailsPaneHeight = MathF.Min(avail.Y * 0.25f, 200.0f);
            }

            if (!_frames.IsEmpty)
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                Vector2 cursor = ImGui.GetCursorScreenPos();
                Vector2 avail = ImGui.GetContentRegionAvail();

                const double Reference = 1.0 / 60.0;

                Vector2 elementSize = new Vector2(avail.X, 48.0f);

                cursor += new Vector2(1.0f);
                elementSize -= new Vector2(2.0f);

                double maxDeltaValue = _frames.Average();
                float textBoxHeight = elementSize.Y - context.FontSize;

                for (int i = 0; i < 2; i++)
                {
                    double val = i switch
                    {
                        0 => Reference,
                        1 => maxDeltaValue
                    };

                    string str = $"{(val * 1000.0).ToString("F1", CultureInfo.InvariantCulture)}ms";
                    Vector2 textSize = ImGui.CalcTextSize(str);

                    Vector2 pos = new Vector2(avail.X - textSize.X, (float)(1.0 - val / Reference) * textBoxHeight);
                    if (i == 1)
                        pos.Y = Math.Max(pos.Y, context.FontSize - context.Style.FramePadding.Y);
                    pos += cursor;

                    drawList.AddText(pos, 0xffffffff, str);

                    elementSize.X = MathF.Min(elementSize.X, avail.X - textSize.X);
                }

                elementSize.X -= context.Style.FramePadding.X;

                float width = elementSize.X / _frames.Capacity;
                int activeIndex = -1;

                for (int i = 0; i < _frames.Size; i++)
                {
                    Vector2 pos = new Vector2(i * width, elementSize.Y - MathF.Min((float)(_frames[i] / Reference), 1.0f) * elementSize.Y);
                    Boundaries bounds = new Boundaries(pos + cursor, new Vector2(pos.X + width, elementSize.Y) + cursor);

                    bool isActive = activeIndex == -1 && bounds.IsWithin(context.IO.MousePos);
                    if (isActive)
                        activeIndex = i;

                    drawList.AddRectFilled(bounds.Minimum, bounds.Maximum, isActive ? 0xff1064a3 : 0xff1b98f7);
                    drawList.AddRect(bounds.Minimum, bounds.Maximum, isActive ? 0xff0392ff : 0xff1c5480);
                }

                drawList.AddRect(cursor - Vector2.One, cursor + elementSize + Vector2.One, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR);
                ImGui.Dummy(elementSize + new Vector2(2.0f));

                if (activeIndex != -1)
                {
                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.TextUnformatted($"{(_frames[activeIndex] * 1000.0).ToString("F5", CultureInfo.InvariantCulture)}ms");
                        ImGui.EndTooltip();
                    }
                }
            }

            ImGui.Separator();

            if (_timestamps.Count > 0)
            {
                if (ImGui.BeginChild("BLOCKS"u8, new Vector2(-1.0f, ImGui.GetContentRegionAvail().Y - _detailsPaneHeight)))
                {
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                    Vector2 cursor = ImGui.GetCursorScreenPos();
                    Vector2 avail = ImGui.GetContentRegionAvail();

                    Vector2 usedSpace = avail;

                    bool leftMouseDown = false;
                    {
                        uint id = ImGui.GetID("##BLOCKS"u8);
                        ImRect bb = new ImRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + ImGui.GetContentRegionAvail());

                        bool hovered = false;
                        ImGuiP.ButtonBehavior(bb, id, ref hovered, ref leftMouseDown, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);

                        ImGuiP.ItemAdd(bb, id);

                        ref ImGuiIO io = ref context.IO;

                        if (ImGui.IsItemActive() || ImGui.IsItemHovered())
                        {
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
                    }

                    _viewZoom = MathF.Max(_viewZoom, 1.0f);
                    _viewOffset = MathF.Min(MathF.Max(_viewOffset, -_viewZoom * 0.5f + 0.5f), _viewZoom * 0.5f - 0.5f);

                    float mul = usedSpace.X * _viewZoom;
                    float halfSize = usedSpace.X * 0.5f;

                    double offset = (_viewOffset / _viewZoom - 0.5);

                    long largestEndTimestamp = 0;
                    long totalAllocated = 0;

                    foreach (ProfilingTimestamp timestamp in _timestamps)
                    {
                        largestEndTimestamp = Math.Max(timestamp.EndTimestamp, largestEndTimestamp);
                        totalAllocated += timestamp.Allocated;
                    }

                    leftMouseDown = leftMouseDown && ImGui.IsMouseDown(ImGuiMouseButton.Left);
                    int activeHashPrev = _activeHash;

                    if (leftMouseDown)
                        _activeHash = int.MinValue;

                    for (int i = 0; i < 20; i++)
                    {
                        float x = mul * (i / 20.0f + (float)offset) + usedSpace.X * 0.5f;
                        drawList.AddRectFilled(cursor + new Vector2(x, 0.0f), cursor + new Vector2(x + mul * 0.05f, usedSpace.Y), (i % 2) > 0 ? 0x80808080 : 0x80404040);
                    }

                    foreach (ProfilingTimestamp timestamp in _timestamps)
                    {
                        Color32 solidColor = new Color32((uint)(timestamp.Name.GetDjb2HashCode() | 0xff000000), true);
                        Color32 lineColor = new Color32((byte)(solidColor.R / 2), (byte)(solidColor.G / 2), (byte)(solidColor.B / 2));
                        uint textColor = 0xffffffff;

                        int rgbBig = Math.Max(solidColor.R, Math.Max(solidColor.G, solidColor.B));
                        int average = (int)((solidColor.R + solidColor.G + solidColor.B) * 0.33333f);

                        if (average > 90 && rgbBig > 214)
                            textColor = 0xff000000;

                        float startX = (float)(timestamp.StartTimestamp / (double)largestEndTimestamp + offset) * mul + halfSize;
                        float endX = (float)(timestamp.EndTimestamp / (double)largestEndTimestamp + offset) * mul + halfSize;

                        float y = timestamp.Depth * 24.0f;

                        Boundaries bounds = new Boundaries(new Vector2(startX, y), new Vector2(endX, y + 24.0f));
                        bounds = Boundaries.Offset(bounds, cursor);

                        drawList.AddRectFilled(bounds.Minimum, bounds.Maximum, solidColor.ABGR);
                        drawList.AddRect(bounds.Minimum, bounds.Maximum, activeHashPrev == timestamp.Id.Hash ? new Color32(255).ABGR : lineColor.ABGR);

                        if (bounds.Maximum.X - bounds.Minimum.X > 12.0f)
                        {
                            drawList.AddRect(new Vector2(bounds.Maximum.X - 6.0f, bounds.Minimum.Y + 2.0f), new Vector2(bounds.Maximum.X - 2.0f, bounds.Maximum.Y - 2.0f), lineColor.ABGR);
                            drawList.AddRectFilled(new Vector2(bounds.Maximum.X - 5.0f, float.Lerp(bounds.Minimum.Y + 2.0f, bounds.Maximum.Y - 2.0f, (float)(timestamp.Allocated / (double)totalAllocated))), new Vector2(bounds.Maximum.X - 3.0f, bounds.Maximum.Y - 2.0f), 0xffff0000);

                            float percentage = (float)((timestamp.EndTimestamp - timestamp.StartTimestamp) / (double)largestEndTimestamp);
                            unsafe
                            {
                                Vector4 clipRect = new Vector4(bounds.Minimum.X, bounds.Minimum.Y, bounds.Maximum.X - 8.0f, bounds.Maximum.Y - 2.0f);
                                drawList.AddText(null, 0.0f, bounds.Minimum + new Vector2(4.0f, 2.0f), textColor, percentage > 0.09f ? $"{timestamp.Name} ({float.Truncate(percentage * 100.0f).ToString()}%)" : timestamp.Name, ref clipRect);
                            }
                        }

                        if (leftMouseDown && bounds.IsWithin(context.IO.MousePos))
                        {
                            _activeHash = timestamp.Id.Hash;
                        }
                    }
                }
                ImGui.EndChild();
            }

            if (_activeHash != int.MinValue)
            {
                int idx = _timestamps.FindIndex((x) => x.Id.Hash == _activeHash);
                if (idx != -1)
                {
                    ImGui.Separator();

                    if (ImGui.BeginChild("DETAILS"u8, new Vector2(-1.0f)))
                    {
                        ProfilingTimestamp timestamp = _timestamps[idx];

                        double time = (timestamp.EndTimestamp - timestamp.StartTimestamp) / (double)Stopwatch.Frequency;

                        ImGui.TextUnformatted(timestamp.Name);
                        ImGui.NewLine();

                        ImGui.TextUnformatted($"Miliseconds: {(time * 1000.0).ToString("F6")}");
                        ImGui.TextUnformatted($"Percentage: {(time / _frames.Back()).ToString("P", CultureInfo.InvariantCulture)}");
                        ImGui.TextUnformatted($"Allocated: {FileUtility.FormatSize(timestamp.Allocated, provider: CultureInfo.InvariantCulture)}");

                    }
                    ImGui.EndChild();
                }
            }
        }

        public void MenuBar(ExtConsoleHost host)
        {
            if (ImGui.BeginMenu("Features"))
            {
                MsgSetProfilerFeatures.ProfilerFeatures features = _features;
                if (ImGui.MenuItem("Allocation"u8, Flags.HasFlag(features, MsgSetProfilerFeatures.ProfilerFeatures.Allocation)))
                    features = Flags.ChangeFlags(features, MsgSetProfilerFeatures.ProfilerFeatures.Allocation);

                if (_features != features)
                {
                    host.SendMessage(host.ActiveClient!, new MsgSetProfilerFeatures
                    {
                        Features = features
                    });

                    _features = features;
                }

                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Sampling", _sampleNewTimestamps))
                _sampleNewTimestamps = !_sampleNewTimestamps;
        }

        public void Recieve(QueuedMessage queued)
        {
            switch (queued.Id)
            {
                case MessageId.NewProfilerData:
                    {
                        if (_sampleNewTimestamps)
                        {
                            MsgNewProfilerData msg = queued.Deserialize<MsgNewProfilerData>();

                            _timestamps.Clear();
                            _timestamps.EnsureCapacity(msg.TimestampCount);

                            _frames.PushBack(msg.Frametime);
                        }
                        break;
                    }
                case MessageId.ProfilerTimestamp:
                    {
                        if (_sampleNewTimestamps)
                        {
                            MsgProfilerTimestamp msg = queued.Deserialize<MsgProfilerTimestamp>();
                            _timestamps.Add(new ProfilingTimestamp(msg.Name, new ProfilingId(msg.PrId), msg.Depth, msg.Start, msg.End, msg.Allocated));
                        }

                        break;
                    }
            }
        }

        public string WindowName => "Profiler";
        public bool HasMenuBar => true;

        private readonly record struct ProfilingTimestamp(string Name, ProfilingId Id, int Depth, long StartTimestamp, long EndTimestamp, int Allocated);

        private readonly record struct ProfilingId(long Raw)
        {
            public int ThreadId => (int)(Raw >> 48 & 0xffff);
            public int TimestampId => (int)(Raw >> 32 & 0xffff);
            public int Hash => (int)(Raw & 0xffffffff);
        }
    }
}
