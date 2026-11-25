using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxSceneDesc
    {
        public PxVec3 gravity;
        public PxSimulationEventCallback* simulationEventCallback;
        public PxContactModifyCallback* contactModifyCallback;
        public PxCCDContactModifyCallback* ccdContactModifyCallback;
        public PxPostSolveCallback* deformableSurfacePostSolveCallback;
        public PxPostSolveCallback* deformableVolumePostSolveCallback;
        public void* filterShaderData;
        public uint filterShaderDataSize;
        public PxSimulationFilterShader* filterShader;
        public PxSimulationFilterCallback* filterCallback;
        public PxPairFilteringMode kineKineFilteringMode;
        public PxPairFilteringMode staticKineFilteringMode;
        public PxBroadPhaseType broadPhaseType;
        public PxBroadPhaseCallback* broadPhaseCallback;
        public PxGpuBroadPhaseDesc* gpuBroadPhaseDesc;
        public PxSceneLimits limits;
        public PxSolverType solverType;
        public float bounceThresholdVelocity;
        public float frictionOffsetThreshold;
        public float frictionCorrelationDistance;
        public PxSceneFlag flags;
        public PxCpuDispatcher* cpuDispatcher;
        public PxCudaContextManager* cudaContextManager;
        public void* userData;
        public uint solverBatchSize;
        public uint solverArticulationBatchSize;
        public uint nbContactDataBlocks;
        public uint maxNbContactDataBlocks;
        public float maxBiasCoefficient;
        public uint contactReportStreamBufferSize;
        public uint ccdMaxPasses;
        public float ccdThreshold;
        public float ccdMaxSeparation;
        public float wakeCounterResetValue;
        public PxBounds3 sanityBounds;
        public PxGpuDynamicsMemoryConfig gpuDynamicsConfig;
        public uint gpuMaxNumPartitions;
        public uint gpuMaxNumStaticPartitions;
        public uint contactPairSlabSize;
        public PxSceneQuerySystem* sceneQuerySystem;
        public PxPruningStructureType staticStructure;
        public PxPruningStructureType dynamicStructure;
        public uint dynamicTreeRebuildRateHint;
        public PxDynamicTreeSecondaryPruner dynamicTreeSecondaryPruner;
        public PxBVHBuildStrategy staticBVHBuildStrategy;
        public PxBVHBuildStrategy dynamicBVHBuildStrategy;
        public uint staticNbObjectsPerNode;
        public uint dynamicNbObjectsPerNode;
        public PxSceneQueryUpdateMode sceneQueryUpdateMode;
    }
}
