using Primary.Common;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Resources;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    internal sealed class RenderPassStateData
    {
        private Dictionary<FrameGraphTexture, FGRenderTargetType> _outputDict;
        private Dictionary<FrameGraphResource, FGResourceStateData> _resourceDict;

        internal RenderPassStateData()
        {
            _outputDict = new Dictionary<FrameGraphTexture, FGRenderTargetType>();
            _resourceDict = new Dictionary<FrameGraphResource, FGResourceStateData>();
        }

        internal void Reset()
        {
            _outputDict.Clear();
            _resourceDict.Clear();
        }

        internal void AddOutput(FrameGraphTexture idx, FGRenderTargetType type) => _outputDict[idx] = type;
        internal void AddResource(FrameGraphResource idx, FGResourceStateData data) => _resourceDict[idx] = data;

        internal void SetupState(ref readonly RenderPassDescription desc)
        {
            foreach (ref readonly UsedResourceData data in desc.Resources.Span)
            {
                if (data.Resource.ResourceId != FGResourceId.Global)
                {
                    AddResource(data.Resource, new FGResourceStateData(data.Usage));
                }
            }

            foreach (ref readonly UsedRenderTargetData data in desc.RenderTargets.Span)
            {
                if (((FrameGraphResource)data.Target).ResourceId != FGResourceId.Global)
                {
                    AddOutput(data.Target, data.Type);
                }
            }
        }

        internal bool ContainsOutput(FrameGraphTexture texture, FGRenderTargetType outputType)
        {
            ref FGRenderTargetType val = ref CollectionsMarshal.GetValueRefOrNullRef(_outputDict, texture);
            if (Unsafe.IsNullRef(ref val))
                return false;

            return val == outputType;
        }

        internal bool ContainsResource(FrameGraphResource resource, FGResourceUsage resourceAccess)
        {
            ref FGResourceStateData val = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceDict, resource);
            if (Unsafe.IsNullRef(ref val))
                return false;

            return FlagUtility.HasFlag(val.Usage, resourceAccess);
        }
    }

    internal readonly record struct FGResourceStateData(FGResourceUsage Usage);

    internal enum FGOutputType : byte
    {
        RenderTarget = 0,
        DepthStencil
    }
}
