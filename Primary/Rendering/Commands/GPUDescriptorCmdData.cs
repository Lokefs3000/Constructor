using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Recording;

namespace Primary.Rendering.Commands
{
    internal readonly record struct GPUDescriptorCmdData(int IndexIntoDescriptorArray)
    {
        public bool IsNull => IndexIntoDescriptorArray == -1;

        public static readonly GPUDescriptorCmdData Null = new GPUDescriptorCmdData(-1);

        public static implicit operator GPUDescriptorCmdData(DescriptorData data) => new GPUDescriptorCmdData(data.GPUDescriptorIndex);
    }
}
