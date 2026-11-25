using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.R2.ForwardPlus.Statistics
{
    public record struct DrawStatistics
    {
        public int OpaqueDrawCalls;
        public int OpaqueShaderCount;
        public int OpaqueFlagCount;

        internal void ClearTransientData()
        {
            OpaqueDrawCalls = 0;
            OpaqueShaderCount = 0;
            OpaqueFlagCount = 0;
        }
    }
}
