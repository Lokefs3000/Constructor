using Primary.Common;
using Primary.Rendering2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    internal interface IStateChangeCommand
    {

    }

    internal struct UnmanagedCommandMeta
    {
        public RecCommandType Type;
    }

    internal struct UCSetRenderTarget : IStateChangeCommand
    {
        public byte Slot;
        public int RenderTarget;
    }

    internal struct UCSetDepthStencil : IStateChangeCommand
    {
        public int DepthStencil;
    }

    internal struct UCSetViewports : IStateChangeCommand
    {
        public int Slot;
        public FGViewport? Viewport;
    }

    internal struct UCSetScissor : IStateChangeCommand
    {
        public int Slot;
        public FGRect? Scissor;
    }

    internal struct UCSetStencilRef : IStateChangeCommand
    {
        public uint StencilRef;
    }

    internal struct UCSetBuffer : IStateChangeCommand
    {
        public int Buffer;
        public FGSetBufferLocation Location;
        public int Stride;
    }

    internal struct UCSetProperties : IStateChangeCommand
    {
        public int ResourceCount;
        public int DataBlockSize;
    }
}
