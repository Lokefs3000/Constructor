using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Commands;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Rendering.State
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly record struct CmdDataResource(CmdResourceType Type, bool IsExternal, nint Resource)
    {
        public bool IsNull => IsExternal ? Resource == nint.Zero : ((nuint)Resource) == nuint.MaxValue;

        public static readonly CmdDataResource NullBuffer = new CmdDataResource(CmdResourceType.Buffer, false, (nint)nuint.MaxValue);
        public static readonly CmdDataResource NullTexture = new CmdDataResource(CmdResourceType.Texture, false, (nint)nuint.MaxValue);
        public static readonly CmdDataResource NullSampler = new CmdDataResource(CmdResourceType.Sampler, false, (nint)nuint.MaxValue);

        public static unsafe implicit operator CmdDataResource(FrameGraphBuffer buffer) => new CmdDataResource(CmdResourceType.Buffer, buffer.IsExternal, buffer.IsExternal ? (nint)Unsafe.As<RHIBuffer>(buffer.Resource!).GetAsNative() : buffer.Index);
        public static unsafe implicit operator CmdDataResource(FrameGraphTexture texture) => new CmdDataResource(CmdResourceType.Texture, texture.IsExternal, texture.IsExternal ? (nint)Unsafe.As<RHITexture>(texture.Resource!).GetAsNative() : texture.Index);
        public static unsafe implicit operator CmdDataResource(RHISampler sampler) => new CmdDataResource(CmdResourceType.Sampler, true, (nint)sampler.GetAsNative());
        public static unsafe implicit operator CmdDataResource(FrameGraphResource resource) => resource.ResourceId == FGResourceId.Texture ? resource.AsTexture() : resource.AsBuffer();
    }

    internal enum CmdResourceType : byte
    {
        Buffer = 0,
        Texture,
        Sampler
    }

    internal struct CmdSetRenderTarget
    {
        public byte Slot;
        public CmdDataResource Texture;
    }

    internal struct CmdCommitRenderTargets
    {
        public byte ActiveCount;
        public bool DeferSetState;
    }

    internal struct CmdSetDepthStencil
    {
        public CmdDataResource Texture;
    }

    internal struct CmdClearRenderTarget
    {
        public CmdDataResource Texture;
        public Color? Color;
        public FGRect? Rect;
    }

    internal struct CmdClearDepthStencil
    {
        public CmdDataResource Texture;
        public FGClearFlags ClearFlags;
        public float? Depth;
        public byte? Stencil;
        public FGRect? Rect;
    }

    internal struct CmdSetViewport
    {
        public byte Slot;
        public FGViewport Viewport;
    }

    internal struct CmdCommitViewports
    {
        public byte ActiveCount;
    }

    internal struct CmdSetScissor
    {
        public byte Slot;
        public FGRect Scissor;
    }

    internal struct CmdCommitScissors
    {
        public byte ActiveCount;
    }

    internal struct CmdSetStencilRef
    {
        public uint StencilRef;
    }

    internal struct CmdSetVertexBuffer
    {
        public CmdDataResource Resource;
        public uint BufferSize;
        public ushort Stride;
    }

    internal struct CmdSetIndexBuffer
    {
        public CmdDataResource Resource;
        public uint BufferSize;
        public ushort Stride;
    }

    internal struct CmdSetPipeline
    {
        public int Index;
    }

    internal struct CmdSetResource
    {
        public ShPropertyStages Stages;
        public ShPropertyFlags Flags;

        public int DataOffset;
        public CmdDataResource Resource;
    }

    internal struct CmdSetRawData
    {
        public int DataOffset;
        public int DataSize;
        public nint DataPointer;
    }

    internal struct CmdSetConstants
    {
        public int DataSize;
        public nint DataPointer;
    }

    internal struct CmdSetResourcesInfo
    {
        public ShHeaderFlags HeaderFlags;
        public int ConstantsSize;
        public int DataSizeRequired;
    }

    internal struct CmdDrawInstanced
    {
        public uint VertexCount;
        public uint InstanceCount;
        public uint StartVertex;
        public uint StartInstance;
    }

    internal struct CmdDrawIndexedInstanced
    {
        public uint IndexCount;
        public uint InstanceCount;
        public uint StartIndex;
        public int BaseVertex;
        public uint StartInstance;
    }

    internal struct CmdUploadBuffer
    {
        public int UploadIndex;

        public uint DataSize;
        public nint DataPointer;

        public uint BufferOffset;
    }

    internal struct CmdUploadTexture
    {
        public int UploadIndex;

        public uint DataSize;
        public uint DataRowPitch;
        public nint DataPointer;

        public FGBox? Box;
        public uint SubresourceIndex;
    }

    internal struct CmdCopyBuffer
    {
        public CmdDataResource Source;
        public uint SourceOffset;

        public CmdDataResource Destination;
        public uint DestinationOffset;

        public uint NumBytes;
    }

    internal struct CmdDataTextureFootprint
    {
        public uint Offset;

        public RHIFormat Format;

        public uint Width;
        public uint Height;
        public uint Depth;

        public uint RowPitch;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct CmdDataTextureSource
    {
        [FieldOffset(0)]
        public CmdDataResource Resource;

        [FieldOffset(11)]
        public CmdDataTextureSourceType Type;

        [FieldOffset(12)]
        public uint SubresourceIndex;

        [FieldOffset(12)]
        public CmdDataTextureFootprint Footprint;
    }

    internal enum CmdDataTextureSourceType : byte
    {
        SubresourceIndex = 0,
        Footprint
    }

    internal struct CmdCopyTexture
    {
        public CmdDataTextureSource Source;
        public FGBox? SourceBox;

        public CmdDataTextureSource Destination;
        public uint DstX;
        public uint DstY;
        public uint DstZ;
    }

    internal struct CmdDispatch
    {
        public uint ThreadGroupSizeX;
        public uint ThreadGroupSizeY;
        public uint ThreadGroupSizeZ;
    }

    internal struct CmdPresentOnWindow
    {
        public CmdDataResource Texture;
        public uint WindowId;
    }
}
