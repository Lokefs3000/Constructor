using Primary.Common;
using Primary.Rendering2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
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
        public bool IsExternal;
        public nint Texture;
        public FGBox? DestinationBox;
        public int SubresourceIndex;
        public nint DataPointer;
        public uint DataSize;
    }
}
