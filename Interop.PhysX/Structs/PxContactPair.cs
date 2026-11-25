using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxContactPair
    {
        public PxShape* shapes0;
        public PxShape* shapes1;
        public sbyte* contactPatches;
        public sbyte* contactPoints;
        public float* contactImpulses;
        public sbyte* frictionPatches;
        public uint requiredBufferSize;
        public sbyte contactCount;
        public sbyte patchCount;
        public sbyte contactStreamSize;
        public PxContactPairFlag flags;
        public PxPairFlag events;
        public uint internalData0;
        public uint internalData1;
    }
}
