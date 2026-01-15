using Primary.Rendering.Structures;

namespace Primary.Rendering.Recording
{
    public interface IStateChangeCommand
    {

    }

    public struct UnmanagedCommandMeta
    {
        public RecCommandType Type;
    }

    public struct UCSetRenderTarget : IStateChangeCommand
    {
        public bool IsExternal;
        public nint Texture;
        public byte Slot;
    }

    public struct UCSetDepthStencil : IStateChangeCommand
    {
        public int DepthStencil;
    }

    public struct UCSetViewport : IStateChangeCommand
    {
        public int Slot;
        public FGViewport? Viewport;
    }

    public struct UCSetScissor : IStateChangeCommand
    {
        public int Slot;
        public FGRect? Scissor;
    }

    public struct UCSetStencilRef : IStateChangeCommand
    {
        public uint StencilRef;
    }

    public struct UCSetBuffer : IStateChangeCommand
    {
        public bool IsExternal;
        public nint Buffer;
        public FGSetBufferLocation Location;
        public int Stride;
    }

    public struct UCSetProperties : IStateChangeCommand
    {
        public int ResourceCount;
        public int DataBlockSize;

        public bool UseBufferForHeader;
    }

    public struct UCSetPipeline : IStateChangeCommand
    {
        public nint Pipeline;
        public bool IsCompute;
    }

    public struct UCSetConstants : IStateChangeCommand
    {
        public int ConstantsDataSize;
        public nint DataPointer;
    }
}
