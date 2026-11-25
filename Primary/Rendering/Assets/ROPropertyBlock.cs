using Primary.Assets;
using Primary.Rendering.Resources;
using Primary.RHI;

namespace Primary.Rendering.Assets
{
    public readonly record struct ROPropertyBlock
    {
        private readonly PropertyBlock? _block;

        public ROPropertyBlock(PropertyBlock? block)
        {
            _block = block;
        }

        public BufferHandle GetFrameGraphBuffer(string id) => _block?.GetFrameGraphBuffer(id) ?? BufferHandle.Null;
        public RHIBuffer? GetRHIBuffer(string id) => _block?.GetRHIBuffer(id);
        public TextureHandle GetFrameGraphTexture(string id) => _block?.GetFrameGraphTexture(id) ?? TextureHandle.Null;
        public RHITexture? GetRHITexture(string id) => _block?.GetRHITexture(id);
        public TextureAsset? GetTextureAsset(string id) => _block?.GetTextureAsset(id);

        public void CopyBlockDataTo(Span<byte> span) => _block?.CopyBlockDataTo(span);

        public IShaderResourceSource? Shader => _block?.Shader;
        public int BlockSize => _block?.BlockSize ?? 0;

        public bool IsNull => _block == null;

        internal PropertyBlock? InternalBlock => _block;

        public static implicit operator ROPropertyBlock(PropertyBlock? block) => new ROPropertyBlock(block);
    }
}
