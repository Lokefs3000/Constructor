using Primary.RHI;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Resources
{
    public readonly record struct FrameGraphBuffer : IEquatable<FrameGraphBuffer>, IEquatable<FrameGraphResource>
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

        public bool Equals(FrameGraphBuffer other) => _resource.Equals(other._resource);
        public bool Equals(FrameGraphResource other) => _resource.Equals(other);

        [UnscopedRef]
        public ref readonly FrameGraphBufferDesc Description => ref _resource.BufferDesc;
        public int Index => _resource.Index;

        public RHIBuffer? Resource => Unsafe.As<RHIBuffer>(_resource.Resource);

        public bool IsExternal => _resource.IsExternal;
        public bool IsValidAndRenderGraph => _resource.IsValidAndRenderGraph;
        public bool IsNull => _resource.IsNull;

        public static readonly FrameGraphBuffer Invalid = new FrameGraphBuffer(new FrameGraphResource());

        public static implicit operator FrameGraphResource(FrameGraphBuffer resource) => resource._resource;
        public static explicit operator FrameGraphBuffer(FrameGraphResource resource) => resource.AsBuffer();

        public static implicit operator FrameGraphBuffer(RHIBuffer buffer) => new FrameGraphBuffer(new FrameGraphResource(buffer, buffer.DebugName));
    }
}
