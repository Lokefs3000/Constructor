using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxSceneLimits
    {
        public uint maxNbActors;
        public uint maxNbBodies;
        public uint maxNbStaticShapes;
        public uint maxNbDynamicShapes;
        public uint maxNbAggregates;
        public uint maxNbConstraints;
        public uint maxNbRegions;
        public uint maxNbBroadPhaseOverlaps;
    }
}
