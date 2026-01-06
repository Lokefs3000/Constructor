using Primary.Assets;
using Primary.Rendering2.Resources;

using RHI = Primary.RHI;

namespace Primary.Rendering2.Assets
{
    public readonly record struct ROPropertyBlock
    {
        private readonly PropertyBlock? _block;

        public ROPropertyBlock(PropertyBlock? block)
        {
            _block = block; 
        }

        public FrameGraphBuffer GetFrameGraphBuffer(int id) => _block?.GetFrameGraphBuffer(id) ?? FrameGraphBuffer.Invalid;
        public RHI.Buffer? GetRHIBuffer(int id) => _block?.GetRHIBuffer(id);
        public FrameGraphTexture GetFrameGraphTexture(int id) => _block?.GetFrameGraphTexture(id) ?? FrameGraphTexture.Invalid;
        public RHI.Texture? GetRHITexture(int id) => _block?.GetRHITexture(id);
        public TextureAsset? GetTextureAsset(int id) => _block?.GetTextureAsset(id);

        public void CopyBlockDataTo(nint nativePtr) => _block?.CopyBlockDataTo(nativePtr);

        public ShaderAsset2? Shader => _block?.Shader;
        public int BlockSize => _block?.BlockSize ?? 0;

        public bool IsNull => _block == null;

        internal PropertyBlock? InternalBlock => _block;

        public static implicit operator ROPropertyBlock(PropertyBlock? block) => new ROPropertyBlock(block);
    }
}
