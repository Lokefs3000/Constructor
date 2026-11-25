using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace xxHash.XXH3
{
    [StructLayout(LayoutKind.Sequential, Pack = 64)]
    public unsafe struct XXH3State
    {
        public fixed ulong Acc[8];
        public fixed byte CustomSecret[XXH3.SecretDefaultSize];
        public fixed byte Buffer[XXH3.InternalBufferSize];
        public uint BufferedSize;
        public uint UseSeed;
        public ulong NbStripesSoFar;
        public ulong TotalLen;
        public ulong NbStripesPerBlock;
        public ulong SecretLimit;
        public ulong Seed;
        public ulong Reserved64;
        public byte* ExtSecret;
    }
}
