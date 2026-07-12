using System.Runtime.Versioning;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe static class ConversionHelper
    {
        public static Format ToTextureFormat(this RHIFormat format) => format switch
        {
            RHIFormat.Unknown => Format.FormatUnknown,
            RHIFormat.RGBA32_Typeless => Format.FormatR32G32B32A32Typeless,
            RHIFormat.RGBA32_Float => Format.FormatR32G32B32A32Float,
            RHIFormat.RGBA32_UInt => Format.FormatR32G32B32A32Uint,
            RHIFormat.RGBA32_SInt => Format.FormatR32G32B32A32Sint,
            RHIFormat.RGB32_Typeless => Format.FormatR32G32B32Typeless,
            RHIFormat.RGB32_Float => Format.FormatR32G32B32Float,
            RHIFormat.RGB32_UInt => Format.FormatR32G32B32Uint,
            RHIFormat.RGB32_SInt => Format.FormatR32G32B32Sint,
            RHIFormat.RG32_Typeless => Format.FormatR32G32Typeless,
            RHIFormat.RG32_Float => Format.FormatR32G32Float,
            RHIFormat.RG32_UInt => Format.FormatR32G32Uint,
            RHIFormat.RG32_SInt => Format.FormatR32G32Sint,
            RHIFormat.R32_Typeless => Format.FormatR32Typeless,
            RHIFormat.R32_Float => Format.FormatR32Float,
            RHIFormat.R32_UInt => Format.FormatR32Uint,
            RHIFormat.R32_SInt => Format.FormatR32Sint,
            RHIFormat.RGBA16_Typeless => Format.FormatR16G16B16A16Typeless,
            RHIFormat.RGBA16_Float => Format.FormatR16G16B16A16Float,
            RHIFormat.RGBA16_UNorm => Format.FormatR16G16B16A16Unorm,
            RHIFormat.RGBA16_UInt => Format.FormatR16G16B16A16Uint,
            RHIFormat.RGBA16_SNorm => Format.FormatR16G16B16A16SNorm,
            RHIFormat.RGBA16_SInt => Format.FormatR16G16B16A16Sint,
            RHIFormat.RG16_Typeless => Format.FormatR16G16Typeless,
            RHIFormat.RG16_Float => Format.FormatR16G16Float,
            RHIFormat.RG16_UNorm => Format.FormatR16G16Unorm,
            RHIFormat.RG16_UInt => Format.FormatR16G16Uint,
            RHIFormat.RG16_SNorm => Format.FormatR16G16SNorm,
            RHIFormat.RG16_SInt => Format.FormatR16G16Sint,
            RHIFormat.R16_Typeless => Format.FormatR16Typeless,
            RHIFormat.R16_Float => Format.FormatR16Float,
            RHIFormat.R16_UInt => Format.FormatR16Uint,
            RHIFormat.R16_SNorm => Format.FormatR16SNorm,
            RHIFormat.R16_SInt => Format.FormatR16Sint,
            RHIFormat.RGBA8_Typeless => Format.FormatR8G8B8A8Typeless,
            RHIFormat.RGBA8_UNorm => Format.FormatR8G8B8A8Unorm,
            RHIFormat.RGBA8_UNorm_sRGB => Format.FormatR8G8B8A8UnormSrgb,
            RHIFormat.RGBA8_UInt => Format.FormatR8G8B8A8Uint,
            RHIFormat.RGBA8_SNorm => Format.FormatR8G8B8A8SNorm,
            RHIFormat.RGBA8_SInt => Format.FormatR8G8B8A8Sint,
            RHIFormat.RG8_Typeless => Format.FormatR8G8Typeless,
            RHIFormat.RG8_UNorm => Format.FormatR8G8Unorm,
            RHIFormat.RG8_UInt => Format.FormatR8G8Uint,
            RHIFormat.RG8_SNorm => Format.FormatR8G8SNorm,
            RHIFormat.RG8_SInt => Format.FormatR8G8Sint,
            RHIFormat.R8_Typeless => Format.FormatR8Typeless,
            RHIFormat.R8_UNorm => Format.FormatR8Unorm,
            RHIFormat.R8_UInt => Format.FormatR8Uint,
            RHIFormat.R8_SNorm => Format.FormatR8SNorm,
            RHIFormat.R8_SInt => Format.FormatR8Sint,
            RHIFormat.RGB10A2_Typeless => Format.FormatR10G10B10A2Typeless,
            RHIFormat.RGB10A2_UNorm => Format.FormatR10G10B10A2Unorm,
            RHIFormat.RGB10A2_UInt => Format.FormatR10G10B10A2Uint,
            RHIFormat.RG11B10_Float => Format.FormatR11G11B10Float,
            RHIFormat.D32_Float => Format.FormatD32Float,
            RHIFormat.D16_UNorm => Format.FormatD16Unorm,
            RHIFormat.R32G8X24_Typeless => Format.FormatR32G8X24Typeless,
            RHIFormat.D32_Float_S8X24_UInt => Format.FormatD32FloatS8X24Uint,
            RHIFormat.R32_Float_X8X24_Typeless => Format.FormatR32FloatX8X24Typeless,
            RHIFormat.X32_Typeless_G8X24_UInt => Format.FormatX32TypelessG8X24Uint,
            RHIFormat.R24G8_Typeless => Format.FormatR24G8Typeless,
            RHIFormat.D24_UNorm_S8_UInt => Format.FormatD24UnormS8Uint,
            RHIFormat.R24_UNorm_X8_Typeless => Format.FormatR24UnormX8Typeless,
            RHIFormat.X24_Typeless_G8_UInt => Format.FormatX24TypelessG8Uint,
            RHIFormat.BC1_Typeless => Format.FormatBC1Typeless,
            RHIFormat.BC1_UNorm => Format.FormatBC1Unorm,
            RHIFormat.BC1_UNorm_sRGB => Format.FormatBC1UnormSrgb,
            RHIFormat.BC2_Typeless => Format.FormatBC2Typeless,
            RHIFormat.BC2_UNorm => Format.FormatBC2Unorm,
            RHIFormat.BC2_UNorm_sRGB => Format.FormatBC2UnormSrgb,
            RHIFormat.BC3_Typeless => Format.FormatBC3Typeless,
            RHIFormat.BC3_UNorm => Format.FormatBC3Unorm,
            RHIFormat.BC3_UNorm_sRGB => Format.FormatBC3UnormSrgb,
            RHIFormat.BC4_Typeless => Format.FormatBC4Typeless,
            RHIFormat.BC4_UNorm => Format.FormatBC4Unorm,
            RHIFormat.BC4_SNorm => Format.FormatBC4SNorm,
            RHIFormat.BC5_Typeless => Format.FormatBC5Typeless,
            RHIFormat.BC5_UNorm => Format.FormatBC5Unorm,
            RHIFormat.BC5_SNorm => Format.FormatBC5SNorm,
            RHIFormat.BC6H_Typeless => Format.FormatBC6HTypeless,
            RHIFormat.BC6H_UFloat16 => Format.FormatBC6HUF16,
            RHIFormat.BC6H_SFloat16 => Format.FormatBC6HSF16,
            RHIFormat.BC7_Typeless => Format.FormatBC7Typeless,
            RHIFormat.BC7_UNorm => Format.FormatBC7Unorm,
            RHIFormat.BC7_UNorm_sRGB => Format.FormatBC7UnormSrgb,
            _ => Format.FormatUnknown,
        };

        public static Format ToRenderTargetFormat(this RHIFormat format) => format switch
        {
            RHIFormat.RGBA32_Float => Format.FormatR32G32B32A32Float,
            RHIFormat.RGBA32_UInt => Format.FormatR32G32B32A32Uint,
            RHIFormat.RGBA32_SInt => Format.FormatR32G32B32A32Sint,
            RHIFormat.RGB32_Float => Format.FormatR32G32B32Float,
            RHIFormat.RGB32_UInt => Format.FormatR32G32B32Uint,
            RHIFormat.RGB32_SInt => Format.FormatR32G32B32Sint,
            RHIFormat.RG32_Float => Format.FormatR32G32Float,
            RHIFormat.RG32_UInt => Format.FormatR32G32Uint,
            RHIFormat.RG32_SInt => Format.FormatR32G32Sint,
            RHIFormat.R32_Float => Format.FormatR32Float,
            RHIFormat.R32_UInt => Format.FormatR32Uint,
            RHIFormat.R32_SInt => Format.FormatR32Sint,
            RHIFormat.RGBA16_Typeless => Format.FormatR16G16B16A16Typeless,
            RHIFormat.RGBA16_Float => Format.FormatR16G16B16A16Float,
            RHIFormat.RGBA16_UNorm => Format.FormatR16G16B16A16Unorm ,
            RHIFormat.RGBA16_UInt => Format.FormatR16G16B16A16Uint,
            RHIFormat.RGBA16_SNorm => Format.FormatR16G16B16A16SNorm,
            RHIFormat.RGBA16_SInt => Format.FormatR16G16B16A16Sint,
            RHIFormat.RG16_Typeless => Format.FormatR16G16Typeless,
            RHIFormat.RG16_Float => Format.FormatR16G16Float,
            RHIFormat.RG16_UNorm => Format.FormatR16G16Unorm,
            RHIFormat.RG16_UInt => Format.FormatR16G16Uint,
            RHIFormat.RG16_SNorm => Format.FormatR16G16SNorm,
            RHIFormat.RG16_SInt => Format.FormatR16G16Sint,
            RHIFormat.R16_Typeless => Format.FormatR16Typeless,
            RHIFormat.R16_Float => Format.FormatR16Float,
            RHIFormat.R16_UInt => Format.FormatR16Uint,
            RHIFormat.R16_SNorm => Format.FormatR16SNorm,
            RHIFormat.R16_SInt => Format.FormatR16Sint,
            RHIFormat.RGBA8_Typeless => Format.FormatR8G8B8A8Typeless,
            RHIFormat.RGBA8_UNorm => Format.FormatR8G8B8A8Unorm,
            RHIFormat.RGBA8_UNorm_sRGB => Format.FormatR8G8B8A8UnormSrgb,
            RHIFormat.RGBA8_UInt => Format.FormatR8G8B8A8Uint,
            RHIFormat.RGBA8_SNorm => Format.FormatR8G8B8A8SNorm,
            RHIFormat.RGBA8_SInt => Format.FormatR8G8B8A8Sint,
            RHIFormat.RG8_Typeless => Format.FormatR8G8Typeless,
            RHIFormat.RG8_UNorm => Format.FormatR8G8Unorm,
            RHIFormat.RG8_UInt => Format.FormatR8G8Uint,
            RHIFormat.RG8_SNorm => Format.FormatR8G8SNorm,
            RHIFormat.RG8_SInt => Format.FormatR8G8Sint,
            RHIFormat.R8_Typeless => Format.FormatR8Typeless,
            RHIFormat.R8_UNorm => Format.FormatR8Unorm,
            RHIFormat.R8_UInt => Format.FormatR8Uint,
            RHIFormat.R8_SNorm => Format.FormatR8SNorm,
            RHIFormat.R8_SInt => Format.FormatR8Sint,
            RHIFormat.RGB10A2_Typeless => Format.FormatR10G10B10A2Typeless,
            RHIFormat.RGB10A2_UNorm => Format.FormatR10G10B10A2Unorm,
            RHIFormat.RGB10A2_UInt => Format.FormatR10G10B10A2Uint,
            RHIFormat.RG11B10_Float => Format.FormatR11G11B10Float,
            _ => Format.FormatUnknown,
        };

        public static Format ToDepthStencilFormat(this RHIFormat format) => format switch
        {
            RHIFormat.R32_Typeless => Format.FormatD32Float,
            RHIFormat.R16_Typeless => Format.FormatD16Unorm,
            RHIFormat.D32_Float => Format.FormatD32Float,
            RHIFormat.D16_UNorm => Format.FormatD16Unorm,
            RHIFormat.R32G8X24_Typeless => Format.FormatD32FloatS8X24Uint,
            RHIFormat.D32_Float_S8X24_UInt => Format.FormatD32FloatS8X24Uint,
            RHIFormat.R32_Float_X8X24_Typeless => Format.FormatD32FloatS8X24Uint,
            RHIFormat.X32_Typeless_G8X24_UInt => Format.FormatD32FloatS8X24Uint,
            RHIFormat.R24G8_Typeless => Format.FormatD24UnormS8Uint,
            RHIFormat.D24_UNorm_S8_UInt => Format.FormatD24UnormS8Uint,
            RHIFormat.R24_UNorm_X8_Typeless => Format.FormatD24UnormS8Uint,
            RHIFormat.X24_Typeless_G8_UInt => Format.FormatD24UnormS8Uint,
            _ => Format.FormatUnknown,
        };

        public static Format ToResourceViewFormat(this RHIFormat format) => format switch
        {
            RHIFormat.Unknown => Format.FormatUnknown,
            RHIFormat.RGBA32_Float => Format.FormatR32G32B32A32Float,
            RHIFormat.RGBA32_UInt => Format.FormatR32G32B32A32Uint,
            RHIFormat.RGBA32_SInt => Format.FormatR32G32B32A32Sint,
            RHIFormat.RGB32_Float => Format.FormatR32G32B32Float,
            RHIFormat.RGB32_UInt => Format.FormatR32G32B32Uint,
            RHIFormat.RGB32_SInt => Format.FormatR32G32B32Sint,
            RHIFormat.RG32_Float => Format.FormatR32G32Float,
            RHIFormat.RG32_UInt => Format.FormatR32G32Uint,
            RHIFormat.RG32_SInt => Format.FormatR32G32Sint,
            RHIFormat.R32_Float => Format.FormatR32Float,
            RHIFormat.R32_UInt => Format.FormatR32Uint,
            RHIFormat.R32_SInt => Format.FormatR32Sint,
            RHIFormat.RGBA16_Float => Format.FormatR16G16B16A16Float,
            RHIFormat.RGBA16_UNorm => Format.FormatR16G16B16A16Unorm,
            RHIFormat.RGBA16_UInt => Format.FormatR16G16B16A16Uint,
            RHIFormat.RGBA16_SNorm => Format.FormatR16G16B16A16SNorm,
            RHIFormat.RGBA16_SInt => Format.FormatR16G16B16A16Sint,
            RHIFormat.RG16_Float => Format.FormatR16G16Float,
            RHIFormat.RG16_UNorm => Format.FormatR16G16Unorm,
            RHIFormat.RG16_UInt => Format.FormatR16G16Uint,
            RHIFormat.RG16_SNorm => Format.FormatR16G16SNorm,
            RHIFormat.RG16_SInt => Format.FormatR16G16Sint,
            RHIFormat.R16_Float => Format.FormatR16Float,
            RHIFormat.R16_UInt => Format.FormatR16Uint,
            RHIFormat.R16_SNorm => Format.FormatR16SNorm,
            RHIFormat.R16_SInt => Format.FormatR16Sint,
            RHIFormat.RGBA8_UNorm => Format.FormatR8G8B8A8Unorm,
            RHIFormat.RGBA8_UNorm_sRGB => Format.FormatR8G8B8A8UnormSrgb,
            RHIFormat.RGBA8_UInt => Format.FormatR8G8B8A8Uint,
            RHIFormat.RGBA8_SNorm => Format.FormatR8G8B8A8SNorm,
            RHIFormat.RGBA8_SInt => Format.FormatR8G8B8A8Sint,
            RHIFormat.RG8_UNorm => Format.FormatR8G8Unorm,
            RHIFormat.RG8_UInt => Format.FormatR8G8Uint,
            RHIFormat.RG8_SNorm => Format.FormatR8G8SNorm,
            RHIFormat.RG8_SInt => Format.FormatR8G8Sint,
            RHIFormat.R8_UNorm => Format.FormatR8Unorm,
            RHIFormat.R8_UInt => Format.FormatR8Uint,
            RHIFormat.R8_SNorm => Format.FormatR8SNorm,
            RHIFormat.R8_SInt => Format.FormatR8Sint,
            RHIFormat.RGB10A2_Typeless => Format.FormatR10G10B10A2Typeless,
            RHIFormat.RGB10A2_UNorm => Format.FormatR10G10B10A2Unorm,
            RHIFormat.RGB10A2_UInt => Format.FormatR10G10B10A2Uint,
            RHIFormat.RG11B10_Float => Format.FormatR11G11B10Float,
            RHIFormat.R32G8X24_Typeless => Format.FormatR32G8X24Typeless,
            RHIFormat.R32_Float_X8X24_Typeless => Format.FormatR32FloatX8X24Typeless,
            RHIFormat.X32_Typeless_G8X24_UInt => Format.FormatX32TypelessG8X24Uint,
            RHIFormat.R24G8_Typeless => Format.FormatR24G8Typeless,
            RHIFormat.R24_UNorm_X8_Typeless => Format.FormatR24UnormX8Typeless,
            RHIFormat.X24_Typeless_G8_UInt => Format.FormatX24TypelessG8Uint,
            RHIFormat.BC1_Typeless => Format.FormatBC1Typeless,
            RHIFormat.BC1_UNorm => Format.FormatBC1Unorm,
            RHIFormat.BC1_UNorm_sRGB => Format.FormatBC1UnormSrgb,
            RHIFormat.BC2_Typeless => Format.FormatBC2Typeless,
            RHIFormat.BC2_UNorm => Format.FormatBC2Unorm,
            RHIFormat.BC2_UNorm_sRGB => Format.FormatBC2UnormSrgb,
            RHIFormat.BC3_Typeless => Format.FormatBC3Typeless,
            RHIFormat.BC3_UNorm => Format.FormatBC3Unorm,
            RHIFormat.BC3_UNorm_sRGB => Format.FormatBC3UnormSrgb,
            RHIFormat.BC4_Typeless => Format.FormatBC4Typeless,
            RHIFormat.BC4_UNorm => Format.FormatBC4Unorm,
            RHIFormat.BC4_SNorm => Format.FormatBC4SNorm,
            RHIFormat.BC5_Typeless => Format.FormatBC5Typeless,
            RHIFormat.BC5_UNorm => Format.FormatBC5Unorm,
            RHIFormat.BC5_SNorm => Format.FormatBC5SNorm,
            RHIFormat.BC6H_Typeless => Format.FormatBC6HTypeless,
            RHIFormat.BC6H_UFloat16 => Format.FormatBC6HUF16,
            RHIFormat.BC6H_SFloat16 => Format.FormatBC6HSF16,
            RHIFormat.BC7_Typeless => Format.FormatBC7Typeless,
            RHIFormat.BC7_UNorm => Format.FormatBC7Unorm,
            RHIFormat.BC7_UNorm_sRGB => Format.FormatBC7UnormSrgb,
            _ => Format.FormatUnknown,
        };

        public static ResourceDimension ToResourceDimension(this RHIDimension dimension) => dimension switch
        {
            RHIDimension.Texture1D => ResourceDimension.Texture1D,
            RHIDimension.Texture2D => ResourceDimension.Texture2D,
            RHIDimension.Texture3D => ResourceDimension.Texture3D,
            _ => ResourceDimension.Unknown,
        };

        public static Format ToSwapChainFormat(this RHIFormat format) => format switch
        {
            RHIFormat.RGBA8_UNorm => Format.FormatR8G8B8A8Unorm,
            RHIFormat.RGBA8_UNorm_sRGB => Format.FormatR8G8B8A8UnormSrgb,
            RHIFormat.RGB10A2_UNorm => Format.FormatR10G10B10A2Unorm,
            _ => Format.FormatUnknown,
        };

        public static RHIFormat ToDepthFormat(this RHIFormat format) => format switch
        {
            RHIFormat.D16_UNorm => RHIFormat.R16_Float,
            RHIFormat.D24_UNorm_S8_UInt => RHIFormat.R24_UNorm_X8_Typeless,
            RHIFormat.D32_Float => RHIFormat.R32_Float,
            RHIFormat.D32_Float_S8X24_UInt => RHIFormat.R32_Float_X8X24_Typeless,
            _ => RHIFormat.Unknown,
        };

        public static RHIFormat ToStencilFormat(this RHIFormat format) => format switch
        {
            RHIFormat.D24_UNorm_S8_UInt => RHIFormat.X24_Typeless_G8_UInt,
            RHIFormat.D32_Float_S8X24_UInt => RHIFormat.X32_Typeless_G8X24_UInt,
            RHIFormat.X24_Typeless_G8_UInt => RHIFormat.X24_Typeless_G8_UInt,
            RHIFormat.X32_Typeless_G8X24_UInt => RHIFormat.X32_Typeless_G8X24_UInt,
            _ => RHIFormat.Unknown,
        };

        public static FillMode ToFillMode(this RHIFillMode fillMode) => fillMode switch
        {
            RHIFillMode.Solid => FillMode.Solid,
            RHIFillMode.Wireframe => FillMode.Wireframe,
            _ => FillMode.Solid
        };

        public static CullMode ToCullMode(this RHICullMode cullMode) => cullMode switch
        {
            RHICullMode.None => CullMode.None,
            RHICullMode.Back => CullMode.Back,
            RHICullMode.Front => CullMode.Front,
            _ => CullMode.Back,
        };

        public static DepthWriteMask ToDepthWriteMask(this RHIDepthWriteMask depthWriteMask) => depthWriteMask switch
        {
            RHIDepthWriteMask.None => DepthWriteMask.Zero,
            RHIDepthWriteMask.All => DepthWriteMask.All,
            _ => DepthWriteMask.All
        };

        public static ComparisonFunc ToComparisonFunc(this RHIComparisonFunction comparisonFunction) => comparisonFunction switch
        {
            RHIComparisonFunction.None => ComparisonFunc.None,
            RHIComparisonFunction.Never => ComparisonFunc.Never,
            RHIComparisonFunction.Less => ComparisonFunc.Less,
            RHIComparisonFunction.Equal => ComparisonFunc.Equal,
            RHIComparisonFunction.LessEqual => ComparisonFunc.LessEqual,
            RHIComparisonFunction.Greater => ComparisonFunc.Greater,
            RHIComparisonFunction.NotEqual => ComparisonFunc.NotEqual,
            RHIComparisonFunction.GreaterEqual => ComparisonFunc.GreaterEqual,
            RHIComparisonFunction.Always => ComparisonFunc.Always,
            _ => ComparisonFunc.None,
        };

        public static StencilOp ToStencilOp(this RHIStencilOperation stencilOperation) => stencilOperation switch
        {
            RHIStencilOperation.Keep => StencilOp.Keep,
            RHIStencilOperation.Zero => StencilOp.Zero,
            RHIStencilOperation.Replace => StencilOp.Replace,
            RHIStencilOperation.IncrSaturation => StencilOp.IncrSat,
            RHIStencilOperation.DecrSatuaration => StencilOp.DecrSat,
            RHIStencilOperation.Invert => StencilOp.Invert,
            RHIStencilOperation.Increment => StencilOp.Incr,
            RHIStencilOperation.Decrement => StencilOp.Decr,
            _ => StencilOp.Keep,
        };

        public static PrimitiveTopologyType ToPrimitiveTopologyType(this RHIPrimitiveTopologyType primitiveTopologyType) => primitiveTopologyType switch
        {
            RHIPrimitiveTopologyType.Triangle => PrimitiveTopologyType.Triangle,
            RHIPrimitiveTopologyType.Line => PrimitiveTopologyType.Line,
            RHIPrimitiveTopologyType.Point => PrimitiveTopologyType.Point,
            RHIPrimitiveTopologyType.Patch => PrimitiveTopologyType.Patch,
            _ => PrimitiveTopologyType.Triangle,
        };

        public static Blend ToBlend(this RHIBlend blend) => blend switch
        {
            RHIBlend.Zero => Blend.Zero,
            RHIBlend.One => Blend.One,
            RHIBlend.SrcColor => Blend.SrcColor,
            RHIBlend.InvSrcColor => Blend.InvSrcColor,
            RHIBlend.SrcAlpha => Blend.SrcAlpha,
            RHIBlend.InvSrcAlpha => Blend.InvSrcAlpha,
            RHIBlend.DestAlpha => Blend.DestAlpha,
            RHIBlend.InvDestAlpha => Blend.InvDestAlpha,
            RHIBlend.DestColor => Blend.DestColor,
            RHIBlend.InvDestColor => Blend.InvDestColor,
            RHIBlend.SrcAlphaSaturate => Blend.SrcAlphaSat,
            RHIBlend.BlendFactor => Blend.BlendFactor,
            RHIBlend.InvBlendFactor => Blend.InvBlendFactor,
            RHIBlend.Src1Color => Blend.Src1Color,
            RHIBlend.InvSrc1Color => Blend.InvSrc1Color,
            RHIBlend.Src1Alpha => Blend.Src1Alpha,
            RHIBlend.InvSrc1Alpha => Blend.InvSrc1Alpha,
            RHIBlend.AlphaFactor => Blend.AlphaFactor,
            RHIBlend.InvAlphaFactor => Blend.InvAlphaFactor,
            _ => Blend.Zero,
        };

        public static BlendOp ToBlendOp(this RHIBlendOperation blendOperation) => blendOperation switch
        {
            RHIBlendOperation.Add => BlendOp.Add,
            RHIBlendOperation.Subtract => BlendOp.Subtract,
            RHIBlendOperation.ReverseSubtract => BlendOp.RevSubtract,
            RHIBlendOperation.Minimum => BlendOp.Max,
            RHIBlendOperation.Maximum => BlendOp.Min,
            _ => BlendOp.Add,
        };

        public static Format ToFormat(this RHIElementFormat elementFormat) => elementFormat switch
        {
            RHIElementFormat.Single1 => Format.FormatR32Float,
            RHIElementFormat.Single2 => Format.FormatR32G32Float,
            RHIElementFormat.Single3 => Format.FormatR32G32B32Float,
            RHIElementFormat.Single4 => Format.FormatR32G32B32A32Float,
            RHIElementFormat.UInt1 => Format.FormatR32Uint,
            RHIElementFormat.UInt2 => Format.FormatR32G32Uint,
            RHIElementFormat.UInt3 => Format.FormatR32G32B32Uint,
            RHIElementFormat.UInt4 => Format.FormatR32G32B32A32Uint,
            RHIElementFormat.Byte4 => Format.FormatR8G8B8A8Unorm,
            _ => Format.FormatUnknown,
        };

        public static TextureAddressMode ToTextureAddressMode(this RHITextureAddressMode textureAddressMode) => textureAddressMode switch
        {
            RHITextureAddressMode.Repeat => TextureAddressMode.Wrap,
            RHITextureAddressMode.Mirror => TextureAddressMode.Mirror,
            RHITextureAddressMode.Clamp => TextureAddressMode.Clamp,
            RHITextureAddressMode.Border => TextureAddressMode.Border,
            RHITextureAddressMode.MirrorOnce => TextureAddressMode.MirrorOnce,
            _ => TextureAddressMode.Wrap,
        };

        public static StaticBorderColor ToStaticBorderColor(this RHISamplerBorder samplerBorder) => samplerBorder switch
        {
            RHISamplerBorder.TransparentBlack => StaticBorderColor.TransparentBlack,
            RHISamplerBorder.OpaqueBlack => StaticBorderColor.OpaqueBlack,
            RHISamplerBorder.OpaqueWhite => StaticBorderColor.OpaqueWhite,
            RHISamplerBorder.OpaqueBlackUInt => StaticBorderColor.OpaqueBlackUint,
            RHISamplerBorder.OpaqueWhiteUInt => StaticBorderColor.OpaqueWhiteUint,
            _ => StaticBorderColor.TransparentBlack,
        };

        public static InputClassification ToInputClass(this RHIInputClass inputClass) => inputClass switch
        {
            RHIInputClass.PerVertex => InputClassification.PerVertexData,
            RHIInputClass.PerInstance => InputClassification.PerInstanceData,
            _ => InputClassification.PerVertexData,
        };
    }
}
