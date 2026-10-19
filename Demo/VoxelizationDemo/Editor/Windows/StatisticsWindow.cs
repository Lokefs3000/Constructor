using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using VoxelizationDemo.Editor.Windows.Statistics;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class StatisticsWindow : IEditorWindow
    {
        private readonly IStatisticsModule[] _modules;

        internal StatisticsWindow()
        {
            _modules = [
                new FrametimeModule(),
                new RenderPassModule()
                ];
        }

        public bool UpdateAndRender()
        {
            ImGui.SetNextWindowPos(new Vector2(11.0f, ImGui.GetMainViewport().Size.Y * 0.5f));
            if (ImGui.Begin("##STATISTICS"u8, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs))
            {
                ImGuiP.BringWindowToDisplayBack(ImGuiP.GetCurrentWindow());
                foreach (IStatisticsModule module in _modules)
                {
                    module.ShowStatistics();
                }
            }
            ImGui.End();

            return true;
        }
    }
}
