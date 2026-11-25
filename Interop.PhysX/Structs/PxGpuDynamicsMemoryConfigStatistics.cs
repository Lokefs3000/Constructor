using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxGpuDynamicsMemoryConfigStatistics
    {
        public ulong tempBufferCapacity;
        public uint rigidContactCount;
        public uint rigidPatchCount;
        public uint foundLostPairs;
        public uint foundLostAggregatePairs;
        public uint totalAggregatePairs;
        public uint deformableSurfaceContacts;
        public uint deformableVolumeContacts;
        public uint softbodyContacts;
        public uint particleContacts;
        public uint collisionStackSize;
    }
}
