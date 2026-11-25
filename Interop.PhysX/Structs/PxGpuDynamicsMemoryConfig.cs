using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxGpuDynamicsMemoryConfig
    {
        public ulong tempBufferCapacity;
        public uint maxRigidContactCount;
        public uint maxRigidPatchCount;
        public uint heapCapacity;
        public uint foundLostPairsCapacity;
        public uint foundLostAggregatePairsCapacity;
        public uint totalAggregatePairsCapacity;
        public uint maxDeformableSurfaceContacts;
        public uint maxDeformableVolumeContacts;
        public uint maxParticleContacts;
        public uint collisionStackSize;
    }
}
