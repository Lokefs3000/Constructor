using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Configuration
{
    public sealed class FrameGraphOptions
    {
        public bool CompileWithAsyncCompute = true;
        // It takes about 0.2ms for a barrier
        public float AsyncComputeTimeThreshold = 0.3f;

        public int DescriptorRTHeapSize = 512;
        public int DescriptorDSHeapSize = 512;
        public int DescriptorRVCPUHeapSize = 2048;
        public int DescriptorRVGPUHeapSize = 16384;
        public int DescriptorRVSamplerHeapSize = 2048;

        public bool AllowMultithreadedPlayback = false;
    }
}
