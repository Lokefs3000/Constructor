using System;
using System.Collections.Generic;
using System.Text;
using Primary.Profiling;
using Primary.Timing;
using Primary.Utility;

namespace PrimaryEditor.Windows.GCProfiler
{
    internal sealed class CurrentUsageGraphSource : IGraphSource<double>
    {
        private AverageAnalyser<double> _heapSizeAverage;

        internal CurrentUsageGraphSource()
        {
            _heapSizeAverage = new AverageAnalyser<double>(60, 0.1f);
        }

        internal void UpdateValues()
        {
            _heapSizeAverage.Sample(ProfilingManager.Instance.GCProfiler.CurrentMemoryUsage, Time.DeltaTime);
        }

        public double Maximum => _heapSizeAverage.Max();
        public double Minimum => 0.0;

        public ReadOnlySpan<double> Values => _heapSizeAverage.Values;
        public int Head => _heapSizeAverage.Head;
    }
}
