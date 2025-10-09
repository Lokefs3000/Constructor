using CircularBuffer;
using Hexa.NET.ImGui;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.ViewWidgets
{
    internal class FramePacingWidget : SceneViewWidget
    {
        private CircularBuffer<double> _frameSteps;

        private double _frameScale;

        private double _activeFrameDelta;
        private int _activeFrameCount;

        internal FramePacingWidget()
        {
            _frameSteps = new CircularBuffer<double>(20);
        }

        protected override void Render()
        {
            double dt = Time.DeltaTimeDouble;
            int index = Time.FrameIndex;

            ImGui.TextUnformatted($"Delta: {(dt * 1000.0).ToString("F3", CultureInfo.InvariantCulture)}ms ({(1.0 / dt).ToString("F1", CultureInfo.InvariantCulture)} fps)");
            ImGui.TextUnformatted($"Frame: {(uint)index}");

            Vector2 drawRegion = new Vector2(ImGui.GetContentRegionAvail().X, 30.0f);
            Vector2 cursor = ImGui.GetCursorScreenPos();

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            double max = 0.0;
            double all = 0.0;
            foreach (double step in _frameSteps)
            {
                max = Math.Max(max, step);
                all += step;
            }

            if (max > _frameScale)
                _frameScale = max;
            else
                _frameScale = double.Lerp(_frameScale, max * 1.25, Math.Min(dt * 2.0, 1.0));

            string maxText = $"{(_frameScale * 1000.0).ToString("F1", CultureInfo.InvariantCulture)}ms";
            double spikeLimit = all / _frameSteps.Size;

            drawList.AddText(cursor, 0xffffffff, maxText);
            drawList.AddText(cursor + new Vector2(0.0f, drawRegion.Y - 13.0f), 0xffffffff, "0.0ms");

            float width = ImGui.CalcTextSize(maxText).X + 4.0f;
            cursor.X += width;
            drawRegion.X -= width;

            int stepIndex = -1;
            double lastStep = 0.0;

            drawList.AddRect(cursor, cursor + drawRegion, 0xff808080, 3.0f);
            ImGui.Dummy(new Vector2(0.0f, drawRegion.Y));

            cursor += Vector2.One;
            drawRegion -= new Vector2(2.0f);

            foreach (double step in _frameSteps)
            {
                if (stepIndex >= 0)
                {
                    float h1 = (float)((1.0 - lastStep / _frameScale) * drawRegion.Y);
                    float h2 = (float)((1.0 - step / _frameScale) * drawRegion.Y);

                    uint color = 0xffffffff;
                    if (Math.Abs(step - lastStep) > spikeLimit)
                        color = 0xff2020ff;

                    drawList.AddLine(cursor + new Vector2(stepIndex / 20.0f * drawRegion.X, h1), cursor + new Vector2((stepIndex + 1) / 20.0f * drawRegion.X, h2), color);
                }

                stepIndex++;
                lastStep = step;
            }

            if (_activeFrameDelta >= 0.1)
            {
                _frameSteps.PushBack(_activeFrameDelta / _activeFrameCount);

                _activeFrameDelta = 0.0;
                _activeFrameCount = 0;
            }
            else
            {
                _activeFrameDelta += dt;
                _activeFrameCount++;
            }
        }

        public override ReadOnlySpan<string> RequiredIcons => ReadOnlySpan<string>.Empty;
        public override bool IsFloating => true;
    }
}
