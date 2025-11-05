using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    internal interface IExecutionCommand
    {

    }

    internal struct ExecutionCommandMeta
    {
        public RecCommandType Type;
        //public RecCommandEffectFlags Effect;
    }

    internal struct UCDrawIndexedInstanced : IExecutionCommand
    {
        public uint IndexCount;
        public uint InstanceCount;
        public uint StartIndex;
        public int BaseVertex;
        public uint StartInstance;
    }

    internal struct UCDrawInstanced : IExecutionCommand
    {
        public uint InstanceCount;
        public uint StartVertex;
        public uint StartInstance;
    }
}
