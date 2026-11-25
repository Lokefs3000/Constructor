using Primary.Common;
using Primary.Rendering.Ancient;
using Primary.Rendering.Ancient.Pass;
using Primary.Rendering.Ancient.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Ancient.Recording
{
    internal sealed class RenderPassStateData
    {
        private Dictionary<FGTexture, FGRenderTargetType> _outputDict;
        private Dictionary<FGResource, FGResourceStateData> _resourceDict;

        internal RenderPassStateData()
        {
            _outputDict = new Dictionary<FGTexture, FGRenderTargetType>();
            _resourceDict = new Dictionary<FGResource, FGResourceStateData>();
        }

        internal void Reset()
        {
            _outputDict.Clear();
            _resourceDict.Clear();
        }

        internal void AddOutput(FGTexture idx, FGRenderTargetType type) => _outputDict[idx] = type;
        internal void AddResource(FGResource idx, FGResourceStateData data) => _resourceDict[idx] = data;

        internal void SetupState(ref readonly RenderPassDescription desc)
        {
            foreach (ref readonly UsedResourceData data in desc.Resources.AsSpan())
            {
                if (data.Resource.ResourceId != FGResourceId.Global)
                {
                    AddResource(data.Resource, new FGResourceStateData(data.Usage));
                }
            }

            foreach (ref readonly UsedRenderTargetData data in desc.RenderTargets.AsSpan())
            {
                if (((FGResource)data.Target).ResourceId != FGResourceId.Global)
                {
                    AddOutput(data.Target, data.Type);
                }
            }
        }

        internal bool ContainsOutput(FGTexture texture, FGRenderTargetType outputType)
        {
            ref FGRenderTargetType val = ref CollectionsMarshal.GetValueRefOrNullRef(_outputDict, texture);
            if (Unsafe.IsNullRef(ref val))
                return false;

            return val == outputType;
        }

        internal bool ContainsResource(FGResource resource, FGResourceUsage resourceAccess)
        {
            ref FGResourceStateData val = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceDict, resource);
            if (Unsafe.IsNullRef(ref val))
                return false;

            return Flags.HasFlag(val.Usage, resourceAccess);
        }
    }

    internal readonly record struct FGResourceStateData(FGResourceUsage Usage);

    internal enum FGOutputType : byte
    {
        RenderTarget = 0,
        DepthStencil
    }
}
