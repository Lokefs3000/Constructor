using Primary.Rendering.Resources;
using Primary.RHI2;

namespace Primary.Rendering.Pass
{
    public sealed class FrameGraphSetup
    {
        public RHISwapChain? OutputSwapChain { get; internal set; }
        public FrameGraphTexture DestinationTexture { get; internal set; }

        internal void ClearForFrame()
        {
            OutputSwapChain = null;
            DestinationTexture = FrameGraphTexture.Invalid;
        }
    }
}
