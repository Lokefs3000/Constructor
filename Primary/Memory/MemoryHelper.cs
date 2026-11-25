using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Memory
{
    public static class MemoryHelper
    {
        public static int AlignUp(int value, int alignment)
        {
            return value + (-value & (alignment - 1));
        }
    }
}
