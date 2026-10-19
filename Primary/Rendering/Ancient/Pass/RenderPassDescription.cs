using Collections.Pooled;
using Primary.Collections;
using Primary.Rendering.Ancient;
using Primary.Rendering.Ancient.Recording;
using System.Buffers;

namespace Primary.Rendering.Ancient.Pass
{
    public readonly record struct RenderPassDescription(string Name, int GroupIndex, RenderPassType Type, ArraySegment<UsedResourceData> Resources, ArraySegment<UsedRenderTargetData> RenderTargets, IPassData PassData, Action<object, IPassContext, IPassData>? Function, object? RealFunction, bool AllowCulling);

    public enum RenderPassType : byte
    {
        Graphics = 0,
        Compute
    }
}
