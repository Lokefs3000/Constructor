using Primary.Common;
using Primary.Rendering2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    internal interface IUnmanagedCommand
    {

    }

    internal struct UnmanagedCommandMeta
    {
        public RecCommandType Type;
    }

    internal struct UCSetRenderTarget : IUnmanagedCommand
    {
        public byte Slot;
        public int RenderTarget;
    }

    internal struct UCSetDepthStencil : IUnmanagedCommand
    {
        public int DepthStencil;
    }

    internal struct UCSetViewports : IUnmanagedCommand
    {
        public int Slot;
        public FGViewport? Viewport;
    }

    internal struct UCSetScissor : IUnmanagedCommand
    {
        public int Slot;
        public FGRect? Scissor;
    }

    internal struct UCSetStencilRef : IUnmanagedCommand
    {
        public uint StencilRef;
    }

    internal struct UCSetBuffer : IUnmanagedCommand
    {
        public int Buffer;
        public FGSetBufferLocation Location;
        public int Stride;
    }
}
