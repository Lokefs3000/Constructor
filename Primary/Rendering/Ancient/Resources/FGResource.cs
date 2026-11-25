using Primary.Common;
using Primary.RHI;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Ancient.Resources
{
    public readonly record struct FGResource : IEquatable<FGResource>
    {
        private readonly int _index;
        private readonly FGResourceId _resourceId;
        private readonly RHIResource? _resource;
        private readonly ResourceUnion _union;

        private readonly string? _debugName;

        public FGResource()
        {
            _index = -1;
            _resourceId = FGResourceId.Undefined;
            _resource = null;
            _union = default;

            _debugName = null;
        }

        internal FGResource(int index, FGResourceId resourceId = FGResourceId.Global)
        {
            _index = index;
            _resourceId = resourceId;
            _resource = null;
            _union = default;

            _debugName = null;
        }

        internal FGResource(int index, FGTextureDesc res, string? debugName)
        {
            _index = index;
            _resourceId = FGResourceId.Texture;
            _resource = null;
            _union = new ResourceUnion(res);

            _debugName = debugName;
        }

        internal FGResource(int index, FGBufferDesc res, string? debugName)
        {
            _index = index;
            _resourceId = FGResourceId.Buffer;
            _resource = null;
            _union = new ResourceUnion(res);

            _debugName = debugName;
        }

        internal FGResource(RHIBuffer buffer, string? debugName)
        {
            _index = -1;
            _resourceId = FGResourceId.Buffer | FGResourceId.External;
            _resource = buffer;
            _union = default;

            _debugName = debugName;
        }

        internal FGResource(RHITexture texture, string? debugName)
        {
            _index = -1;
            _resourceId = FGResourceId.Texture | FGResourceId.External;
            _resource = texture;
            _union = default;

            _debugName = debugName;
        }

        internal FGResource(RHIResource resource, string? debugName)
        {
            _index = -1;
            _resourceId = (resource.Type == RHIResourceType.Texture ? FGResourceId.Texture : FGResourceId.Buffer) | FGResourceId.External;
            _resource = resource;
            _union = default;

            _debugName = debugName;
        }

        public FGTexture AsTexture()
        {
            if (ResourceId != FGResourceId.Texture)
                throw new InvalidCastException(_resourceId.ToString());
            return new FGTexture(this);
        }

        public FGBuffer AsBuffer()
        {
            if (ResourceId != FGResourceId.Buffer)
                throw new InvalidCastException(_resourceId.ToString());
            return new FGBuffer(this);
        }

        public override int GetHashCode() => (IsExternal ? Resource!.GetHashCode() : Index.GetHashCode()) ^ (int)_resourceId;
        public override string ToString() => _debugName ?? _resource?.ToString() ?? (_index == -1 ? "Invalid" : $"{_resourceId}:{_index}");

        public bool Equals(FGResource other) => _resourceId == other._resourceId && (IsExternal ? (other._resource == _resource) : (other._index == _index));

        public int Index => _index;
        public string? DebugName => _debugName;

        [UnscopedRef]
        internal ref readonly FGTextureDesc TextureDesc => ref _union.Texture;
        [UnscopedRef]
        internal ref readonly FGBufferDesc BufferDesc => ref _union.Buffer;

        internal RHIResource? Resource => _resource;

        internal FGResourceId ResourceId => (FGResourceId)((int)_resourceId & 0b01111111);
        internal bool IsExternal => Flags.HasFlag(_resourceId, FGResourceId.External);

        internal bool IsValidAndRenderGraph => _resource == null && _index >= 0;
        internal bool IsNull => IsExternal ? _resource == null : _index == -1;

        public static readonly FGResource Invalid = new FGResource();

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct ResourceUnion
        {
            [FieldOffset(0)]
            public readonly FGTextureDesc Texture;
            [FieldOffset(0)]
            public readonly FGBufferDesc Buffer;

            public ResourceUnion(FGTextureDesc res) => Texture = res;
            public ResourceUnion(FGBufferDesc res) => Buffer = res;
        }
    }

    public enum FGResourceId : byte
    {
        Undefined = 0,

        Texture,
        Buffer,
        Global,

        External = 1 << 7
    }
}
