using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Statistics;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Windows.Statistics
{
    internal sealed class RenderPassModule : IStatisticsModule
    {
        private RenderPassStats _totalStats;

        private int _renderPassCount;
        private int _requiredUploadMemory;

        internal RenderPassModule()
        {
            _totalStats = default;

            _renderPassCount = 0;
            _requiredUploadMemory = 0;
        }

        public void ShowStatistics()
        {
            ImGui.TextUnformatted($"Pass count: {_renderPassCount}");
            ImGui.TextUnformatted($"    Vertex count: {_totalStats.TotalVertexCount}");
            ImGui.TextUnformatted($"    Index count: {_totalStats.TotalIndexCount}");
            ImGui.TextUnformatted($"    Instance count: {_totalStats.TotalInstanceCount}");
            ImGui.TextUnformatted($"    Draw calls: {_totalStats.DrawCallsIssued}");
            ImGui.TextUnformatted($"    Dispatches: {_totalStats.DispatchesIssued}");
            ImGui.TextUnformatted($"    Barrier refreshes: {_totalStats.BarrierRefreshes}");
            ImGui.TextUnformatted($"    Upload memory: {FileUtility.FormatSize(_totalStats.UploadMemoryUsed, "F2")} (+{FileUtility.FormatSize(_requiredUploadMemory - _totalStats.UploadMemoryUsed, "F2")})");

            RenderingManager renderer = VoxelRuntime.Instance.RenderingManager;
            renderer.InspectRenderPlan(OnInspectRenderPlan);
        }

        private void OnInspectRenderPlan(CompiledRenderPlan renderPlan)
        {
            _totalStats = default;

            _renderPassCount = renderPlan.RenderPasses.Count;
            _requiredUploadMemory = renderPlan.RequiredUploadMemory;

            foreach (CompiledRenderPass renderPass in renderPlan.RenderPasses)
            {
                ref RenderPassStats stats = ref renderPass.Stats;
                _totalStats.TotalVertexCount += stats.TotalVertexCount;
                _totalStats.TotalIndexCount += stats.TotalIndexCount;
                _totalStats.TotalInstanceCount += stats.TotalInstanceCount;

                _totalStats.DrawCallsIssued += stats.DrawCallsIssued;
                _totalStats.DispatchesIssued += stats.DispatchesIssued;

                _totalStats.BarrierRefreshes += stats.BarrierRefreshes;

                _totalStats.UploadMemoryUsed += stats.UploadMemoryUsed;
            }
        }
    }
}
