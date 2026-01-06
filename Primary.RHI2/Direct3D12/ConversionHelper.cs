using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.DirectX;

using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_FILL_MODE;
using static TerraFX.Interop.DirectX.D3D12_CULL_MODE;
using static TerraFX.Interop.DirectX.D3D12_DEPTH_WRITE_MASK;
using static TerraFX.Interop.DirectX.D3D12_COMPARISON_FUNC;
using static TerraFX.Interop.DirectX.D3D12_STENCIL_OP;
using static TerraFX.Interop.DirectX.D3D12_BLEND;
using static TerraFX.Interop.DirectX.D3D12_BLEND_OP;
using static TerraFX.Interop.DirectX.D3D12_PRIMITIVE_TOPOLOGY_TYPE;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_ADDRESS_MODE;
using static TerraFX.Interop.DirectX.D3D12_STATIC_BORDER_COLOR;
using static TerraFX.Interop.DirectX.D3D12_INPUT_CLASSIFICATION;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    public unsafe static class ConversionHelper
    {
        public static DXGI_FORMAT ToTextureFormat(this RHIFormat format) => format switch
        {
            RHIFormat.Unknown => DXGI_FORMAT_UNKNOWN,
            RHIFormat.RGBA32_Typeless => DXGI_FORMAT_R32G32B32A32_TYPELESS,
            RHIFormat.RGBA32_Float => DXGI_FORMAT_R32G32B32A32_FLOAT,
            RHIFormat.RGBA32_UInt => DXGI_FORMAT_R32G32B32A32_UINT,
            RHIFormat.RGBA32_SInt => DXGI_FORMAT_R32G32B32A32_SINT,
            RHIFormat.RGB32_Typeless => DXGI_FORMAT_R32G32B32_TYPELESS,
            RHIFormat.RGB32_Float => DXGI_FORMAT_R32G32B32_FLOAT,
            RHIFormat.RGB32_UInt => DXGI_FORMAT_R32G32B32_UINT,
            RHIFormat.RGB32_SInt => DXGI_FORMAT_R32G32B32_SINT,
            RHIFormat.RG32_Typeless => DXGI_FORMAT_R32G32_TYPELESS,
            RHIFormat.RG32_Float => DXGI_FORMAT_R32G32_FLOAT,
            RHIFormat.RG32_UInt => DXGI_FORMAT_R32G32_UINT,
            RHIFormat.RG32_SInt => DXGI_FORMAT_R32G32_SINT,
            RHIFormat.R32_Typeless => DXGI_FORMAT_R32_TYPELESS,
            RHIFormat.R32_Float => DXGI_FORMAT_R32_FLOAT,
            RHIFormat.R32_UInt => DXGI_FORMAT_R32_UINT,
            RHIFormat.R32_SInt => DXGI_FORMAT_R32_SINT,
            RHIFormat.RGBA16_Typeless => DXGI_FORMAT_R16G16B16A16_TYPELESS,
            RHIFormat.RGBA16_Float => DXGI_FORMAT_R16G16B16A16_FLOAT,
            RHIFormat.RGBA16_UNorm => DXGI_FORMAT_R16G16B16A16_UNORM,
            RHIFormat.RGBA16_UInt => DXGI_FORMAT_R16G16B16A16_UINT,
            RHIFormat.RGBA16_SNorm => DXGI_FORMAT_R16G16B16A16_SNORM,
            RHIFormat.RGBA16_SInt => DXGI_FORMAT_R16G16B16A16_SINT,
            RHIFormat.RG16_Typeless => DXGI_FORMAT_R16G16_TYPELESS,
            RHIFormat.RG16_Float => DXGI_FORMAT_R16G16_FLOAT,
            RHIFormat.RG16_UNorm => DXGI_FORMAT_R16G16_UNORM,
            RHIFormat.RG16_UInt => DXGI_FORMAT_R16G16_UINT,
            RHIFormat.RG16_SNorm => DXGI_FORMAT_R16G16_SNORM,
            RHIFormat.RG16_SInt => DXGI_FORMAT_R16G16_SINT,
            RHIFormat.R16_Typeless => DXGI_FORMAT_R16_TYPELESS,
            RHIFormat.R16_Float => DXGI_FORMAT_R16_FLOAT,
            RHIFormat.R16_UInt => DXGI_FORMAT_R16_UINT,
            RHIFormat.R16_SNorm => DXGI_FORMAT_R16_SNORM,
            RHIFormat.R16_SInt => DXGI_FORMAT_R16_SINT,
            RHIFormat.RGBA8_Typeless => DXGI_FORMAT_R8G8B8A8_TYPELESS,
            RHIFormat.RGBA8_UNorm => DXGI_FORMAT_R8G8B8A8_UNORM,
            RHIFormat.RGBA8_UNorm_sRGB => DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
            RHIFormat.RGBA8_UInt => DXGI_FORMAT_R8G8B8A8_UINT,
            RHIFormat.RGBA8_SNorm => DXGI_FORMAT_R8G8B8A8_SNORM,
            RHIFormat.RGBA8_SInt => DXGI_FORMAT_R8G8B8A8_SINT,
            RHIFormat.RG8_Typeless => DXGI_FORMAT_R8G8_TYPELESS,
            RHIFormat.RG8_UNorm => DXGI_FORMAT_R8G8_UNORM,
            RHIFormat.RG8_UInt => DXGI_FORMAT_R8G8_UINT,
            RHIFormat.RG8_SNorm => DXGI_FORMAT_R8G8_SNORM,
            RHIFormat.RG8_SInt => DXGI_FORMAT_R8G8_SINT,
            RHIFormat.R8_Typeless => DXGI_FORMAT_R8_TYPELESS,
            RHIFormat.R8_UNorm => DXGI_FORMAT_R8_UNORM,
            RHIFormat.R8_UInt => DXGI_FORMAT_R8_UINT,
            RHIFormat.R8_SNorm => DXGI_FORMAT_R8_SNORM,
            RHIFormat.R8_SInt => DXGI_FORMAT_R8_SINT,
            RHIFormat.RGB10A2_Typeless => DXGI_FORMAT_R10G10B10A2_TYPELESS,
            RHIFormat.RGB10A2_UNorm => DXGI_FORMAT_R10G10B10A2_UNORM,
            RHIFormat.RGB10A2_UInt => DXGI_FORMAT_R10G10B10A2_UINT,
            RHIFormat.RG11B10_Float => DXGI_FORMAT_R11G11B10_FLOAT,
            RHIFormat.D32_Float => DXGI_FORMAT_D32_FLOAT,
            RHIFormat.D16_UNorm => DXGI_FORMAT_D16_UNORM,
            RHIFormat.R32G8X24_Typeless => DXGI_FORMAT_R32G8X24_TYPELESS,
            RHIFormat.D32_Float_S8X24_UInt => DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
            RHIFormat.R32_Float_X8X24_Typeless => DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
            RHIFormat.X32_Typeless_G8X24_UInt => DXGI_FORMAT_X32_TYPELESS_G8X24_UINT,
            RHIFormat.R24G8_Typeless => DXGI_FORMAT_R24G8_TYPELESS,
            RHIFormat.D24_UNorm_S8_UInt => DXGI_FORMAT_D24_UNORM_S8_UINT,
            RHIFormat.R24_UNorm_X8_Typeless => DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
            RHIFormat.X24_Typeless_G8_UInt => DXGI_FORMAT_X24_TYPELESS_G8_UINT,
            RHIFormat.BC1_Typeless => DXGI_FORMAT_BC1_TYPELESS,
            RHIFormat.BC1_UNorm => DXGI_FORMAT_BC1_UNORM,
            RHIFormat.BC1_UNorm_sRGB => DXGI_FORMAT_BC1_UNORM_SRGB,
            RHIFormat.BC2_Typeless => DXGI_FORMAT_BC2_TYPELESS,
            RHIFormat.BC2_UNorm => DXGI_FORMAT_BC2_UNORM,
            RHIFormat.BC2_UNorm_sRGB => DXGI_FORMAT_BC2_UNORM_SRGB,
            RHIFormat.BC3_Typeless => DXGI_FORMAT_BC3_TYPELESS,
            RHIFormat.BC3_UNorm => DXGI_FORMAT_BC3_UNORM,
            RHIFormat.BC3_UNorm_sRGB => DXGI_FORMAT_BC3_UNORM_SRGB,
            RHIFormat.BC4_Typeless => DXGI_FORMAT_BC4_TYPELESS,
            RHIFormat.BC4_UNorm => DXGI_FORMAT_BC4_UNORM,
            RHIFormat.BC4_SNorm => DXGI_FORMAT_BC4_SNORM,
            RHIFormat.BC5_Typeless => DXGI_FORMAT_BC5_TYPELESS,
            RHIFormat.BC5_UNorm => DXGI_FORMAT_BC5_UNORM,
            RHIFormat.BC5_SNorm => DXGI_FORMAT_BC5_SNORM,
            RHIFormat.BC6H_Typeless => DXGI_FORMAT_BC6H_TYPELESS,
            RHIFormat.BC6H_UFloat16 => DXGI_FORMAT_BC6H_UF16,
            RHIFormat.BC6H_SFloat16 => DXGI_FORMAT_BC6H_SF16,
            RHIFormat.BC7_Typeless => DXGI_FORMAT_BC7_TYPELESS,
            RHIFormat.BC7_UNorm => DXGI_FORMAT_BC7_UNORM,
            RHIFormat.BC7_UNorm_sRGB => DXGI_FORMAT_BC7_UNORM_SRGB,
            _ => DXGI_FORMAT_UNKNOWN,
        };

        public static DXGI_FORMAT ToRenderTargetFormat(this RHIFormat format) => format switch
        {
            RHIFormat.RGBA32_Float => DXGI_FORMAT_R32G32B32A32_FLOAT,
            RHIFormat.RGBA32_UInt => DXGI_FORMAT_R32G32B32A32_UINT,
            RHIFormat.RGBA32_SInt => DXGI_FORMAT_R32G32B32A32_SINT,
            RHIFormat.RGB32_Float => DXGI_FORMAT_R32G32B32_FLOAT,
            RHIFormat.RGB32_UInt => DXGI_FORMAT_R32G32B32_UINT,
            RHIFormat.RGB32_SInt => DXGI_FORMAT_R32G32B32_SINT,
            RHIFormat.RG32_Float => DXGI_FORMAT_R32G32_FLOAT,
            RHIFormat.RG32_UInt => DXGI_FORMAT_R32G32_UINT,
            RHIFormat.RG32_SInt => DXGI_FORMAT_R32G32_SINT,
            RHIFormat.R32_Float => DXGI_FORMAT_R32_FLOAT,
            RHIFormat.R32_UInt => DXGI_FORMAT_R32_UINT,
            RHIFormat.R32_SInt => DXGI_FORMAT_R32_SINT,
            RHIFormat.RGBA16_Typeless => DXGI_FORMAT_R16G16B16A16_TYPELESS,
            RHIFormat.RGBA16_Float => DXGI_FORMAT_R16G16B16A16_FLOAT,
            RHIFormat.RGBA16_UNorm => DXGI_FORMAT_R16G16B16A16_UNORM,
            RHIFormat.RGBA16_UInt => DXGI_FORMAT_R16G16B16A16_UINT,
            RHIFormat.RGBA16_SNorm => DXGI_FORMAT_R16G16B16A16_SNORM,
            RHIFormat.RGBA16_SInt => DXGI_FORMAT_R16G16B16A16_SINT,
            RHIFormat.RG16_Typeless => DXGI_FORMAT_R16G16_TYPELESS,
            RHIFormat.RG16_Float => DXGI_FORMAT_R16G16_FLOAT,
            RHIFormat.RG16_UNorm => DXGI_FORMAT_R16G16_UNORM,
            RHIFormat.RG16_UInt => DXGI_FORMAT_R16G16_UINT,
            RHIFormat.RG16_SNorm => DXGI_FORMAT_R16G16_SNORM,
            RHIFormat.RG16_SInt => DXGI_FORMAT_R16G16_SINT,
            RHIFormat.R16_Typeless => DXGI_FORMAT_R16_TYPELESS,
            RHIFormat.R16_Float => DXGI_FORMAT_R16_FLOAT,
            RHIFormat.R16_UInt => DXGI_FORMAT_R16_UINT,
            RHIFormat.R16_SNorm => DXGI_FORMAT_R16_SNORM,
            RHIFormat.R16_SInt => DXGI_FORMAT_R16_SINT,
            RHIFormat.RGBA8_Typeless => DXGI_FORMAT_R8G8B8A8_TYPELESS,
            RHIFormat.RGBA8_UNorm => DXGI_FORMAT_R8G8B8A8_UNORM,
            RHIFormat.RGBA8_UNorm_sRGB => DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
            RHIFormat.RGBA8_UInt => DXGI_FORMAT_R8G8B8A8_UINT,
            RHIFormat.RGBA8_SNorm => DXGI_FORMAT_R8G8B8A8_SNORM,
            RHIFormat.RGBA8_SInt => DXGI_FORMAT_R8G8B8A8_SINT,
            RHIFormat.RG8_Typeless => DXGI_FORMAT_R8G8_TYPELESS,
            RHIFormat.RG8_UNorm => DXGI_FORMAT_R8G8_UNORM,
            RHIFormat.RG8_UInt => DXGI_FORMAT_R8G8_UINT,
            RHIFormat.RG8_SNorm => DXGI_FORMAT_R8G8_SNORM,
            RHIFormat.RG8_SInt => DXGI_FORMAT_R8G8_SINT,
            RHIFormat.R8_Typeless => DXGI_FORMAT_R8_TYPELESS,
            RHIFormat.R8_UNorm => DXGI_FORMAT_R8_UNORM,
            RHIFormat.R8_UInt => DXGI_FORMAT_R8_UINT,
            RHIFormat.R8_SNorm => DXGI_FORMAT_R8_SNORM,
            RHIFormat.R8_SInt => DXGI_FORMAT_R8_SINT,
            RHIFormat.RGB10A2_Typeless => DXGI_FORMAT_R10G10B10A2_TYPELESS,
            RHIFormat.RGB10A2_UNorm => DXGI_FORMAT_R10G10B10A2_UNORM,
            RHIFormat.RGB10A2_UInt => DXGI_FORMAT_R10G10B10A2_UINT,
            RHIFormat.RG11B10_Float => DXGI_FORMAT_R11G11B10_FLOAT,
            _ => DXGI_FORMAT_UNKNOWN,
        };

        public static DXGI_FORMAT ToDepthStencilFormat(this RHIFormat format) => format switch
        {
            RHIFormat.R32_Typeless => DXGI_FORMAT_D32_FLOAT,
            RHIFormat.R16_Typeless => DXGI_FORMAT_D16_UNORM,
            RHIFormat.D32_Float => DXGI_FORMAT_D32_FLOAT,
            RHIFormat.D16_UNorm => DXGI_FORMAT_D16_UNORM,
            RHIFormat.R32G8X24_Typeless => DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
            RHIFormat.D32_Float_S8X24_UInt => DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
            RHIFormat.R32_Float_X8X24_Typeless => DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
            RHIFormat.X32_Typeless_G8X24_UInt => DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
            RHIFormat.R24G8_Typeless => DXGI_FORMAT_D24_UNORM_S8_UINT,
            RHIFormat.D24_UNorm_S8_UInt => DXGI_FORMAT_D24_UNORM_S8_UINT,
            RHIFormat.R24_UNorm_X8_Typeless => DXGI_FORMAT_D24_UNORM_S8_UINT,
            RHIFormat.X24_Typeless_G8_UInt => DXGI_FORMAT_D24_UNORM_S8_UINT,
            _ => DXGI_FORMAT_UNKNOWN,
        };

        public static DXGI_FORMAT ToResourceViewFormat(this RHIFormat format) => format switch
        {
            RHIFormat.Unknown => DXGI_FORMAT_UNKNOWN,
            RHIFormat.RGBA32_Float => DXGI_FORMAT_R32G32B32A32_FLOAT,
            RHIFormat.RGBA32_UInt => DXGI_FORMAT_R32G32B32A32_UINT,
            RHIFormat.RGBA32_SInt => DXGI_FORMAT_R32G32B32A32_SINT,
            RHIFormat.RGB32_Float => DXGI_FORMAT_R32G32B32_FLOAT,
            RHIFormat.RGB32_UInt => DXGI_FORMAT_R32G32B32_UINT,
            RHIFormat.RGB32_SInt => DXGI_FORMAT_R32G32B32_SINT,
            RHIFormat.RG32_Float => DXGI_FORMAT_R32G32_FLOAT,
            RHIFormat.RG32_UInt => DXGI_FORMAT_R32G32_UINT,
            RHIFormat.RG32_SInt => DXGI_FORMAT_R32G32_SINT,
            RHIFormat.R32_Float => DXGI_FORMAT_R32_FLOAT,
            RHIFormat.R32_UInt => DXGI_FORMAT_R32_UINT,
            RHIFormat.R32_SInt => DXGI_FORMAT_R32_SINT,
            RHIFormat.RGBA16_Float => DXGI_FORMAT_R16G16B16A16_FLOAT,
            RHIFormat.RGBA16_UNorm => DXGI_FORMAT_R16G16B16A16_UNORM,
            RHIFormat.RGBA16_UInt => DXGI_FORMAT_R16G16B16A16_UINT,
            RHIFormat.RGBA16_SNorm => DXGI_FORMAT_R16G16B16A16_SNORM,
            RHIFormat.RGBA16_SInt => DXGI_FORMAT_R16G16B16A16_SINT,
            RHIFormat.RG16_Float => DXGI_FORMAT_R16G16_FLOAT,
            RHIFormat.RG16_UNorm => DXGI_FORMAT_R16G16_UNORM,
            RHIFormat.RG16_UInt => DXGI_FORMAT_R16G16_UINT,
            RHIFormat.RG16_SNorm => DXGI_FORMAT_R16G16_SNORM,
            RHIFormat.RG16_SInt => DXGI_FORMAT_R16G16_SINT,
            RHIFormat.R16_Float => DXGI_FORMAT_R16_FLOAT,
            RHIFormat.R16_UInt => DXGI_FORMAT_R16_UINT,
            RHIFormat.R16_SNorm => DXGI_FORMAT_R16_SNORM,
            RHIFormat.R16_SInt => DXGI_FORMAT_R16_SINT,
            RHIFormat.RGBA8_UNorm => DXGI_FORMAT_R8G8B8A8_UNORM,
            RHIFormat.RGBA8_UNorm_sRGB => DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
            RHIFormat.RGBA8_UInt => DXGI_FORMAT_R8G8B8A8_UINT,
            RHIFormat.RGBA8_SNorm => DXGI_FORMAT_R8G8B8A8_SNORM,
            RHIFormat.RGBA8_SInt => DXGI_FORMAT_R8G8B8A8_SINT,
            RHIFormat.RG8_UNorm => DXGI_FORMAT_R8G8_UNORM,
            RHIFormat.RG8_UInt => DXGI_FORMAT_R8G8_UINT,
            RHIFormat.RG8_SNorm => DXGI_FORMAT_R8G8_SNORM,
            RHIFormat.RG8_SInt => DXGI_FORMAT_R8G8_SINT,
            RHIFormat.R8_UNorm => DXGI_FORMAT_R8_UNORM,
            RHIFormat.R8_UInt => DXGI_FORMAT_R8_UINT,
            RHIFormat.R8_SNorm => DXGI_FORMAT_R8_SNORM,
            RHIFormat.R8_SInt => DXGI_FORMAT_R8_SINT,
            RHIFormat.RGB10A2_Typeless => DXGI_FORMAT_R10G10B10A2_TYPELESS,
            RHIFormat.RGB10A2_UNorm => DXGI_FORMAT_R10G10B10A2_UNORM,
            RHIFormat.RGB10A2_UInt => DXGI_FORMAT_R10G10B10A2_UINT,
            RHIFormat.RG11B10_Float => DXGI_FORMAT_R11G11B10_FLOAT,
            RHIFormat.R32G8X24_Typeless => DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
            RHIFormat.R32_Float_X8X24_Typeless => DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
            RHIFormat.X32_Typeless_G8X24_UInt => DXGI_FORMAT_X32_TYPELESS_G8X24_UINT,
            RHIFormat.R24G8_Typeless => DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
            RHIFormat.R24_UNorm_X8_Typeless => DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
            RHIFormat.X24_Typeless_G8_UInt => DXGI_FORMAT_X24_TYPELESS_G8_UINT,
            RHIFormat.BC1_Typeless => DXGI_FORMAT_BC1_TYPELESS,
            RHIFormat.BC1_UNorm => DXGI_FORMAT_BC1_UNORM,
            RHIFormat.BC1_UNorm_sRGB => DXGI_FORMAT_BC1_UNORM_SRGB,
            RHIFormat.BC2_Typeless => DXGI_FORMAT_BC2_TYPELESS,
            RHIFormat.BC2_UNorm => DXGI_FORMAT_BC2_UNORM,
            RHIFormat.BC2_UNorm_sRGB => DXGI_FORMAT_BC2_UNORM_SRGB,
            RHIFormat.BC3_Typeless => DXGI_FORMAT_BC3_TYPELESS,
            RHIFormat.BC3_UNorm => DXGI_FORMAT_BC3_UNORM,
            RHIFormat.BC3_UNorm_sRGB => DXGI_FORMAT_BC3_UNORM_SRGB,
            RHIFormat.BC4_Typeless => DXGI_FORMAT_BC4_TYPELESS,
            RHIFormat.BC4_UNorm => DXGI_FORMAT_BC4_UNORM,
            RHIFormat.BC4_SNorm => DXGI_FORMAT_BC4_SNORM,
            RHIFormat.BC5_Typeless => DXGI_FORMAT_BC5_TYPELESS,
            RHIFormat.BC5_UNorm => DXGI_FORMAT_BC5_UNORM,
            RHIFormat.BC5_SNorm => DXGI_FORMAT_BC5_SNORM,
            RHIFormat.BC6H_Typeless => DXGI_FORMAT_BC6H_TYPELESS,
            RHIFormat.BC6H_UFloat16 => DXGI_FORMAT_BC6H_UF16,
            RHIFormat.BC6H_SFloat16 => DXGI_FORMAT_BC6H_SF16,
            RHIFormat.BC7_Typeless => DXGI_FORMAT_BC7_TYPELESS,
            RHIFormat.BC7_UNorm => DXGI_FORMAT_BC7_UNORM,
            RHIFormat.BC7_UNorm_sRGB => DXGI_FORMAT_BC7_UNORM_SRGB,
            _ => DXGI_FORMAT_UNKNOWN,
        };

        public static D3D12_RESOURCE_DIMENSION ToResourceDimension(this RHIDimension dimension) => dimension switch
        {
            RHIDimension.Texture1D => D3D12_RESOURCE_DIMENSION_TEXTURE1D,
            RHIDimension.Texture2D => D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            RHIDimension.Texture3D => D3D12_RESOURCE_DIMENSION_TEXTURE3D,
            _ => D3D12_RESOURCE_DIMENSION_UNKNOWN,
        };

        public static DXGI_FORMAT ToSwapChainFormat(this RHIFormat format) => format switch
        {
            RHIFormat.RGBA8_UNorm => DXGI_FORMAT_R8G8B8A8_UNORM,
            RHIFormat.RGBA8_UNorm_sRGB => DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
            _ => DXGI_FORMAT_UNKNOWN,
        };

        public static D3D12_FILL_MODE ToFillMode(this RHIFillMode fillMode) => fillMode switch
        {
            RHIFillMode.Solid => D3D12_FILL_MODE_SOLID,
            RHIFillMode.Wireframe => D3D12_FILL_MODE_WIREFRAME,
            _ => D3D12_FILL_MODE_SOLID
        };

        public static D3D12_CULL_MODE ToCullMode(this RHICullMode cullMode) => cullMode switch
        {
            RHICullMode.None => D3D12_CULL_MODE_NONE,
            RHICullMode.Back => D3D12_CULL_MODE_BACK,
            RHICullMode.Front => D3D12_CULL_MODE_FRONT,
            _ => D3D12_CULL_MODE_BACK,
        };

        public static D3D12_DEPTH_WRITE_MASK ToDepthWriteMask(this RHIDepthWriteMask depthWriteMask) => depthWriteMask switch
        {
            RHIDepthWriteMask.None => D3D12_DEPTH_WRITE_MASK_ZERO,
            RHIDepthWriteMask.All => D3D12_DEPTH_WRITE_MASK_ALL,
            _ => D3D12_DEPTH_WRITE_MASK_ALL
        };

        public static D3D12_COMPARISON_FUNC ToComparisonFunc(this RHIComparisonFunction comparisonFunction) => comparisonFunction switch
        {
            RHIComparisonFunction.None => D3D12_COMPARISON_FUNC_NONE,
            RHIComparisonFunction.Never => D3D12_COMPARISON_FUNC_NEVER,
            RHIComparisonFunction.Less => D3D12_COMPARISON_FUNC_LESS,
            RHIComparisonFunction.Equal => D3D12_COMPARISON_FUNC_EQUAL,
            RHIComparisonFunction.LessEqual => D3D12_COMPARISON_FUNC_LESS_EQUAL,
            RHIComparisonFunction.Greater => D3D12_COMPARISON_FUNC_GREATER,
            RHIComparisonFunction.NotEqual => D3D12_COMPARISON_FUNC_NOT_EQUAL,
            RHIComparisonFunction.GreaterEqual => D3D12_COMPARISON_FUNC_GREATER_EQUAL,
            RHIComparisonFunction.Always => D3D12_COMPARISON_FUNC_ALWAYS,
            _ => D3D12_COMPARISON_FUNC_NEVER,
        };

        public static D3D12_STENCIL_OP ToStencilOp(this RHIStencilOperation stencilOperation) => stencilOperation switch
        {
            RHIStencilOperation.Keep => D3D12_STENCIL_OP_KEEP,
            RHIStencilOperation.Zero => D3D12_STENCIL_OP_ZERO,
            RHIStencilOperation.Replace => D3D12_STENCIL_OP_REPLACE,
            RHIStencilOperation.IncrSaturation => D3D12_STENCIL_OP_INCR_SAT,
            RHIStencilOperation.DecrSatuaration => D3D12_STENCIL_OP_DECR_SAT,
            RHIStencilOperation.Invert => D3D12_STENCIL_OP_INVERT,
            RHIStencilOperation.Increment => D3D12_STENCIL_OP_INCR,
            RHIStencilOperation.Decrement => D3D12_STENCIL_OP_DECR,
            _ => D3D12_STENCIL_OP_KEEP,
        };

        public static D3D12_PRIMITIVE_TOPOLOGY_TYPE ToPrimitiveTopologyType(this RHIPrimitiveTopologyType primitiveTopologyType) => primitiveTopologyType switch
        {
            RHIPrimitiveTopologyType.Triangle => D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
            RHIPrimitiveTopologyType.Line => D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE,
            RHIPrimitiveTopologyType.Point => D3D12_PRIMITIVE_TOPOLOGY_TYPE_POINT,
            RHIPrimitiveTopologyType.Patch => D3D12_PRIMITIVE_TOPOLOGY_TYPE_PATCH,
            _ => D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
        };

        public static D3D12_BLEND ToBlend(this RHIBlend blend) => blend switch
        {
            RHIBlend.Zero => D3D12_BLEND_ZERO,
            RHIBlend.One => D3D12_BLEND_ONE,
            RHIBlend.SrcColor => D3D12_BLEND_SRC_COLOR,
            RHIBlend.InvSrcColor => D3D12_BLEND_INV_SRC_COLOR,
            RHIBlend.SrcAlpha => D3D12_BLEND_SRC_ALPHA,
            RHIBlend.InvSrcAlpha => D3D12_BLEND_INV_SRC_ALPHA,
            RHIBlend.DestAlpha => D3D12_BLEND_DEST_ALPHA,
            RHIBlend.InvDestAlpha => D3D12_BLEND_INV_DEST_ALPHA,
            RHIBlend.DestColor => D3D12_BLEND_DEST_COLOR,
            RHIBlend.InvDestColor => D3D12_BLEND_INV_DEST_COLOR,
            RHIBlend.SrcAlphaSaturate => D3D12_BLEND_SRC_ALPHA_SAT,
            RHIBlend.BlendFactor => D3D12_BLEND_BLEND_FACTOR,
            RHIBlend.InvBlendFactor => D3D12_BLEND_INV_BLEND_FACTOR,
            RHIBlend.Src1Color => D3D12_BLEND_SRC1_COLOR,
            RHIBlend.InvSrc1Color => D3D12_BLEND_INV_SRC1_COLOR,
            RHIBlend.Src1Alpha => D3D12_BLEND_SRC1_ALPHA,
            RHIBlend.InvSrc1Alpha => D3D12_BLEND_INV_SRC1_ALPHA,
            RHIBlend.AlphaFactor => D3D12_BLEND_ALPHA_FACTOR,
            RHIBlend.InvAlphaFactor => D3D12_BLEND_INV_ALPHA_FACTOR,
            _ => D3D12_BLEND_ZERO,
        };

        public static D3D12_BLEND_OP ToBlendOp(this RHIBlendOperation blendOperation) => blendOperation switch
        {
            RHIBlendOperation.Add => D3D12_BLEND_OP_ADD,
            RHIBlendOperation.Subtract => D3D12_BLEND_OP_SUBTRACT,
            RHIBlendOperation.ReverseSubtract => D3D12_BLEND_OP_REV_SUBTRACT,
            RHIBlendOperation.Minimum => D3D12_BLEND_OP_MAX,
            RHIBlendOperation.Maximum => D3D12_BLEND_OP_MIN,
            _ => D3D12_BLEND_OP_ADD,
        };

        public static DXGI_FORMAT ToFormat(this RHIElementFormat elementFormat) => elementFormat switch
        {
            RHIElementFormat.Single1 => DXGI_FORMAT_R32_FLOAT,
            RHIElementFormat.Single2 => DXGI_FORMAT_R32G32_FLOAT,
            RHIElementFormat.Single3 => DXGI_FORMAT_R32G32B32_FLOAT,
            RHIElementFormat.Single4 => DXGI_FORMAT_R32G32B32A32_FLOAT,
            RHIElementFormat.Byte4 => DXGI_FORMAT_R8G8B8A8_UNORM,
            _ => DXGI_FORMAT_UNKNOWN,
        };

        public static D3D12_TEXTURE_ADDRESS_MODE ToTextureAddressMode(this RHITextureAddressMode textureAddressMode) => textureAddressMode switch
        {
            RHITextureAddressMode.Repeat => D3D12_TEXTURE_ADDRESS_MODE_WRAP,
            RHITextureAddressMode.Mirror => D3D12_TEXTURE_ADDRESS_MODE_MIRROR,
            RHITextureAddressMode.Clamp => D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
            RHITextureAddressMode.Border => D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            RHITextureAddressMode.MirrorOnce => D3D12_TEXTURE_ADDRESS_MODE_MIRROR_ONCE,
            _ => D3D12_TEXTURE_ADDRESS_MODE_WRAP,
        };

        public static D3D12_STATIC_BORDER_COLOR ToStaticBorderColor(this RHISamplerBorder samplerBorder) => samplerBorder switch
        {
            RHISamplerBorder.TransparentBlack => D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK,
            RHISamplerBorder.OpaqueBlack => D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK,
            RHISamplerBorder.OpaqueWhite => D3D12_STATIC_BORDER_COLOR_OPAQUE_WHITE,
            RHISamplerBorder.OpaqueBlackUInt => D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK_UINT,
            RHISamplerBorder.OpaqueWhiteUInt => D3D12_STATIC_BORDER_COLOR_OPAQUE_WHITE_UINT,
            _ => D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK,
        };

        public static D3D12_INPUT_CLASSIFICATION ToInputClass(this RHIInputClass inputClass) => inputClass switch
        {
            RHIInputClass.PerVertex => D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            RHIInputClass.PerInstance => D3D12_INPUT_CLASSIFICATION_PER_INSTANCE_DATA,
            _ => D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
        };
    }
}
