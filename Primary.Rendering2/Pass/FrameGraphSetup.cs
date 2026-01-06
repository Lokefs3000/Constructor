using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.Pass
{
    public sealed class FrameGraphSetup
    {
        public RHI.SwapChain? OutputSwapChain { get; internal set; }
        public FrameGraphTexture DestinationTexture { get; internal set; }

        internal void ClearForFrame()
        {
            OutputSwapChain = null;
            DestinationTexture = FrameGraphTexture.Invalid;
        }
    }
}
