using Primary.Common;

namespace Primary.RHI
{
    public abstract class Sampler : Resource
    {
        public abstract ref readonly SamplerDescription Description { get; }
    }

    public struct SamplerDescription
    {
        public TextureFilter Filter;

        public TextureAddressMode AddressModeU;
        public TextureAddressMode AddressModeV;
        public TextureAddressMode AddressModeW;

        public ComparisonFunc ComparisonFunc;

        public Color? BorderColor;

        public float MipLODBias;
        public float MinLOD;
        public float MaxLOD;
        public uint MaxAnisotropy;
    }
}
