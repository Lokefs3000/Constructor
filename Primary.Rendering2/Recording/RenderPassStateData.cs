using Primary.Common;
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
        private Dictionary<int, FGOutputType> _outputDict;
        private Dictionary<int, FGResourceStateData> _resourceDict;

        internal RenderPassStateData()
        {
            _outputDict = new Dictionary<int, FGOutputType>();
            _resourceDict = new Dictionary<int, FGResourceStateData>();
        }

        internal bool ContainsOutput(FrameGraphTexture texture, FGOutputType outputType)
        {
            ref FGOutputType val = ref CollectionsMarshal.GetValueRefOrNullRef(_outputDict, texture.Index);
            if (Unsafe.IsNullRef(ref val))
                return false;

            return val == outputType;
        }

        internal bool ContainsResource(FrameGraphResource resource, FGResourceUsage resourceAccess)
        {
            ref FGResourceStateData val = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceDict, resource.Index);
            if (Unsafe.IsNullRef(ref val))
                return false;

            return val.Usage == resourceAccess;
        }
    }

    internal readonly record struct FGResourceStateData(FGResourceUsage Usage);

    internal enum FGOutputType : byte
    {
        RenderTarget = 0,
        DepthStencil
    }
}
