using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.GUI.ImGui;
using Primary.Rendering.Statistics;
using Primary.Timing;
using VoxelizationDemo.Core;
using VoxelizationDemo.Rendering;

namespace VoxelizationDemo.Diagnostics
{
    internal class DebugStatDrawer : IImGuiDrawer
    {
        public void Draw()
        {
            if (IMGUI.BeginWindow("DEBUGSTATS", ImGuiWindowFlags.NoTitlebar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysOnTop | ImGuiWindowFlags.AlwaysResize))
            {
                ImGuiWindowState windowState = IMGUI.CurrentWindow!;
                windowState.Position = new Vector2(20.0f);

                windowState.DrawList.DrawFilledRect(windowState.Position, windowState.Position + windowState.Size, 0x80000000);

                RenderStatistics statistics = VoxelRuntime.Instance.RenderingManager.Statistics;
                CoreRenderPath renderPath = (CoreRenderPath)VoxelRuntime.Instance.RenderingManager.CurrentRenderPath!;

                IMGUI.Text($"Delta time: {Time.DeltaTimeDouble * 1000.0:f4} ms ({1.0 / Time.DeltaTimeDouble:f1} fps)");
                
                IMGUI.Text($"Regions iterated: {statistics.Batch.RegionsIterated}");
                IMGUI.Text($"Octants traversed: {statistics.Batch.OctantsTraversed}");
                IMGUI.Text($"Objects considered: {statistics.Batch.OctantObjectsConsidered}");
                IMGUI.Text($"Objects passed: {statistics.Batch.OctantObjectsPassed}");

                IMGUI.Text($"Point lights: {renderPath.LightCollector!.PointLights.Count}");

                IMGUI.Text($"Materials: {renderPath.RenderList!.MaterialIds.Count}");
                IMGUI.Text($"Models: {renderPath.RenderList!.ModelIds.Count}");
                IMGUI.Text($"Shaders: {renderPath.RenderList!.ShaderIds.Count}");
                IMGUI.Text($"Flags: {renderPath.RenderList!.TotalFlagCount}");

                IMGUI.EndWindow();
            }
        }
    }
}
