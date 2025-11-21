using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Shaders.Data
{
    public readonly record struct StaticSamplerData(string Name, AttributeData[] Attributes, SamplerFilter Filter, SamplerAddressMode AddressModeU, SamplerAddressMode AddressModeV, SamplerAddressMode AddressModeW, uint MaxAnisotropy, float MipLODBias, float MinLOD, float MaxLOD, SamplerBorder Border, IndexRange DeclerationRange);

    public enum SamplerFilter : byte
    {
        Point = 0,
        MinMagPointMipLinear,
        MinPointMagLinearMipPoint,
        MinPointMagMipLinear,
        MinLinearMagMipPoint,
        MinLinearMagPointMipLinear,
        MinMagLinearMipPoint,
        Linear,
        MinMagAnisotropicMipPoint,
    }

    public enum SamplerAddressMode : byte
    {
        Repeat = 0,
        Mirror,
        ClampToEdge,
        ClampToBorder
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
