using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxContactPairHeader
    {
        public PxActor* actors0;
        public PxActor* actors1;
        public sbyte* extraDataStream;
        public ushort extraDataStreamSize;
        public PxContactPairHeaderFlag flags;
        public PxContactPair* pairs;
        public uint nbPairs;
    }
}
