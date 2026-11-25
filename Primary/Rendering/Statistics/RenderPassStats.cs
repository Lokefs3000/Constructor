using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Statistics
{
    public struct RenderPassStats
    {
        public int TotalVertexCount;
        public int TotalIndexCount;
        public int TotalInstanceCount;

        public int DrawCallsIssued;
        public int DispatchesIssued;

        public int BarrierRefreshes;

        public int UploadMemoryUsed;
    }
}
