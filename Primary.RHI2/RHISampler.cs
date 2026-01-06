using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.RHI2
{
    public unsafe abstract class RHISampler : RHIResource, AsNativeObject<RHISamplerNative>
    {
        protected RHISamplerDescription _description;

        public ref readonly RHISamplerDescription Description => ref _description;

        public abstract RHISamplerNative* GetAsNative();
    }

    public struct RHISamplerNative
    {
        public RHISamplerDescription Description;
    }

    public struct RHISamplerDescription
    {
        public RHIFilterType Min;
        public RHIFilterType Mag;
        public RHIFilterType Mip;
        public RHIReductionType Reduction;

        public RHITextureAddressMode AddressModeU;
        public RHITextureAddressMode AddressModeV;
        public RHITextureAddressMode AddressModeW;

        public float MipLODBias;
        public uint MaxAnisotropy;

        public RHIComparisonFunction ComparisonFunction;

        public Color BorderColor;

        public float MinLOD;
        public float MaxLOD;

        public RHISamplerDescription()
        {
            Min = RHIFilterType.Linear;
            Mag = RHIFilterType.Linear;
            Mip = RHIFilterType.Linear;
            Reduction = RHIReductionType.Standard;

            AddressModeU = RHITextureAddressMode.Repeat;
            AddressModeV = RHITextureAddressMode.Repeat;
            AddressModeW = RHITextureAddressMode.Repeat;

            MipLODBias = 1.0f;
            MaxAnisotropy = 1;

            ComparisonFunction = RHIComparisonFunction.Never;

            BorderColor = Color.TransparentBlack;

            MinLOD = 0.0f;
            MaxLOD = float.MaxValue;
        }

        public RHISamplerDescription(RHISamplerDescription other)
        {
            Min = other.Min;
            Mag = other.Mag;
            Mip = other.Mip;
            Reduction = other.Reduction;

            AddressModeU = other.AddressModeU;
            AddressModeV = other.AddressModeV;
            AddressModeW = other.AddressModeW;

            MipLODBias = other.MipLODBias;
            MaxAnisotropy = other.MaxAnisotropy;

            ComparisonFunction = other.ComparisonFunction;

            BorderColor = other.BorderColor;

            MinLOD = other.MinLOD;
            MaxLOD = other.MaxLOD;
        }

        [UnscopedRef]
        public unsafe Span<uint> BorderColorUInt => MemoryMarshal.Cast<Color, uint>(new Span<Color>(ref BorderColor));
    }
}
