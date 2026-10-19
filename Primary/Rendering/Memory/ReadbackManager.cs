using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Memory
{
    internal sealed class ReadbackManager
    {
        private readonly VirtualAllocator _allocator;



        internal ReadbackManager()
        {
            _allocator = new VirtualAllocator();
        }


    }


}
