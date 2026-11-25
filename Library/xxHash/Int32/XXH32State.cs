using System;
using System.Collections.Generic;
using System.Text;

namespace xxHash.Int32
{
    public unsafe struct XXH32State
    {
        public uint TotalLen32;
        public uint LargeLen;
        public fixed uint Acc[4];
        public fixed byte Buffer[BufferSize];
        public uint BufferedSize;
        public uint Reserved;

        public const int BufferSize = 16;
    }
}
