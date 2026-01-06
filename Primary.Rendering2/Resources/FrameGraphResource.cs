using Primary.Common;
using Primary.RHI2;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Primary.Rendering2.Resources
{
    public readonly record struct FrameGraphResource : IEquatable<FrameGraphResource>
    {
        private readonly int _index;
        private readonly FGResourceId _resourceId;
        private readonly RHIResource? _resource;
        private readonly ResourceUnion _union;

        private readonly string? _debugName;

        public FrameGraphResource()
        {
            _index = -1;
            _debugName = null;
        }

        internal FrameGraphResource(int index, FGResourceId resourceId = FGResourceId.Global)
        {
            _index = index;
            _resourceId = resourceId;
            _resource = null;
            _union = default;

            _debugName = null;
        }

        internal FrameGraphResource(int index, FrameGraphTextureDesc res, string? debugName)
        {
            _index = index;
            _resourceId = FGResourceId.Texture;
            _resource = null;
            _union = new ResourceUnion(res);

            _debugName = debugName;
        }

        internal FrameGraphResource(int index, FrameGraphBufferDesc res, string? debugName)
        {
            _index = index;
            _resourceId = FGResourceId.Buffer;
            _resource = null;
            _union = new ResourceUnion(res);

            _debugName = debugName;
        }

        internal FrameGraphResource(RHIBuffer buffer, string? debugName)
        {
            _index = -1;
            _resourceId = FGResourceId.Buffer | FGResourceId.External;
            _resource = buffer;
            _union = default;

            _debugName = debugName;
        }

        internal FrameGraphResource(RHITexture texture, string? debugName)
        {
            _index = -1;
            _resourceId = FGResourceId.Texture | FGResourceId.External;
            _resource = texture;
            _union = default;

            _debugName = debugName;
        }

        internal FrameGraphResource(RHIResource resource, string? debugName)
        {
            _index = -1;
            _resourceId = (resource.Type == RHIResourceType.Texture ? FGResourceId.Texture : FGResourceId.Buffer) | FGResourceId.External;
            _resource = resource;
            _union = default;

            _debugName = debugName;
        }

        public FrameGraphTexture AsTexture()
        {
            if (ResourceId != FGResourceId.Texture)
                throw new InvalidCastException(_resourceId.ToString());
            return new FrameGraphTexture(this);
        }

        public FrameGraphBuffer AsBuffer()
        {
            if (ResourceId != FGResourceId.Buffer)
                throw new InvalidCastException(_resourceId.ToString());
            return new FrameGraphBuffer(this);
        }

        public override int GetHashCode() => IsExternal ? Resource!.GetHashCode() : Index.GetHashCode();
        public override string ToString() => _debugName ?? _resource?.ToString() ?? (_index == -1 ? "Invalid" : $"{_resourceId}:{_index}");

        public bool Equals(FrameGraphResource other) => IsExternal == other.IsExternal && IsExternal ? (other.Resource == Resource) : (other.Index == Index);

        public int Index => _index;
        public string? DebugName => _debugName;

        [UnscopedRef]
        internal ref readonly FrameGraphTextureDesc TextureDesc => ref _union.Texture;
        [UnscopedRef]
        internal ref readonly FrameGraphBufferDesc BufferDesc => ref _union.Buffer;

        internal RHIResource? Resource => _resource;

        internal FGResourceId ResourceId => (FGResourceId)((int)_resourceId & 0b01111111);
        internal bool IsExternal => FlagUtility.HasFlag(_resourceId, FGResourceId.External);

        internal bool IsValidAndRenderGraph => _resource == null && _index >= 0;

        public static readonly FrameGraphResource Invalid = new FrameGraphResource(-1, default(FrameGraphBufferDesc), null);

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct ResourceUnion
        {
            [FieldOffset(0)]
            public readonly FrameGraphTextureDesc Texture;
            [FieldOffset(0)]
            public readonly FrameGraphBufferDesc Buffer;

            public ResourceUnion(FrameGraphTextureDesc res) => Texture = res;
            public ResourceUnion(FrameGraphBufferDesc res) => Buffer = res;
        }
    }

    internal enum FGResourceId : byte
    {
        Texture,
        Buffer,
        Global,

        External = 1 << 7
    }
}
