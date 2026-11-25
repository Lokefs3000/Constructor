using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public abstract class AttributeSignature
    {
        protected abstract AttributeBuilder Setup();
    }

    public enum AttributeUsage : ushort
    {
        None = 0,

        Function = 1 << 0,
        ConstantBuffer = 1 << 1,
        StructuredBuffer = 1 << 2,
        ByteAddressBuffer = 1 << 9,
        Texture1D = 1 << 3,
        Texture2D = 1 << 4,
        Texture3D = 1 << 5,
        TextureCube = 1 << 6,
        SamplerState = 1 << 7,
        StaticSampler = 1 << 10,

        Property = 1 << 8,

        GenericTexture = Texture1D | Texture2D | Texture3D | TextureCube,
        GenericResource = ConstantBuffer | StructuredBuffer | ByteAddressBuffer | Texture1D | Texture2D | Texture3D | TextureCube,
        GenericSampler = SamplerState | StaticSampler,
    }
}
