using Primary.Common;
using Primary.Rendering2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    internal interface IModificationCommand
    {

    }

    internal struct ModificationCommandMeta
    {
        public RecCommandType Type;
    }

    internal struct UCClearRenderTarget : IModificationCommand
    {
        public int RenderTarget;
        public FGRect? Rect;
    }

    internal struct UCClearDepthStencil : IModificationCommand
    {
        public int DepthStencil;
        public FGClearFlags ClearFlags;
        public FGRect? Rect;
    }

    internal struct UCClearRenderTargetCustom : IModificationCommand
    {
        public int RenderTarget;
        public FGRect? Rect;
        public Color Color;
    }

    internal struct UCClearDepthStencilCustom : IModificationCommand
    {
        public int DepthStencil;
        public FGClearFlags ClearFlags;
        public FGRect? Rect;
        public float Depth;
        public byte Stencil;
    }

    internal struct UCUploadBuffer : IModificationCommand
    {
        public int Buffer;
        public uint Offset;
        public nint DataPointer;
        public uint DataSize;
    }

    internal struct UCUploadTexture : IModificationCommand
    {
        public int Texture;
        public FGBox? DestinationBox;
        public int SubresourceIndex;
        public nint DataPointer;
        public uint DataSize;
    }
}
