using Primary.Common;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Recording
{
    public interface IModificationCommand
    {

    }

    public struct ModificationCommandMeta
    {
        public RecCommandType Type;
    }

    public struct UCClearRenderTarget : IModificationCommand
    {
        public int RenderTarget;
        public FGRect? Rect;
    }

    public struct UCClearDepthStencil : IModificationCommand
    {
        public int DepthStencil;
        public FGClearFlags ClearFlags;
        public FGRect? Rect;
    }

    public struct UCClearRenderTargetCustom : IModificationCommand
    {
        public int RenderTarget;
        public FGRect? Rect;
        public Color Color;
    }

    public struct UCClearDepthStencilCustom : IModificationCommand
    {
        public int DepthStencil;
        public FGClearFlags ClearFlags;
        public FGRect? Rect;
        public float Depth;
        public byte Stencil;
    }

    public struct UCUploadBuffer : IModificationCommand
    {
        public int BufferUploadIndex;
        public nint DataPointer;
        public uint DataSize;
        public uint BufferOffset;
    }

    public struct UCUploadTexture : IModificationCommand
    {
        public int TextureUploadIndex;
        public FGBox? DestinationBox;
        public uint SubresourceIndex;
        public nint DataPointer;
        public uint DataSize;
        public uint DataRowPitch;
    }

    public struct UCCopyBuffer : IModificationCommand
    {
        public bool SourceIsExternal;
        public nint SourceBuffer;
        public uint SourceOffset;
        public bool DestinationIsExternal;
        public nint DestinationBuffer;
        public uint DestinationOffset;
        public uint NumBytes;
    }

    public struct UCCopyTexture : IModificationCommand
    {
        public CopySource Source;
        public FGBox? SourceBox;
        public CopySource Destination;
        public uint DstX;
        public uint DstY;
        public uint DstZ;

        [StructLayout(LayoutKind.Explicit)]
        public struct CopySource
        {
            [FieldOffset(0)]
            public bool IsExternal;
            [FieldOffset(1)]
            public FGResourceId ResourceType;
            [FieldOffset(2)]
            public nint Resource;

            [FieldOffset(10)]
            public CopySourceType Type;

            [FieldOffset(11)]
            public uint SubresourceIndex;

            [FieldOffset(15)]
            public Footprint Footprint;
        }

        public struct Footprint
        {
            public uint Offset;
            public RHIFormat Format;
            public uint Width;
            public uint Height;
            public uint Depth;
            public uint RowPitch;
        }

        public enum CopySourceType : byte
        {
            SubresourceIndex = 0,
            Footprint
        }
    }
}
