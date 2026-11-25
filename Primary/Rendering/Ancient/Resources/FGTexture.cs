using Primary.RHI;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Ancient.Resources
{
    public readonly record struct FGTexture : IEquatable<FGTexture>, IEquatable<FGResource>
    {
        private readonly FGResource _resource;

        internal FGTexture(FGResource resource)
        {
            if (resource.ResourceId != FGResourceId.Texture)
                _resource = FGResource.Invalid;
            else
                _resource = resource;
        }

        public override int GetHashCode() => _resource.GetHashCode();
        public override string ToString() => _resource.ToString();

        public bool Equals(FGTexture other) => _resource.Equals(other._resource);
        public bool Equals(FGResource other) => _resource.Equals(other);

        [UnscopedRef]
        public ref readonly FGTextureDesc Description => ref _resource.TextureDesc;
        public int Index => _resource.Index;

        public RHITexture? Resource => Unsafe.As<RHITexture>(_resource.Resource);

        public bool IsExternal => _resource.IsExternal;
        public bool IsValidAndRenderGraph => _resource.IsValidAndRenderGraph;
        public bool IsNull => _resource.IsNull;

        public static readonly FGTexture Invalid = new FGTexture(new FGResource());

        public static implicit operator FGResource(FGTexture resource) => resource._resource;
        public static explicit operator FGTexture(FGResource resource) => resource.AsTexture();

        public static implicit operator FGTexture(RHITexture texture) => new FGTexture(new FGResource(texture, texture.DebugName));
    }
}
