using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Resources
{
    public readonly record struct FrameGraphBuffer
    {
        private readonly FrameGraphResource _resource;

        internal FrameGraphBuffer(FrameGraphResource resource)
        {
            if (resource.ResourceId != FGResourceId.Buffer)
                _resource = FrameGraphResource.Invalid;
            else
                _resource = resource;
        }

        public override int GetHashCode() => _resource.GetHashCode();
        public override string ToString() => _resource.ToString();

        [UnscopedRef]
        public ref readonly FrameGraphBufferDesc Description => ref _resource.BufferDesc;
        public int Index => _resource.Index;

        public RHI.Resource? Resource => _resource.Resource;

        public bool IsExternal => _resource.IsExternal;
        public bool IsValidAndRenderGraph => _resource.IsValidAndRenderGraph;

        public static readonly FrameGraphBuffer Invalid = new FrameGraphBuffer(new FrameGraphResource(-1, default(FrameGraphBufferDesc), null));

        public static implicit operator FrameGraphResource(FrameGraphBuffer resource) => resource._resource;
        public static explicit operator FrameGraphBuffer(FrameGraphResource resource) => resource.AsBuffer();

        public static implicit operator FrameGraphBuffer(RHI.Buffer buffer) => new FrameGraphBuffer(new FrameGraphResource(buffer, null));
    }
}
