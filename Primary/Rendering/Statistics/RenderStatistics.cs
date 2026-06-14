using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Statistics
{
    public sealed class RenderStatistics
    {
        public BatchStatistics Batch;

        internal RenderStatistics()
        {
            Batch = new BatchStatistics();
        }

        internal void ResetTransientStats()
        {
            Batch.ResetTransientStats();
        }
    }
}
