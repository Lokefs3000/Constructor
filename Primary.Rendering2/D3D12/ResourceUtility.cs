using CommunityToolkit.HighPerformance;
using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Primary.Rendering2.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe static class ResourceUtility
    {
        internal static int GetBufferSize(FrameGraphBuffer buffer) => (int)(buffer.IsExternal ?
            Unsafe.As<RHI.Buffer>(buffer.Resource!).Description.ByteWidth :
            buffer.Description.Width);

        internal static FrameGraphTexture GetFGTexture(nint ptr, bool isExternal) => isExternal ?
            new FrameGraphResource(Unsafe.As<RHI.Texture>(RHI.Resource.FromIntPtr(ptr))!, null).AsTexture() :
            new FrameGraphTexture((int)ptr);

        internal static ID3D12Resource* GetNativeExternalResource(FrameGraphResource resource)
        {
            if (!resource.IsExternal)
                return null;

            return resource.ResourceId switch
            {
                FGResourceId.Texture => (ID3D12Resource*)((RHI.Direct3D12.TextureInternal)Unsafe.As<RHI.Texture>(resource.Resource)!).Resource.NativePointer.ToPointer(),
                FGResourceId.Buffer => (ID3D12Resource*)((RHI.Direct3D12.BufferInternal)Unsafe.As<RHI.Buffer>(resource.Resource)!).Resource.NativePointer.ToPointer(),
                _ => null
            };
        }

        internal static ID3D12Resource* GetResource(ResourceManager resourceManager, FrameGraphResource resource)
        {
            if (!resource.IsExternal)
                return (ID3D12Resource*)resourceManager.GetResource(resource);

            return resource.ResourceId switch
            {
                FGResourceId.Texture => (ID3D12Resource*)((RHI.Direct3D12.TextureInternal)Unsafe.As<RHI.Texture>(resource.Resource)!).Resource.NativePointer.ToPointer(),
                FGResourceId.Buffer => (ID3D12Resource*)((RHI.Direct3D12.BufferInternal)Unsafe.As<RHI.Buffer>(resource.Resource)!).Resource.NativePointer.ToPointer(),
                _ => null
            };
        }

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
