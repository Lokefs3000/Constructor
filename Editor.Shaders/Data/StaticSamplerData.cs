using Primary.Common;

namespace Editor.Shaders.Data
{
    public readonly record struct StaticSamplerData(string Name, AttributeData[] Attributes, SamplerFilter Min, SamplerFilter Mag, SamplerFilter Mip, SamplerReductionType Reduction, SamplerAddressMode AddressModeU, SamplerAddressMode AddressModeV, SamplerAddressMode AddressModeW, uint MaxAnisotropy, float MipLODBias, float MinLOD, float MaxLOD, SamplerBorder Border, IndexRange DeclerationRange);

    public enum SamplerFilter : byte
    {
        Linear = 0,
        Point
    }

    public enum SamplerReductionType : byte
    {
        Standard = 0,
    }

    public enum SamplerAddressMode : byte
    {
        Repeat = 0,
        Mirror,
        Clamp,
        Border
    }

    public enum SamplerBorder : byte
    {
        TransparentBlack = 0,
        OpaqueBlack,
        OpaqueWhite,
        OpaqueBlackUInt,
        OpaqueWhiteUInt
    }
}
