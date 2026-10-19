using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI
{
    public unsafe abstract class RHISwapChainBackbuffer : RHITexture
    {
        public abstract RHISwapChain SwapChain { get; }
        public abstract int BufferIndex { get; }
    }
}
