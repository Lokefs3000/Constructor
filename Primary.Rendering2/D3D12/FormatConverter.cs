using Primary.Rendering2.Resources;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;

using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_FILTER;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_ADDRESS_MODE;
using static TerraFX.Interop.DirectX.D3D12_COMPARISON_FUNC;

namespace Primary.Rendering2.D3D12
{
    [SupportedOSPlatform("windows")]
    internal static class FormatConverter
    {
        internal static DXGI_FORMAT ToDXGIFormat(FGTextureFormat format) => format switch
        {
            FGTextureFormat.RGBA32_Typeless => DXGI_FORMAT_R32G32B32A32_TYPELESS,
            FGTextureFormat.RGBA32_Float => DXGI_FORMAT_R32G32B32A32_FLOAT,
            FGTextureFormat.RGBA32_UInt => DXGI_FORMAT_R32G32B32A32_UINT,
            FGTextureFormat.RGBA32_SInt => DXGI_FORMAT_R32G32B32A32_SINT,
            FGTextureFormat.RGB32_Typeless => DXGI_FORMAT_R32G32B32_TYPELESS,
            FGTextureFormat.RGB32_Float => DXGI_FORMAT_R32G32B32_FLOAT,
            FGTextureFormat.RGB32_UInt => DXGI_FORMAT_R32G32B32_UINT,
            FGTextureFormat.RGB32_SInt => DXGI_FORMAT_R32G32B32_SINT,
            FGTextureFormat.RGBA16_Typeless => DXGI_FORMAT_R16G16B16A16_TYPELESS,
            FGTextureFormat.RGBA16_Float => DXGI_FORMAT_R16G16B16A16_FLOAT,
            FGTextureFormat.RGBA16_UInt => DXGI_FORMAT_R16G16B16A16_UINT,
            FGTextureFormat.RGBA16_SInt => DXGI_FORMAT_R16G16B16A16_SINT,
            FGTextureFormat.RG32_Typeless => DXGI_FORMAT_R32G32_TYPELESS,
            FGTextureFormat.RG32_Float => DXGI_FORMAT_R32G32_FLOAT,
            FGTextureFormat.RG32_UInt => DXGI_FORMAT_R32G32_UINT,
            FGTextureFormat.RG32_SInt => DXGI_FORMAT_R32G32_SINT,
            FGTextureFormat.R32_Float_X8X24_Typeless => DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
            FGTextureFormat.X32_Typeless_G8X24_UInt => DXGI_FORMAT_X32_TYPELESS_G8X24_UINT,
            FGTextureFormat.R32G8X24_Typeless => DXGI_FORMAT_R32G8X24_TYPELESS,
            FGTextureFormat.RGB10A2_Typeless => DXGI_FORMAT_R10G10B10A2_TYPELESS,
            FGTextureFormat.RGB10A2_UNorm => DXGI_FORMAT_R10G10B10A2_UNORM,
            FGTextureFormat.RGB10A2_UInt => DXGI_FORMAT_R10G10B10A2_UINT,
            FGTextureFormat.RG11B10_Float => DXGI_FORMAT_R11G11B10_FLOAT,
            FGTextureFormat.RGBA8_Typeless => DXGI_FORMAT_R8G8B8A8_TYPELESS,
            FGTextureFormat.RGBA8_UNorm => DXGI_FORMAT_R8G8B8A8_UNORM,
            FGTextureFormat.RGBA8_UNorm_sRGB => DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
            FGTextureFormat.RGBA8_UInt => DXGI_FORMAT_R8G8B8A8_UINT,
            FGTextureFormat.RGBA8_SNorm => DXGI_FORMAT_R8G8B8A8_SNORM,
            FGTextureFormat.RGBA8_SInt => DXGI_FORMAT_R8G8B8A8_SINT,
            FGTextureFormat.RG16_Typeless => DXGI_FORMAT_R16G16_TYPELESS,
            FGTextureFormat.RG16_Float => DXGI_FORMAT_R16G16_FLOAT,
            FGTextureFormat.RG16_UNorm => DXGI_FORMAT_R16G16_UNORM,
            FGTextureFormat.RG16_UInt => DXGI_FORMAT_R16G16_UINT,
            FGTextureFormat.RG16_SNorm => DXGI_FORMAT_R16G16_SNORM,
            FGTextureFormat.RG16_SInt => DXGI_FORMAT_R16G16_SINT,
            FGTextureFormat.R32_Typeless => DXGI_FORMAT_R32_TYPELESS,
            FGTextureFormat.R32_Float => DXGI_FORMAT_R32_FLOAT,
            FGTextureFormat.R32_UInt => DXGI_FORMAT_R32_UINT,
            FGTextureFormat.R32_SInt => DXGI_FORMAT_R32_SINT,
            FGTextureFormat.R24G8_Typeless => DXGI_FORMAT_R24G8_TYPELESS,
            FGTextureFormat.R24_UNorm_X8_Typeless => DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
            FGTextureFormat.X24_Typeless_G8_UInt => DXGI_FORMAT_X24_TYPELESS_G8_UINT,
            FGTextureFormat.R8_Typeless => DXGI_FORMAT_R8_TYPELESS,
            FGTextureFormat.R8_UNorm => DXGI_FORMAT_R8_UNORM,
            FGTextureFormat.R8_UInt => DXGI_FORMAT_R8_UINT,
            FGTextureFormat.R8_SNorm => DXGI_FORMAT_R8_SNORM,
            FGTextureFormat.R8_SInt => DXGI_FORMAT_R8_SINT,
            FGTextureFormat.A8_UNorm => DXGI_FORMAT_A8_UNORM,
            FGTextureFormat.R16_Typeless => DXGI_FORMAT_R16_TYPELESS,
            FGTextureFormat.R16_Float => DXGI_FORMAT_R16_FLOAT,
            FGTextureFormat.R16_UNorm => DXGI_FORMAT_R16_UNORM,
            FGTextureFormat.R16_UInt => DXGI_FORMAT_R16_UINT,
            FGTextureFormat.R16_SNorm => DXGI_FORMAT_R16_SNORM,
            FGTextureFormat.R16_SInt => DXGI_FORMAT_R16_SINT,
            FGTextureFormat.RG8_UNorm => DXGI_FORMAT_R8G8_UNORM,
            FGTextureFormat.RG8_UInt => DXGI_FORMAT_R8G8_UINT,
            FGTextureFormat.RG8_SNorm => DXGI_FORMAT_R8G8_SNORM,
            FGTextureFormat.RG8_SInt => DXGI_FORMAT_R8G8_SINT,
            FGTextureFormat.BC1_Typeless => DXGI_FORMAT_BC1_TYPELESS,
            FGTextureFormat.BC1_UNorm => DXGI_FORMAT_BC1_UNORM,
            FGTextureFormat.BC1_Unorm_sRGB => DXGI_FORMAT_BC1_UNORM_SRGB,
            FGTextureFormat.BC2_Typeless => DXGI_FORMAT_BC2_TYPELESS,
            FGTextureFormat.BC2_UNorm => DXGI_FORMAT_BC2_UNORM,
            FGTextureFormat.BC2_UNorm_sRGB => DXGI_FORMAT_BC2_UNORM_SRGB,
            FGTextureFormat.BC3_Typeless => DXGI_FORMAT_BC3_TYPELESS,
            FGTextureFormat.BC3_UNorm => DXGI_FORMAT_BC3_UNORM,
            FGTextureFormat.BC3_UNorm_sRGB => DXGI_FORMAT_BC3_UNORM_SRGB,
            FGTextureFormat.BC4_Typeless => DXGI_FORMAT_BC4_TYPELESS,
            FGTextureFormat.BC4_UNorm => DXGI_FORMAT_BC4_UNORM,
            FGTextureFormat.BC4_SNorm => DXGI_FORMAT_BC4_SNORM,
            FGTextureFormat.BC5_Typeless => DXGI_FORMAT_BC5_TYPELESS,
            FGTextureFormat.BC5_UNorm => DXGI_FORMAT_BC5_UNORM,
            FGTextureFormat.BC5_SNorm => DXGI_FORMAT_BC5_SNORM,
            FGTextureFormat.BC6H_Typeless => DXGI_FORMAT_BC6H_TYPELESS,
            FGTextureFormat.BC6H_UFloat16 => DXGI_FORMAT_BC6H_UF16,
            FGTextureFormat.BC6H_SFloat16 => DXGI_FORMAT_BC6H_SF16,
            FGTextureFormat.BC7_Typeless => DXGI_FORMAT_BC7_TYPELESS,
            FGTextureFormat.BC7_UNorm => DXGI_FORMAT_BC7_UNORM,
            FGTextureFormat.D32_Float_S8X24_UInt => DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
            FGTextureFormat.D32_Float => DXGI_FORMAT_D32_FLOAT,
            FGTextureFormat.D24_UNorm_S8_UInt => DXGI_FORMAT_D24_UNORM_S8_UINT,
            FGTextureFormat.D16_UNorm => DXGI_FORMAT_D16_UNORM,
            _ => throw new NotImplementedException(),
        };

        internal static D3D12_RESOURCE_DIMENSION ToResourceDimension(FGTextureDimension dimension) => dimension switch
        {
            FGTextureDimension._1D => D3D12_RESOURCE_DIMENSION_TEXTURE1D,
            FGTextureDimension._2D => D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            FGTextureDimension._3D => D3D12_RESOURCE_DIMENSION_TEXTURE3D,
            _ => throw new NotImplementedException(),
        };

        internal static RHI.RenderTargetFormat ToRTVFormat(FGTextureFormat format) => format switch
        {
            FGTextureFormat.Undefined => RHI.RenderTargetFormat.Undefined,
            FGTextureFormat.RGBA32_Float => RHI.RenderTargetFormat.RGBA32sf,
            FGTextureFormat.RGBA32_UInt => RHI.RenderTargetFormat.RGBA32ui,
            FGTextureFormat.RGBA32_SInt => RHI.RenderTargetFormat.RGBA32si,
            FGTextureFormat.RGB32_Float => RHI.RenderTargetFormat.RGB32sf,
            FGTextureFormat.RGB32_UInt => RHI.RenderTargetFormat.RGB32ui,
            FGTextureFormat.RGB32_SInt => RHI.RenderTargetFormat.RGB32si,
            FGTextureFormat.RGBA16_Float => RHI.RenderTargetFormat.RGBA16sf,
            FGTextureFormat.RGBA16_UInt => RHI.RenderTargetFormat.RGBA16ui,
            FGTextureFormat.RGBA16_SInt => RHI.RenderTargetFormat.RGBA16si,
            FGTextureFormat.RG32_Float => RHI.RenderTargetFormat.RG32sf,
            FGTextureFormat.RG32_UInt => RHI.RenderTargetFormat.RG32ui,
            FGTextureFormat.RG32_SInt => RHI.RenderTargetFormat.RG32si,
            FGTextureFormat.RGB10A2_UNorm => RHI.RenderTargetFormat.RGB10A2un,
            FGTextureFormat.RGB10A2_UInt => RHI.RenderTargetFormat.RGB10A2ui,
            FGTextureFormat.RG11B10_Float => RHI.RenderTargetFormat.RG11B10sf,
            FGTextureFormat.RGBA8_UNorm => RHI.RenderTargetFormat.RGBA8un,
            FGTextureFormat.RGBA8_UNorm_sRGB => RHI.RenderTargetFormat.RGBA8un_sRGB,
            FGTextureFormat.RGBA8_UInt => RHI.RenderTargetFormat.RGBA8ui,
            FGTextureFormat.RGBA8_SNorm => RHI.RenderTargetFormat.RGBA8sn,
            FGTextureFormat.RGBA8_SInt => RHI.RenderTargetFormat.RGBA8si,
            FGTextureFormat.RG16_Float => RHI.RenderTargetFormat.RG16sf,
            FGTextureFormat.RG16_UNorm => RHI.RenderTargetFormat.RG16un,
            FGTextureFormat.RG16_UInt => RHI.RenderTargetFormat.RG16ui,
            FGTextureFormat.RG16_SNorm => RHI.RenderTargetFormat.RG16sn,
            FGTextureFormat.RG16_SInt => RHI.RenderTargetFormat.RG16si,
            FGTextureFormat.R32_Float => RHI.RenderTargetFormat.R32sf,
            FGTextureFormat.R32_UInt => RHI.RenderTargetFormat.R32ui,
            FGTextureFormat.R32_SInt => RHI.RenderTargetFormat.R32si,
            FGTextureFormat.R8_UNorm => RHI.RenderTargetFormat.R8un,
            FGTextureFormat.R8_UInt => RHI.RenderTargetFormat.R8ui,
            FGTextureFormat.R8_SNorm => RHI.RenderTargetFormat.R8sn,
            FGTextureFormat.R8_SInt => RHI.RenderTargetFormat.R8si,
            FGTextureFormat.A8_UNorm => RHI.RenderTargetFormat.A8un,
            FGTextureFormat.R16_Float => RHI.RenderTargetFormat.R16sf,
            FGTextureFormat.R16_UNorm => RHI.RenderTargetFormat.R16un,
            FGTextureFormat.R16_UInt => RHI.RenderTargetFormat.R16ui,
            FGTextureFormat.R16_SNorm => RHI.RenderTargetFormat.R16sn,
            FGTextureFormat.R16_SInt => RHI.RenderTargetFormat.R16si,
            FGTextureFormat.RG8_UNorm => RHI.RenderTargetFormat.R8un,
            FGTextureFormat.RG8_UInt => RHI.RenderTargetFormat.R8ui,
            FGTextureFormat.RG8_SNorm => RHI.RenderTargetFormat.R8sn,
            FGTextureFormat.RG8_SInt => RHI.RenderTargetFormat.R8si,
            _ => throw new NotImplementedException(),
        };

        internal static RHI.DepthStencilFormat ToDSVFormat(FGTextureFormat format) => format switch
        {
            FGTextureFormat.D32_Float_S8X24_UInt => RHI.DepthStencilFormat.D32sfS8X24ui,
            FGTextureFormat.D32_Float => RHI.DepthStencilFormat.D32sf,
            FGTextureFormat.D24_UNorm_S8_UInt => RHI.DepthStencilFormat.D24unS8ui,
            FGTextureFormat.D16_UNorm => RHI.DepthStencilFormat.D16un,
            FGTextureFormat.R32G8X24_Typeless => RHI.DepthStencilFormat.R32tX8X24ui,
            FGTextureFormat.R24_Typeless_X8_UInt => RHI.DepthStencilFormat.R24tX8ui,
            FGTextureFormat.R16_Typeless => RHI.DepthStencilFormat.R16t,
            FGTextureFormat.R32_Typeless => RHI.DepthStencilFormat.R32t,
            _ => throw new NotImplementedException(),
        };

        internal static D3D12_FILTER ToFilter(RHI.TextureFilter filter) => filter switch
        {
            RHI.TextureFilter.Point => D3D12_FILTER_MIN_MAG_MIP_POINT,
            RHI.TextureFilter.MinMagPointMipLinear => D3D12_FILTER_MIN_MAG_POINT_MIP_LINEAR,
            RHI.TextureFilter.MinPointMagLinearMipPoint => D3D12_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT,
            RHI.TextureFilter.MinPointMagMipLinear => D3D12_FILTER_MIN_POINT_MAG_MIP_LINEAR,
            RHI.TextureFilter.MinLinearMagMipPoint => D3D12_FILTER_MIN_LINEAR_MAG_MIP_POINT,
            RHI.TextureFilter.MinLinearMagPointMipLinear => D3D12_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR,
            RHI.TextureFilter.MinMagLinearMipPoint => D3D12_FILTER_MIN_MAG_LINEAR_MIP_POINT,
            RHI.TextureFilter.Linear => D3D12_FILTER_MIN_MAG_MIP_LINEAR,
            RHI.TextureFilter.Anisotropic => D3D12_FILTER_ANISOTROPIC,
            _ => throw new NotImplementedException(),
        };

        internal static D3D12_TEXTURE_ADDRESS_MODE ToAddressMode(RHI.TextureAddressMode addressMode) => addressMode switch
        {
            RHI.TextureAddressMode.Repeat => D3D12_TEXTURE_ADDRESS_MODE_WRAP,
            RHI.TextureAddressMode.Mirror => D3D12_TEXTURE_ADDRESS_MODE_MIRROR,
            RHI.TextureAddressMode.ClampToEdge => D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
            RHI.TextureAddressMode.ClampToBorder => D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            _ => throw new NotImplementedException(),
        };

        internal static D3D12_COMPARISON_FUNC ToComparisonFunc(RHI.ComparisonFunc func) => func switch
        {
            RHI.ComparisonFunc.None => D3D12_COMPARISON_FUNC_NONE,
            RHI.ComparisonFunc.Never => D3D12_COMPARISON_FUNC_NEVER,
            RHI.ComparisonFunc.Less => D3D12_COMPARISON_FUNC_LESS,
            RHI.ComparisonFunc.Equal => D3D12_COMPARISON_FUNC_EQUAL,
            RHI.ComparisonFunc.LessEqual => D3D12_COMPARISON_FUNC_LESS_EQUAL,
            RHI.ComparisonFunc.Greater => D3D12_COMPARISON_FUNC_GREATER,
            RHI.ComparisonFunc.NotEqual => D3D12_COMPARISON_FUNC_NOT_EQUAL,
            RHI.ComparisonFunc.GreaterEqual => D3D12_COMPARISON_FUNC_GREATER_EQUAL,
            RHI.ComparisonFunc.Always => D3D12_COMPARISON_FUNC_ALWAYS,
            _ => throw new NotImplementedException(),
        };
    }
}
