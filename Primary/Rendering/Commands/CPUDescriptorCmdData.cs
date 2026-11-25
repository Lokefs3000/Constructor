using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Recording;

namespace Primary.Rendering.Commands
{
    internal readonly record struct CPUDescriptorCmdData(int IndexIntoDescriptorArray)
    {
        public bool IsNull => IndexIntoDescriptorArray == -1;

        public static readonly CPUDescriptorCmdData Null = new CPUDescriptorCmdData(-1);

        public static implicit operator CPUDescriptorCmdData(DescriptorData data) => new CPUDescriptorCmdData(data.CPUDescriptorIndex);
    }
}
