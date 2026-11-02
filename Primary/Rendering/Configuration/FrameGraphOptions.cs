using System;
using System.Collections.Generic;
using System.Text;
using Primary.Console;

namespace Primary.Rendering.Configuration
{
    [ConsoleName("r", false)]
    public sealed class FrameGraphOptions
    {
        [ConsoleName("asyncCompute")]
        public bool CompileWithAsyncCompute = true;
        // It takes about 0.2ms for a barrier
        [ConsoleName("asyncComputeThreshold")]
        public float AsyncComputeTimeThreshold = 0.3f;

        public int DescriptorRTHeapSize = 512;
        public int DescriptorDSHeapSize = 512;
        public int DescriptorRVCPUHeapSize = 2048;
        public int DescriptorRVGPUHeapSize = 16384;
        public int DescriptorRVSamplerHeapSize = 2048;

        [ConsoleName("mtPlayback")]
        public bool AllowMultithreadedPlayback = false;
    }
}
