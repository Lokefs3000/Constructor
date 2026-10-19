using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Commands
{
    internal readonly record struct ResourceCmdData(int Index)
    {
        public static implicit operator ResourceCmdData(ResourceHandle handle) => new ResourceCmdData(handle.Index);
        public static implicit operator ResourceCmdData(BufferHandle handle) => new ResourceCmdData(handle.Index);
        public static implicit operator ResourceCmdData(TextureHandle handle) => new ResourceCmdData(handle.Index);
    }
}
