using CommunityToolkit.HighPerformance;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.Rendering.Structures;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Silk.NET.Core.Native;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe static class ResourceUtility
    {
        internal static int GetBufferSize(NRDResource buffer, ResourceManager resources) => (int)(buffer.IsExternal ?
            ((D3D12RHIBufferNative*)buffer.Native)->Base.Description.Width :
            resources.FindFGBuffer(buffer).Description.Width);

        internal static int GetTextureRowPitch(NRDResource texture, ResourceManager resources)
        {
            if (texture.IsExternal)
            {
                D3D12RHITextureNative* native = (D3D12RHITextureNative*)texture.Native;
                return RHIFormatInfo.Query(native->Base.Description.Format).BytesPerPixel * native->Base.Description.Width;
            }
            else
            {
                FrameGraphTexture fg = resources.FindFGTexture(texture);
                return RHIFormatInfo.Query(fg.Description.Format).BytesPerPixel * fg.Description.Width;
            }
        }

        internal static RHIFormat GetTextureFormat(NRDResource texture, ResourceManager resources)
        {
            if (texture.Id != NRDResourceId.Texture)
                return RHIFormat.Unknown;

            return texture.IsExternal ?
                ((D3D12RHITextureNative*)texture.Native)->Base.Description.Format :
                resources.FindFGTexture(texture).Description.Format;
        }

        internal static FGBox GetTextureBox(NRDResource texture, ResourceManager resources)
        {
            if (texture.IsExternal)
            {
                D3D12RHITextureNative* native = (D3D12RHITextureNative*)texture.Native;
                return new FGBox(0, 0, 0,
                    native->Base.Description.Width,
                    native->Base.Description.Height,
                    native->Base.Description.Dimension == RHIDimension.Texture3D ? native->Base.Description.Depth : 1);
            }
            else
            {
                FrameGraphTexture fg = resources.FindFGTexture(texture);
                return new FGBox(0, 0, 0,
                    fg.Description.Width,
                    fg.Description.Height,
                    fg.Description.Dimension == FGTextureDimension._3D ? fg.Description.Depth : 1);
            }
        }

        internal static bool DoesTextureNeedInit(NRDResource texture, ResourceManager resources)
        {
            if (texture.Id != NRDResourceId.Texture)
                return false;

            return (texture.IsExternal ? ((D3D12RHITextureNative*)texture.Native)->Base.Description.Format : resources.FindFGTexture(texture).Description.Format) switch
            {
                RHIFormat.RGBA32_Float => true,
                RHIFormat.RGBA32_UInt => true,
                RHIFormat.RGBA32_SInt => true,
                RHIFormat.RGB32_Float => true,
                RHIFormat.RGB32_UInt => true,
                RHIFormat.RGB32_SInt => true,
                RHIFormat.RG32_Float => true,
                RHIFormat.RG32_UInt => true,
                RHIFormat.RG32_SInt => true,
                RHIFormat.R32_Float => true,
                RHIFormat.R32_UInt => true,
                RHIFormat.R32_SInt => true,
                RHIFormat.RGBA16_Typeless => true,
                RHIFormat.RGBA16_Float => true,
                RHIFormat.RGBA16_UNorm => true,
                RHIFormat.RGBA16_UInt => true,
                RHIFormat.RGBA16_SNorm => true,
                RHIFormat.RGBA16_SInt => true,
                RHIFormat.RG16_Typeless => true,
                RHIFormat.RG16_Float => true,
                RHIFormat.RG16_UNorm => true,
                RHIFormat.RG16_UInt => true,
                RHIFormat.RG16_SNorm => true,
                RHIFormat.RG16_SInt => true,
                RHIFormat.R16_Typeless => true,
                RHIFormat.R16_Float => true,
                RHIFormat.R16_UInt => true,
                RHIFormat.R16_SNorm => true,
                RHIFormat.R16_SInt => true,
                RHIFormat.RGBA8_Typeless => true,
                RHIFormat.RGBA8_UNorm => true,
                RHIFormat.RGBA8_UNorm_sRGB => true,
                RHIFormat.RGBA8_UInt => true,
                RHIFormat.RGBA8_SNorm => true,
                RHIFormat.RGBA8_SInt => true,
                RHIFormat.RG8_Typeless => true,
                RHIFormat.RG8_UNorm => true,
                RHIFormat.RG8_UInt => true,
                RHIFormat.RG8_SNorm => true,
                RHIFormat.RG8_SInt => true,
                RHIFormat.R8_Typeless => true,
                RHIFormat.R8_UNorm => true,
                RHIFormat.R8_UInt => true,
                RHIFormat.R8_SNorm => true,
                RHIFormat.R8_SInt => true,
                RHIFormat.RGB10A2_Typeless => true,
                RHIFormat.RGB10A2_UNorm => true,
                RHIFormat.RGB10A2_UInt => true,
                RHIFormat.RG11B10_Float => true,
                RHIFormat.R32_Typeless => true,
                RHIFormat.D32_Float => true,
                RHIFormat.D16_UNorm => true,
                RHIFormat.R32G8X24_Typeless => true,
                RHIFormat.D32_Float_S8X24_UInt => true,
                RHIFormat.R32_Float_X8X24_Typeless => true,
                RHIFormat.X32_Typeless_G8X24_UInt => true,
                RHIFormat.R24G8_Typeless => true,
                RHIFormat.D24_UNorm_S8_UInt => true,
                RHIFormat.R24_UNorm_X8_Typeless => true,
                RHIFormat.X24_Typeless_G8_UInt => true,
                _ => false
            };
        }

        internal static NRDResource GetNRDBufferResource(nint ptr, bool isExternal) => isExternal ?
            new NRDResource(((D3D12RHIBufferNative*)ptr.ToPointer())) :
            new NRDResource((int)ptr, NRDResourceId.Buffer);

        internal static NRDResource GetNRDTextureResource(nint ptr, bool isExternal) => isExternal ?
            new NRDResource(((D3D12RHITextureNative*)ptr.ToPointer())) :
            new NRDResource((int)ptr, NRDResourceId.Texture);

        internal static NRDResource AsNRDResource(FrameGraphResource resource) => resource.ResourceId switch
        {
            FGResourceId.Texture => resource.IsExternal ? new NRDResource((D3D12RHITextureNative*)Unsafe.As<D3D12RHITexture>(resource.Resource!).GetAsNative()) : new NRDResource(resource.Index, NRDResourceId.Texture),
            FGResourceId.Buffer => resource.IsExternal ? new NRDResource((D3D12RHIBufferNative*)Unsafe.As<D3D12RHIBuffer>(resource.Resource!).GetAsNative()) : new NRDResource(resource.Index, NRDResourceId.Buffer),
            _ => NRDResource.Null
        };

        internal static NRDResource AsNRDResource(CmdDataResource resource) => resource.Type switch
        {
            CmdResourceType.Buffer => resource.IsExternal ? new NRDResource((D3D12RHIBufferNative*)resource.Resource) : new NRDResource((int)resource.Resource, NRDResourceId.Buffer),
            CmdResourceType.Texture => resource.IsExternal ? new NRDResource((D3D12RHITextureNative*)resource.Resource) : new NRDResource((int)resource.Resource, NRDResourceId.Texture),
            CmdResourceType.Sampler => new NRDResource((D3D12RHISamplerNative*)resource.Resource),
            _ => NRDResource.Null
        };
    }
}
