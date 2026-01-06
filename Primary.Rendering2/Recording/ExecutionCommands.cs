using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
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

    public struct UCPresentOnWindow : IExecutionCommand
    {
        public bool IsExternal;
        public nint Texture;
        public uint WindowId;
    }
}
