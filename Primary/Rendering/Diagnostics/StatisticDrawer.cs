using Primary.GUI.ImGui;
using Primary.Timing;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.Rendering.Diagnostics
{
    internal sealed class StatisticDrawer : IImGuiDrawer
    {
        private AverageAnalyser<double> _analyzer;

        internal StatisticDrawer()
        {
            _analyzer = new AverageAnalyser<double>(16, 0.1f);
        }

        public void Draw()
        {
            _analyzer.Sample(Time.DeltaTimeDouble, Time.DeltaTime);

            if (IMGUI.BeginWindow("R-Statistics"))
            {
                double averageDelta = _analyzer.Calculate();

                IMGUI.Text("General:");
                IMGUI.Indent();
                IMGUI.Text($"Framerate: average:{(1.0 / averageDelta):f1}, target:{-1}");
                IMGUI.Text($"Frametime: average:{(averageDelta * 1000.0):f3}ms, raw:{(Time.DeltaTimeDouble * 100.0):f3}ms");
                IMGUI.Unindent();
            }
            IMGUI.EndWindow();
        }
    }
}
