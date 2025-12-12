using Collections.Pooled;
using Primary.Rendering2.Recording;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.Pass
{
    internal readonly record struct RenderPassDescription(string Name, RenderPassType Type, PooledList<UsedResourceData> Resources, PooledList<UsedRenderTargetData> RenderTargets, Type? PassDataType, Action<RasterPassContext, IPassData>? Function);
    
    internal enum RenderPassType : byte
    {
        Graphics = 0,
        Compute
    }
}
