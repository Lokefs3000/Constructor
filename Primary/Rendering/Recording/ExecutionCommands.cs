namespace Primary.Rendering.Recording
{
    public interface IExecutionCommand
    {

    }

    public struct ExecutionCommandMeta
    {
        public RecCommandType Type;
        public RecCommandEffectFlags Effect;
    }

    public struct UCDummy : IExecutionCommand
    {

    }

    public struct UCDrawIndexedInstanced : IExecutionCommand
    {
        public uint IndexCount;
        public uint InstanceCount;
        public uint StartIndex;
        public int BaseVertex;
        public uint StartInstance;
    }

    public struct UCDrawInstanced : IExecutionCommand
    {
        public uint VertexCount;
        public uint InstanceCount;
        public uint StartVertex;
        public uint StartInstance;
    }

    public struct UCDispatch : IExecutionCommand
    {
        public uint ThreadGroupCountX;
        public uint ThreadGroupCountY;
        public uint ThreadGroupCountZ;
    }

    public struct UCPresentOnWindow : IExecutionCommand
    {
        public bool IsExternal;
        public nint Texture;
        public uint WindowId;
    }
}
