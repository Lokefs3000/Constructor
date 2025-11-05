using Primary.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using RHI = Primary.RHI;

namespace Primary.Rendering2.Resources
{
    public readonly struct FrameGraphResource
    {
        private readonly int _index;
        private readonly FGResourceId _resourceId;
        private readonly RHI.Resource? _resource;
        private readonly ResourceUnion _union;

        public FrameGraphResource()
        {
            _index = -1;
        }

        internal FrameGraphResource(int index, FrameGraphTextureDesc res)
        {
            _index = index;
            _resourceId = FGResourceId.Texture;
            _resource = null;
            _union = new ResourceUnion(res);
        }

        internal FrameGraphResource(int index, FrameGraphBufferDesc res)
        {
            _index = index;
            _resourceId = FGResourceId.Buffer;
            _resource = null;
            _union = new ResourceUnion(res);
        }

        internal FrameGraphResource(RHI.Buffer buffer)
        {
            _index = -1;
            _resourceId = FGResourceId.Buffer | FGResourceId.External;
            _resource = buffer;
            _union = default;
        }

        internal FrameGraphResource(RHI.Texture texture)
        {
            _index = -1;
            _resourceId = FGResourceId.Texture | FGResourceId.External;
            _resource = texture;
            _union = default;
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

        public int Index => _index;

        [UnscopedRef]
        internal ref readonly FrameGraphTextureDesc TextureDesc => ref _union.Texture;
        [UnscopedRef]
        internal ref readonly FrameGraphBufferDesc BufferDesc => ref _union.Buffer;

        internal FGResourceId ResourceId => (FGResourceId)((int)_resourceId & 0b01111111);
        internal bool IsExternal => FlagUtility.HasFlag(_resourceId, FGResourceId.External);

        public static readonly FrameGraphResource Invalid = new FrameGraphResource(-1, default(FrameGraphBufferDesc));

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

        External = 1 << 7
    }
}
