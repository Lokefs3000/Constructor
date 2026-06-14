using Editor.Gui.Windows;
using Editor.UI;
using Editor.UI.Elements;
using Primary.Profiling;
using Primary.R2.ForwardPlus;
using Primary.R2.ForwardPlus.Statistics;
using Primary.Rendering.Statistics;
using Primary.RHI;
using Primary.Timing;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.Gui.View
{
    internal sealed class RenderStatsSnippet : ViewSnippet
    {
        private UILabel? _fpsStats;
        private UILabel? _renderStats;
        private UILabel? _octreeStats;
        private UILabel? _drawStats;
        private UILabel? _gpuMemStats;
        private UILabel? _gcStats;

        private AverageAnalyser<double> _fpsAnalyzer;

        public RenderStatsSnippet(string? snippetFile, EditorViewWindow window) : base(snippetFile, window)
        {
            _fpsAnalyzer = new AverageAnalyser<double>(60, 1.0f / 30.0f);
        }

        protected override void SetupSelf()
        {
            if (RootElement != null)
            {
                _fpsStats = RootElement.FindElementWithId<UILabel>("fps-stats");
                _renderStats = RootElement.FindElementWithId<UILabel>("render-stats");
                _octreeStats = RootElement.FindElementWithId<UILabel>("octree-stats");
                _drawStats = RootElement.FindElementWithId<UILabel>("draw-stats");
                _gpuMemStats = RootElement.FindElementWithId<UILabel>("gpu-mem-stats");
                _gcStats = RootElement.FindElementWithId<UILabel>("gc-stats");
            }
        }

        protected override void CleanupSelf()
        {
            
        }

        public override void Update()
        {
            if (_fpsStats != null)
            {
                _fpsAnalyzer.Sample(Time.DeltaTimeDouble, Time.DeltaTime);

                double cur = Time.DeltaTimeDouble;
                double avg = _fpsAnalyzer.Calculate();
                double min = _fpsAnalyzer.Min();
                double max = _fpsAnalyzer.Max();

                _fpsStats.Text = $@"FPS: {(int)(1.0 / cur)} [{(cur * 1000.0):f2} MS]
AVG: {(int)(1.0 / avg)} [{(avg * 1000.0):f2} MS]
MIN: {(int)(1.0 / max)} MAX: {(int)(1.0 / min)}";
            }

            if (_renderStats != null)
            {
                ProfilingManager profiler = ProfilingManager.Instance;
                if (profiler.Timestamps.TryGetValue(Environment.CurrentManagedThreadId, out ThreadProfilingTimestamps timestamps))
                {
                    int index = timestamps.Timestamps.FindIndex(static (x) => x.Name == "Render");
                    if (index != -1)
                    {
                        for (int i = index - 1; i >= 0; --i)
                        {
                            ProfilingTimestamp timestamp = timestamps.Timestamps[i];
                            if (timestamp.Depth == 1 && timestamp.Name == "Perform")
                            {
                                double time = (timestamp.EndTimestamp - timestamp.StartTimestamp) / (double)Stopwatch.Frequency;

                                _renderStats.Text = $@"RENDER: {(time * 1000.0):f2} MS";

                                break;
                            }
                        }
                    } 
                }
            }

            if (_octreeStats != null)
            {
                RenderStatistics statistics = EditorRuntime.GlobalSingleton.RenderingManager.Statistics;

                _octreeStats.Text = $@"OCTANTS: {statistics.Batch.OctantsTraversed}
OBJS: {statistics.Batch.OctantObjectsConsidered}
OBJS PASSED: {statistics.Batch.OctantObjectsPassed}
AVG OBJS: {(statistics.Batch.OctantObjectsConsidered / (double)statistics.Batch.OctantsTraversed):f2}
AVG PASSED: {(statistics.Batch.OctantObjectsPassed / (double)statistics.Batch.OctantObjectsConsidered * 100.0):f1}%";
            }

            if (_drawStats != null)
            {
                ForwardPlusRenderPath? renderPath = EditorRuntime.GlobalSingleton.RenderingManager.CurrentRenderPath as ForwardPlusRenderPath;
                if (renderPath != null)
                {
                    RenderPathStatistics statistics = renderPath.Statistics;

                    _drawStats.Text = $@"DRAW CALLS: {statistics.Draw.OpaqueDrawCalls}
DRAW FRAG: {(statistics.Draw.OpaqueDrawCalls / (double)statistics.Draw.OpaqueShaderCount * 100.0):f1}%
SHADERS: {statistics.Draw.OpaqueShaderCount}
FLAGS: {statistics.Draw.OpaqueFlagCount}";
                }
            }

            if (_gpuMemStats != null)
            {
                RHIDevice device = RHIDevice.Instance!;
                RHIUsedMemoryInfo usedMemoryInfo = device.QueryUsedMemory();

                _gpuMemStats.Text = $@"GPU MEM TOTAL: {(usedMemoryInfo.Local.TotalUsage / (1024.0 * 1024.0)):f2} MB
GPU MEM RES: 000.00 MB
GPU MEM FG: 000.00 MB";
            }

            if (_gcStats != null)
            {
                GCProfiler profiler = ProfilingManager.Instance.GCProfiler;
                GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();

                double durr = 0.0;
                double max = 0.0;
                int gens = 0;

                foreach (TrackedGCMarker marker in profiler.Markers)
                {
                    durr += marker.Duration;
                    max = Math.Max(max, marker.Duration);
                    gens += marker.Generation;
                }
         
                durr /= TimeSpan.TicksPerSecond;
                max /= TimeSpan.TicksPerSecond;

                _gcStats.Text = $@"GC RATE: {(profiler.AllocationRate / 1024.0):f2} KB
GC TOTAL: {(profiler.CurrentMemoryUsage / (1024.0 * 1024.0)):f2} MB
GC FRAG: {(memoryInfo.FragmentedBytes / (1024.0 * 1024.0)):f2} MB
GC COST: {(gens / (double)profiler.Markers.Count):f2}
GC TIME: {(durr * 1000.0):f2} MS [{memoryInfo.PauseTimePercentage:f1} %]
GC TIME AVG: {(durr / (double)profiler.Markers.Count):f3} MS
GC TIME MAX: {max:f3} MS";
            }
        }
    }
}
