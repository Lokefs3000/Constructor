using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering.Datatypes;
using Primary.RHI;

namespace Primary.Rendering.Commands
{
    internal enum CmdCommandType : byte
    {
        Unknown = 0,

        ClearRenderTarget,
        ClearDepthStencil,
        ClearUAVFloat,
        ClearUAVUInt,
        SetRenderTarget,
        CommitRenderTargets,
        SetViewport,
        CommitViewports,
        SetScissor,
        CommitScissors,
        SetStencilRef,
        SetBlendFactor,
        SetPipeline,
        SetDescriptorHeap,
        DrawInstanced,
        DrawIndexedInstanced,
        CopyBuffer,
        CopyTexture,
        SetVertexBuffer,
        SetIndexBuffer,
        SetResourcesData,
        SetConstantsData,
        CommitShaderInput,
        SetSwapChain,
        UploadResource,
        Dispatch
    }

    internal struct CmdClearRenderTarget
    {
        public CPUDescriptorCmdData RenderTarget;
        public Color? ClearColor;
        public byte RectCount;
    }

    internal struct CmdClearDepthStencil
    {
        public CPUDescriptorCmdData DepthStencil;
        public float? Depth;
        public byte? Stencil;
        public byte RectCount;
    }

    internal struct CmdClearUAVFloat
    {
        public int ResourceIndex;
        public CPUDescriptorCmdData ResourceCPU;
        public GPUDescriptorCmdData ResourceGPU;
        public InlineArray4<float> Value;
        public byte RectCount;
    }

    internal struct CmdClearUAVUInt
    {
        public int ResourceIndex;
        public CPUDescriptorCmdData ResourceCPU;
        public GPUDescriptorCmdData ResourceGPU;
        public InlineArray4<uint> Value;
        public byte RectCount;
    }

    internal struct CmdSetRenderTarget
    {
        public RHIFormat RenderTargetFormat;
        public CPUDescriptorCmdData RenderTarget;
        public byte Slot;
    }

    internal struct CmdCommitRenderTargets
    {
        public RHIFormat DepthStencilFormat;
        public CPUDescriptorCmdData DepthStencil;
        public byte StartSlot;
        public byte NumRenderTargets;
    }

    internal struct CmdSetViewport
    {
        public byte Slot;
        public FGViewport Viewport;
    }

    internal struct CmdCommitViewports
    {
        public byte StartSlot;
        public byte NumViewportsToSet;
    }

    internal struct CmdSetScissor
    {
        public byte Slot;
        public FGRect Rect;
    }

    internal struct CmdCommitScissors
    {
        public byte StartSlot;
        public byte NumScissorsToSet;
    }

    internal struct CmdSetStencilRef
    {
        public uint StencilRef;
    }

    internal struct CmdSetBlendFactor
    {
        public Color Color;
    }

    internal struct CmdSetPipeline
    {
        public int PipelineIndex;
    }

    internal struct CmdSetDescriptorHeap
    {
        public int GPUHeapIndex;
        public int SamplerHeapIndex;
    }

    internal struct CmdDrawInstanced
    {
        public uint VertexCountPerInstance;
        public uint InstanceCount;
        public uint StartVertexLocation;
        public uint StartInstanceLocation;
    }

    internal struct CmdDrawIndexedInstanced
    {
        public uint IndexCountPerInstance;
        public uint InstanceCount;
        public uint StartIndexLocation;
        public int BaseVertexLocation;
        public uint StartInstanceLocation;
    }

    internal struct CmdCopyBuffer
    {
        public ResourceCmdData Source;
        public ResourceCmdData Destination;

        public ulong SourceOffset;
        public ulong DestinationOffset;
        public ulong NumBytes;
    }

    internal struct CmdCopyTexture
    {
        public ResourceCmdData Source;
        public ResourceCmdData Destination;

        public Int3 DestinationOffset;
        public FGBox? SourceBox;

        public Footprint SourceFootprint;
        public Footprint DestinationFootprint;

        [StructLayout(LayoutKind.Explicit)]
        internal struct Footprint
        {
            [FieldOffset(0)] public TextureCopyFootprint.Type Type;
            [FieldOffset(1)] public uint SubresourceIndex;
            [FieldOffset(1)] public PlacedFootprint PlacedFootprint;
        }

        internal struct PlacedFootprint
        {
            public ulong DataOffset;
            public RHIFormat Format;
            public ushort Width;
            public ushort Height;
            public ushort Depth;
            public uint RowPitch;
        }
    }

    internal struct CmdSetVertexBuffer
    {
        public ResourceCmdData Buffer;
        public uint ByteWidth;
        public ushort ByteStride;
    }

    internal struct CmdSetIndexBuffer
    {
        public ResourceCmdData Buffer;
        public uint ByteWidth;
        public ushort ByteStride;
    }

    internal struct CmdSetResourcesData
    {
        public ushort ResourcesByteOffset;
        public ushort ResourcesDataSize;
        public ushort DescriptorsByteOffset;
        public bool UseDedicatedBuffer;
    }

    internal struct CmdSetConstantsData
    {
        public ushort ConstantsDataSize;
    }

    internal struct CmdSetSwapChain
    {
        public int SwapChainIndex;
        public CPUDescriptorCmdData RenderTarget;
    }

    internal struct CmdUploadResource
    {
        public ResourceCmdData Resource;
        public uint Slice;
        public FGBox? Box;
        public int UploadIndex;
    }

    internal struct CmdDispatch
    {
        public ushort ThreadGroupCountX;
        public ushort ThreadGroupCountY;
        public ushort ThreadGroupCountZ;
    }
}
