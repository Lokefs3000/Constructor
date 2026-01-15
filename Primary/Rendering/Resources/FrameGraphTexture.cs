using Primary.RHI2;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Resources
{
    public readonly record struct FrameGraphTexture
    {
        private readonly FrameGraphResource _resource;

        internal FrameGraphTexture(FrameGraphResource resource)
        {
            if (resource.ResourceId != FGResourceId.Texture)
                _resource = FrameGraphResource.Invalid;
            else
                _resource = resource;
        }

        internal FrameGraphTexture(int index)
        {
            _resource = new FrameGraphResource(index);
        }

        public override int GetHashCode() => _resource.GetHashCode();
        public override string ToString() => _resource.ToString();

        [UnscopedRef]
        public ref readonly FrameGraphTextureDesc Description => ref _resource.TextureDesc;
        public int Index => _resource.Index;

        public RHITexture? Resource => Unsafe.As<RHITexture>(_resource.Resource);

        public bool IsExternal => _resource.IsExternal;
        public bool IsValidAndRenderGraph => _resource.IsValidAndRenderGraph;
        public bool IsNull => _resource.IsNull;

        public static readonly FrameGraphTexture Invalid = new FrameGraphTexture(new FrameGraphResource(-1, default(FrameGraphTextureDesc), null));

        public static implicit operator FrameGraphResource(FrameGraphTexture resource) => resource._resource;
        public static explicit operator FrameGraphTexture(FrameGraphResource resource) => resource.AsTexture();

        public static implicit operator FrameGraphTexture(RHITexture texture) => new FrameGraphTexture(new FrameGraphResource(texture, texture.DebugName));
    }
}
