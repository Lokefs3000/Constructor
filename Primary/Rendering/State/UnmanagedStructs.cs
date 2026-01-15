using Primary.Assets;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Rendering.State
{
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

    internal struct CmdSetDepthStencil
    {
        public CmdDataResource Texture;
    }

    internal struct CmdSetViewport
    {
        public byte Slot;
        public FGViewport Viewport;
    }

    internal struct CmdSetScissor
    {
        public byte Slot;
        public FGRect Scissor;
    }

    internal struct CmdSetStencilRef
    {
        public uint StencilRef;
    }

    internal struct CmdSetVertexBuffer
    {
        public CmdDataResource Resource;
        public ushort Stride;
    }

    internal struct CmdSetIndexBuffer
    {
        public CmdDataResource Resource;
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
        public int DataSizeRequired;
    }
}
