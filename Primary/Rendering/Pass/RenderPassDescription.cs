using Collections.Pooled;
using Primary.Rendering.Recording;

namespace Primary.Rendering.Pass
{
    public readonly record struct RenderPassDescription(string Name, RenderPassType Type, PooledList<UsedResourceData> Resources, PooledList<UsedRenderTargetData> RenderTargets, Type? PassDataType, Action<IPassContext, IPassData>? Function, bool AllowCulling);

    public enum RenderPassType : byte
    {
        Graphics = 0,
        Compute
    }
}
