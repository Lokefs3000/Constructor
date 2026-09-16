using Primary.RHI;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Resources
{
    public readonly record struct FGBuffer : IEquatable<FGBuffer>, IEquatable<FGResource>
    {
        private readonly FGResource _resource;

        internal FGBuffer(FGResource resource)
        {
            if (resource.ResourceId != FGResourceId.Buffer)
                _resource = FGResource.Invalid;
            else
                _resource = resource;
        }

        public override int GetHashCode() => _resource.GetHashCode();
        public override string ToString() => _resource.ToString();

        public bool Equals(FGBuffer other) => _resource.Equals(other._resource);
        public bool Equals(FGResource other) => _resource.Equals(other);

        [UnscopedRef]
        public ref readonly FGBufferDesc Description => ref _resource.BufferDesc;
        public int Index => _resource.Index;

        public RHIBuffer? Resource => Unsafe.As<RHIBuffer>(_resource.Resource);

        public bool IsExternal => _resource.IsExternal;
        public bool IsValidAndRenderGraph => _resource.IsValidAndRenderGraph;
        public bool IsNull => _resource.IsNull;

        public static readonly FGBuffer Invalid = new FGBuffer(new FGResource());

        public static implicit operator FGResource(FGBuffer resource) => resource._resource;
        public static explicit operator FGBuffer(FGResource resource) => resource.AsBuffer();

        public static implicit operator FGBuffer(RHIBuffer buffer) => new FGBuffer(new FGResource(buffer, buffer.DebugName));
    }
}
