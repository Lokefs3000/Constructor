using System;
using System.Collections.Generic;
using System.Text;

namespace xxHash.Int64
{
    public unsafe struct XXH64State
    {
        public ulong TotalLen;
        public fixed uint Acc[4];
        public fixed byte Buffer[32];
        public uint BufferedSize;
        public uint Reserved32;
        public ulong Reserved64;
    }
}
