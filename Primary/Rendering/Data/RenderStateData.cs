using System;
using System.Collections.Generic;
using System.Text;
using Primary.RHI;

namespace Primary.Rendering.Data
{
    public sealed class RenderStateData : IContextItem
    {
        public RHISwapChain? SwapChain { get; internal set; }

        internal RenderStateData()
        {
            SwapChain = null;
        }
    }
}
