using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Common
{
    public static class SimdUtility
    {

        public static byte CreateShuffleMask(byte p3, byte p2, byte p1, byte p0)
        {
            return (byte)((p3 << 6) | (p2 << 4) | (p1 << 2) | p0);
        }
    }
}
