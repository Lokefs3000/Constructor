using CommunityToolkit.HighPerformance;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

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

        internal static HRESULT SetResourceNameStack(ID3D12Resource* resource, string name)
        {
            Debug.Assert(name.Length + 1 < 1024);
            char* stack = stackalloc char[name.Length + 1];

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(stack), ref Unsafe.As<char, byte>(ref name.DangerousGetReference()), (uint)(name.Length + name.Length));
            stack[name.Length] = '\0';

            return resource->SetName(stack);
        }
    }
}
