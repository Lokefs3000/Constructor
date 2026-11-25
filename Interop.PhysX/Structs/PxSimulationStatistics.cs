using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxSimulationStatistics
    {
        public uint nbActiveConstraints;
        public uint nbActiveDynamicBodies;
        public uint nbActiveKinematicBodies;
        public uint nbStaticBodies;
        public uint nbDynamicBodies;
        public uint nbKinemaicBodies;
        public InlineArray11<uint> nbShapes;
        public uint nbAggregates;
        public uint nbArticulations;
        public uint nbAxisSolverConstraints;
        public uint compressedContactSize;
        public uint requiredContactConstraintMemory;
        public uint peakConstraintMemory;
        public uint nbDiscreteContactPairsTotal;
        public uint nbDiscreteContactPairsWithCacheHits;
        public uint nbDiscreteContactPairsWithContacts;
        public uint nbNewPairs;
        public uint nbLostPairs;
        public uint nbNewTouches;
        public uint nbLostTouches;
        public uint nbPartitions;
        public ulong gpuMemParticles;
        public ulong gpuMemDeformableSurfaces;
        public ulong gpuMemDeformableVolumes;
        public ulong gpuMemHeap;
        public ulong gpuMemHeapBroadPhase;
        public ulong gpuMemHeapNarrowPhase;
        public ulong gpuMemHeapSolver;
        public ulong gpuMemHeapArticulation;
        public ulong gpuMemHeapSimulation;
        public ulong gpuMemHeapSimulationArticulation;
        public ulong gpuMemHeapSimulationParticles;
        public ulong gpuMemHeapSimulationDeformableSurface;
        public ulong gpuMemHeapSimulationDeformableVolume;
        public ulong gpuMemHeapParticles;
        public ulong gpuMemHeapDeformableSurfaces;
        public ulong gpuMemHeapDeformableVolumes;
        public ulong gpuMemHeapOther;
        public PxGpuDynamicsMemoryConfigStatistics gpuDynamicsConfigStatistics;
        public uint nbBroadPhaseAdds;
        public uint nbBroadPhaseRemoves;
        public InlineArray11<InlineArray11<uint>> nbDiscreteContactPairs;
        public InlineArray11<InlineArray11<uint>> nbCCDPairs;
        public InlineArray11<InlineArray11<uint>> nbModifiedContactPairs;
        public InlineArray11<InlineArray11<uint>> nbTriggerPairs;
    }
}
