#pragma once

#include <cstdint>

#define OPAQUE_CLASS(name, inherit) typedef void C##name
#define ALIASED_TYPE(value, name) typedef value C##name

OPAQUE_CLASS(PxFoundation);
OPAQUE_CLASS(PxPhysics);
OPAQUE_CLASS(PxPvd);
OPAQUE_CLASS(PxAggregate);
OPAQUE_CLASS(PxTriangleMesh);
OPAQUE_CLASS(PxInputStream);
OPAQUE_CLASS(PxTetrahedronMesh);
OPAQUE_CLASS(PxHeightField);
OPAQUE_CLASS(PxConvexMesh);
OPAQUE_CLASS(PxBVH);
OPAQUE_CLASS(PxScene);
OPAQUE_CLASS(PxSimulationEventCallback);
OPAQUE_CLASS(PxContactModifyCallback);
OPAQUE_CLASS(PxCCDContactModifyCallback);
OPAQUE_CLASS(PxPostSolveCallback);
OPAQUE_CLASS(PxSimulationFilterShader);
OPAQUE_CLASS(PxSimulationFilterCallback);
OPAQUE_CLASS(PxBroadPhaseCallback);
OPAQUE_CLASS(PxGpuBroadPhaseDesc);
OPAQUE_CLASS(PxCpuDispatcher);
OPAQUE_CLASS(PxCudaContextManager);
OPAQUE_CLASS(PxSceneQuerySystemBase);
OPAQUE_CLASS(PxSceneQuerySystem, PxSceneQuerySystemBase);
OPAQUE_CLASS(PxBase);
OPAQUE_CLASS(PxRefCounted, PxBase);
OPAQUE_CLASS(PxActor, PxBase);
OPAQUE_CLASS(PxRigidActor, PxActor);
OPAQUE_CLASS(PxRigidStatic, PxRigidActor);
OPAQUE_CLASS(PxRigidDynamic, PxRigidActor);
OPAQUE_CLASS(PxPruningStructure);
OPAQUE_CLASS(PxBaseMaterial, PxRefCounted);
OPAQUE_CLASS(PxMaterial, PxBaseMaterial);
OPAQUE_CLASS(PxShape, PxRefCounted);
OPAQUE_CLASS(PxDeformableVolumeMaterial, PxBaseMaterial);
OPAQUE_CLASS(PxDeformableSurfaceMaterial, PxBaseMaterial);
OPAQUE_CLASS(PxConstraint);
OPAQUE_CLASS(PxConstraintConnector);
OPAQUE_CLASS(PxConstraintSolverPrep);
OPAQUE_CLASS(PxConstraintVisualize);
OPAQUE_CLASS(PxDeformableAttachment);
OPAQUE_CLASS(PxDeformableElementFilter);
OPAQUE_CLASS(PxDeformableSurface);
OPAQUE_CLASS(PxDeformableVolume);
OPAQUE_CLASS(PxPBDParticleSystem);
OPAQUE_CLASS(PxParticleBuffer);
OPAQUE_CLASS(PxParticleAndDiffuseBuffer);
OPAQUE_CLASS(PxPBDMaterial, PxBaseMaterial);
OPAQUE_CLASS(PxDeletionListener);
OPAQUE_CLASS(PxTaskManager);
OPAQUE_CLASS(PxPvdSceneClient);
OPAQUE_CLASS(PxDirectGPUAPI);
OPAQUE_CLASS(PxDeformableVolumeMesh);
OPAQUE_CLASS(PxArticulationReducedCoordinate);
OPAQUE_CLASS(PxCollection);
OPAQUE_CLASS(PxBaseTask);
OPAQUE_CLASS(PxRenderBuffer);
OPAQUE_CLASS(PxErrorCallback);
OPAQUE_CLASS(PxAllocatorCallback);
OPAQUE_CLASS(PxAllocationListener);
OPAQUE_CLASS(PxProfilerCallback);
OPAQUE_CLASS(PxOverlapCallback);
OPAQUE_CLASS(PxRaycastCallback);
OPAQUE_CLASS(PxSweepCallback);
OPAQUE_CLASS(PxQueryFilterCallback);

typedef int8_t CPxI8;
typedef uint8_t CPxU8;
typedef int16_t CPxI16;
typedef uint16_t CPxU16;
typedef int32_t CPxI32;
typedef uint32_t CPxU32;
typedef int64_t CPxI64;
typedef uint64_t CPxU64;
typedef float CPxReal;

ALIASED_TYPE(CPxU32, PxAggregateFilterHint);
ALIASED_TYPE(CPxU8, PxDominanceGroup);
ALIASED_TYPE(CPxU8, PxClientID);
ALIASED_TYPE(CPxU32, PxShapeGPUIndex);
ALIASED_TYPE(CPxU16, PxType);
ALIASED_TYPE(CPxU32, PxRigidDynamicGPUIndex);

enum class CPxPairFilteringMode
{
	eKEEP = 0,
	eSUPPRESS = 1,
	eKILL = 2,
	eDEFAULT = 1
};

enum class CPxBroadPhaseType
{
	eSAP,
	eMBP,
	eABP,
	ePABP,
	eGPU,
	eLAST
};

enum class CPxSolverType
{
	ePGS,
	eTGS
};

enum class CPxSceneFlag
{
	eENABLE_ACTIVE_ACTORS = 1 << 0,
	eENABLE_CCD = 1 << 1,
	eDISABLE_CCD_RESWEEP = 1 << 2,
	eENABLE_PCM = 1 << 6,
	eDISABLE_CONTACT_REPORT_BUFFER_RESIZE = 1 << 7,
	eDISABLE_CONTACT_CACHE = 1 << 8,
	eREQUIRE_RW_LOCK = 1 << 9,
	eENABLE_STABILIZATION = 1 << 10,
	eENABLE_AVERAGE_POINT = 1 << 11,
	eEXCLUDE_KINEMATICS_FROM_ACTIVE_ACTORS = 1 << 12,
	eENABLE_GPU_DYNAMICS = 1 << 13,
	eENABLE_ENHANCED_DETERMINISM = 1 << 14,
	eENABLE_FRICTION_EVERY_ITERATION = 1 << 15,
	eENABLE_EXTERNAL_FORCES_EVERY_ITERATION_TGS = 1 << 16,
	eENABLE_DIRECT_GPU_API = 1 << 17,
	eENABLE_BODY_ACCELERATIONS = 1 << 18,
	eSOLVE_ARTICULATION_CONTACT_LAST = 1 << 19,
	eDISABLE_SLEEPING = 1 << 20,
	eMUTABLE_FLAGS = eENABLE_ACTIVE_ACTORS | eEXCLUDE_KINEMATICS_FROM_ACTIVE_ACTORS
};

enum class CPxPruningStructureType
{
	eNONE,
	eDYNAMIC_AABB_TREE,
	eSTATIC_AABB_TREE,
	eLAST
};

enum class CPxBVHBuildStrategy
{
	eFAST,
	eDEFAULT,
	eSAH,
	eLAST
};

enum class CPxSceneQueryUpdateMode
{
	eBUILD_ENABLED_COMMIT_ENABLED,
	eBUILD_ENABLED_COMMIT_DISABLED,
	eBUILD_DISABLED_COMMIT_DISABLED
};

enum class CPxDynamicTreeSecondaryPruner
{
	eNONE,
	eBUCKET,
	eINCREMENTAL,
	eBVH,
	eLAST
};

enum class CPxShapeFlag
{
	eSIMULATION_SHAPE = 1 << 0,
	eSCENE_QUERY_SHAPE = 1 << 1,
	eTRIGGER_SHAPE = 1 << 2,
	eVISUALIZATION = 1 << 3
};

enum class CPxGeometryType
{
	eSPHERE,
	ePLANE,
	eCAPSULE,
	eBOX,
	eCONVEXCORE,
	eCONVEXMESH,
	ePARTICLESYSTEM,
	eTETRAHEDRONMESH,
	eTRIANGLEMESH,
	eHEIGHTFIELD,
	eCUSTOM,

	eGEOMETRY_COUNT
};

enum class CPxConvexMeshGeometryFlag : uint8_t
{
	eTIGHT_BOUNDS = 1 << 0
};

enum class CPxMeshGeometryFlag : uint8_t
{
	eTIGHT_BOUNDS = 1 << 0,
	eDOUBLE_SIDED = 1 << 1
};

enum class CPxConstraintFlag : uint16_t
{
	eBROKEN = 1 << 0,
	eCOLLISION_ENABLED = 1 << 3,
	eVISUALIZATION = 1 << 4,
	eDRIVE_LIMITS_ARE_FORCES = 1 << 5,
	eIMPROVED_SLERP = 1 << 7,
	eDISABLE_PREPROCESSING = 1 << 8,
	eENABLE_EXTENDED_LIMITS = 1 << 9,
	eGPU_COMPATIBLE = 1 << 10,
	eALWAYS_UPDATE = 1 << 11,
	eDISABLE_CONSTRAINT = 1 << 12
};

enum class CPxDeformableAttachmentTargetType
{
	eVERTEX,
	eTRIANGLE,
	eTETRAHEDRON,
	eRIGID,
	eWORLD,
	eUNDEFINED
};

enum class CPxDeletionEventFlag
{
	eUSER_RELEASE = 1 << 0,
	eMEMORY_RELEASE = 1 << 1
};

enum class CPxActorTypeFlag
{
	eRIGID_STATIC = 1 << 0,
	eRIGID_DYNAMIC = 1 << 1
};

enum class CPxContactPairHeaderFlag : CPxU16
{
	eREMOVED_ACTOR_0 = 1 << 0,
	eREMOVED_ACTOR_1 = 1 << 1
};

enum class CPxContactPairFlag : CPxU16
{
	eREMOVED_SHAPE_0 = 1 << 0,
	eREMOVED_SHAPE_1 = 1 << 1,
	eACTOR_PAIR_HAS_FIRST_TOUCH = 1 << 2,
	eACTOR_PAIR_LOST_TOUCH = 1 << 3,
	eINTERNAL_HAS_IMPULSES = 1 << 4,
	eINTERNAL_CONTACTS_ARE_FLIPPED = 1 << 5
};

enum class CPxPairFlag : CPxU16
{
	eSOLVE_CONTACT = 1 << 0,
	eMODIFY_CONTACTS = 1 << 1,
	eNOTIFY_TOUCH_FOUND = 1 << 2,
	eNOTIFY_TOUCH_PERSISTS = 1 << 3,
	eNOTIFY_TOUCH_LOST = 1 << 4,
	eNOTIFY_TOUCH_CCD = 1 << 5,
	eNOTIFY_THRESHOLD_FORCE_FOUND = 1 << 6,
	eNOTIFY_THRESHOLD_FORCE_PERSISTS = 1 << 7,
	eNOTIFY_THRESHOLD_FORCE_LOST = 1 << 8,
	eNOTIFY_CONTACT_POINTS = 1 << 9,
	eDETECT_DISCRETE_CONTACT = 1 << 10,
	eDETECT_CCD_CONTACT = 1 << 11,
	ePRE_SOLVER_VELOCITY = 1 << 12,
	ePOST_SOLVER_VELOCITY = 1 << 13,
	eCONTACT_EVENT_POSE = 1 << 14,
	eNEXT_FREE = 1 << 15,

	eCONTACT_DEFAULT = eSOLVE_CONTACT | eDETECT_DISCRETE_CONTACT,
	eTRIGGER_DEFAULT = eNOTIFY_TOUCH_FOUND | eNOTIFY_TOUCH_LOST | eDETECT_DISCRETE_CONTACT
};

enum class CPxVisualizationParameter
{
	eSCALE,
	eWORLD_AXES,
	eBODY_AXES,
	eBODY_MASS_AXES,
	eBODY_LIN_VELOCITY,
	eBODY_ANG_VELOCITY,
	eCONTACT_POINT,
	eCONTACT_NORMAL,
	eCONTACT_ERROR,
	eCONTACT_IMPULSE,
	eFRICTION_POINT,
	eFRICTION_NORMAL,
	eFRICTION_IMPULSE,
	eACTOR_AXES,
	eCOLLISION_AABBS,
	eCOLLISION_SHAPES,
	eCOLLISION_AXES,
	eCOLLISION_COMPOUNDS,
	eCOLLISION_FNORMALS,
	eCOLLISION_EDGES,
	eCOLLISION_STATIC,
	eCOLLISION_DYNAMIC,
	eJOINT_LOCAL_FRAMES,
	eJOINT_LIMITS,
	eCULL_BOX,
	eMBP_REGIONS,
	eSIMULATION_MESH,
	eSDF,

	eNUM_VALUES,
	eFORCE_DWORD = 0x7fffffff
};

enum class CRbPairStatsType
{
	eDISCRETE_CONTACT_PAIRS,
	eCCD_PAIRS,
	eMODIFIED_CONTACT_PAIRS,
	eTRIGGER_PAIRS
};

enum class CPxErrorCode
{
	eNO_ERROR = 0,
	eDEBUG_INFO = 1,
	eDEBUG_WARNING = 2,
	eINVALID_PARAMETER = 4,
	eINVALID_OPERATION = 8,
	eOUT_OF_MEMORY = 16,
	eINTERNAL_ERROR = 32,
	eABORT = 64,
	ePERF_WARNING = 128,
	eMASK_ALL = -1
};

enum class CPxBaseFlag : CPxU16
{
	eOWNS_MEMORY = 1 << 0,
	eIS_RELEASABLE = 1 << 1
};

enum class CPxActorFlag : CPxU8
{
	eVISUALIZATION = 1 << 0,
	eDISABLE_GRAVITY = 1 << 1,
	eSEND_SLEEP_NOTIFIES = 1 << 2,
	eDISABLE_SIMULATION = 1 << 3
};

enum class CPxActorType
{
	eRIGID_STATIC,
	eRIGID_DYNAMIC,
	eARTICULATION_LINK,
	eDEFORMABLE_SURFACE,
	eDEFORMABLE_VOLUME,
	ePBD_PARTICLESYSTEM,
	eACTOR_COUNT,
	eACTOR_FORCE_DWORD = 0x7fffffff
};

enum class CPxRigidDynamicLockFlag : CPxU8
{
	eLOCK_LINEAR_X = 1 << 0,
	eLOCK_LINEAR_Y = 1 << 1,
	eLOCK_LINEAR_Z = 1 << 2,
	eLOCK_ANGULAR_X = 1 << 3,
	eLOCK_ANGULAR_Y = 1 << 4,
	eLOCK_ANGULAR_Z = 1 << 5
};

enum class CPxMaterialFlag : CPxU16
{
	eDISABLE_FRICTION = 1 << 0,
	eDISABLE_STRONG_FRICTION = 1 << 1,
	eCOMPLIANT_ACCELERATION_SPRING = 1 << 4
};

enum class CPxCombineMode : CPxU32
{
	eAVERAGE = 0,
	eMIN = 1,
	eMULTIPLY = 2,
	eMAX = 3,
	eN_VALUES = 4,
	ePAD_32 = 0x7fffffff
};

enum class CPxHitFlag : CPxU16
{
	ePOSITION = 1 << 0,
	eNORMAL = 1 << 1,
	eUV = 1 << 3,
	eASSUME_NO_INITIAL_OVERLAP = 1 << 4,
	eANY_HIT = 1 << 5,
	eMESH_MULTIPLE = 1 << 6,
	eMESH_BOTH_SIDES = 1 << 7,
	ePRECISE_SWEEP = 1 << 8,
	eMTD = 1 << 9,
	eFACE_INDEX = 1 << 10,
	eDEFAULT = ePOSITION | eNORMAL | eFACE_INDEX,
	eMODIFIABLE_FLAGS = eMESH_MULTIPLE | eMESH_BOTH_SIDES | eASSUME_NO_INITIAL_OVERLAP | ePRECISE_SWEEP
};

enum class CPxGeometryQueryFlag
{
	eSIMD_GUARD = 1 << 0,
	eDEFAULT = eSIMD_GUARD
};

enum class CPxQueryFlag : CPxU16
{
	eSTATIC = 1 << 0,
	eDYNAMIC = 1 << 1,
	ePREFILTER = 1 << 2,
	ePOSTFILTER = 1 << 3,
	eANY_HIT = 1 << 4,
	eNO_BLOCK = 1 << 5,
	eBATCH_QUERY_LEGACY_BEHAVIOUR = 1 << 6,
	eDISABLE_HARDCODED_FILTER = 1 << 6,
	eRESERVED = 1 << 15
};

struct CPxVec3
{
	CPxReal x;
	CPxReal y;
	CPxReal z;
};

struct CPxVec4
{
	CPxReal x;
	CPxReal y;
	CPxReal z;
};

struct CPxQuat
{
	CPxReal x;
	CPxReal y;
	CPxReal z;
	CPxReal w;
};

struct CPxTransform
{
	CPxQuat q;
	CPxVec3 p;
};

struct CPxBounds3
{
	CPxVec3 minimum;
	CPxVec3 maximum;
};

struct CPxMeshScale
{
	CPxVec3 scale;
	CPxQuat rotation;
};

struct CPxTypedBoundedDataU32
{
	CPxU32* data;
	CPxU32 stride;
	CPxU32 count;
};

struct CPxTypedBoundedDataVec4
{
	CPxVec4* data;
	CPxU32 stride;
	CPxU32 count;
};

struct CPxTolerancesScale
{
	CPxReal length;
	CPxReal speed;
};

struct CPxSceneLimits
{
	CPxU32 maxNbActors;
	CPxU32 maxNbBodies;
	CPxU32 maxNbStaticShapes;
	CPxU32 maxNbDynamicShapes;
	CPxU32 maxNbAggregates;
	CPxU32 maxNbConstraints;
	CPxU32 maxNbRegions;
	CPxU32 maxNbBroadPhaseOverlaps;
};

struct CPxGpuDynamicsMemoryConfig
{
	CPxU64 tempBufferCapacity;
	CPxU32 maxRigidContactCount;
	CPxU32 maxRigidPatchCount;
	CPxU32 heapCapacity;
	CPxU32 foundLostPairsCapacity;
	CPxU32 foundLostAggregatePairsCapacity;
	CPxU32 totalAggregatePairsCapacity;
	CPxU32 maxDeformableSurfaceContacts;
	CPxU32 maxDeformableVolumeContacts;
	CPxU32 maxParticleContacts;
	CPxU32 collisionStackSize;
};

struct CPxSceneDesc
{
	CPxVec3 gravity;
	CPxSimulationEventCallback* simulationEventCallback;
	CPxContactModifyCallback* contactModifyCallback;
	CPxCCDContactModifyCallback* ccdContactModifyCallback;
	CPxPostSolveCallback* deformableSurfacePostSolveCallback;
	CPxPostSolveCallback* deformableVolumePostSolveCallback;
	void* filterShaderData;
	CPxU32 filterShaderDataSize;
	CPxSimulationFilterShader* filterShader;
	CPxSimulationFilterCallback* filterCallback;
	CPxPairFilteringMode kineKineFilteringMode;
	CPxPairFilteringMode staticKineFilteringMode;
	CPxBroadPhaseType broadPhaseType;
	CPxBroadPhaseCallback* broadPhaseCallback;
	CPxGpuBroadPhaseDesc* gpuBroadPhaseDesc;
	CPxSceneLimits limits;
	CPxSolverType solverType;
	CPxReal bounceThresholdVelocity;
	CPxReal frictionOffsetThreshold;
	CPxReal frictionCorrelationDistance;
	CPxSceneFlag flags;
	CPxCpuDispatcher* cpuDispatcher;
	CPxCudaContextManager* cudaContextManager;
	void* userData;
	CPxU32 solverBatchSize;
	CPxU32 solverArticulationBatchSize;
	CPxU32 nbContactDataBlocks;
	CPxU32 maxNbContactDataBlocks;
	CPxReal maxBiasCoefficient;
	CPxU32 contactReportStreamBufferSize;
	CPxU32 ccdMaxPasses;
	CPxReal ccdThreshold;
	CPxReal ccdMaxSeparation;
	CPxReal wakeCounterResetValue;
	CPxBounds3 sanityBounds;
	CPxGpuDynamicsMemoryConfig gpuDynamicsConfig;
	CPxU32 gpuMaxNumPartitions;
	CPxU32 gpuMaxNumStaticPartitions;
	CPxU32 contactPairSlabSize;
	CPxSceneQuerySystem* sceneQuerySystem;
	CPxPruningStructureType staticStructure;
	CPxPruningStructureType dynamicStructure;
	CPxU32 dynamicTreeRebuildRateHint;
	CPxDynamicTreeSecondaryPruner dynamicTreeSecondaryPruner;
	CPxBVHBuildStrategy staticBVHBuildStrategy;
	CPxBVHBuildStrategy dynamicBVHBuildStrategy;
	CPxU32 staticNbObjectsPerNode;
	CPxU32 dynamicNbObjectsPerNode;
	CPxSceneQueryUpdateMode sceneQueryUpdateMode;
};

struct CPxGeometry
{
	CPxGeometryType type;
};

struct CPxBoxGeometry
{
	CPxGeometry base;
	CPxVec3 halfExtents;
};

struct CPxCapsuleGeometry
{
	CPxGeometry base;
	CPxReal radius;
	CPxReal halfHeight;
};

struct CPxConvexCoreGeometry
{
	CPxGeometry base;
};

struct CPxConvexMeshGeometry
{
	CPxGeometry base;
	CPxMeshScale scale;
	CPxConvexMesh* convexMesh;
	CPxConvexMeshGeometryFlag meshFlags;
};

struct CPxHeightFieldGeometry
{
	CPxGeometry base;
	CPxHeightField* heightField;
	CPxReal heightScale;
	CPxReal rowScale;
	CPxReal columnScale;
	CPxMeshGeometryFlag heightFieldFlags;
};

struct CPxParticleSystemGeometry
{
	CPxGeometry base;
};

struct CPxPlaneGeometry
{
	CPxGeometry base;
};

struct CPxSphereGeometry
{
	CPxGeometry base;
	CPxReal radius;
};

struct CPxTetrahedronMeshGeometry
{
	CPxGeometry base;
	CPxTetrahedronMesh* tetrahedronMesh;
};

struct CPxTriangleMeshGeometry
{
	CPxGeometry base;
	CPxMeshScale scale;
	CPxMeshGeometryFlag meshFlags;
	CPxTriangleMesh* triangleMesh;
};

union CPxGeometryHolder
{
	CPxGeometry base;
	CPxBoxGeometry box;
	CPxCapsuleGeometry capsule;
	CPxConvexCoreGeometry convexCore;
	CPxConvexMeshGeometry convexMesh;
	CPxHeightFieldGeometry heightField;
	CPxParticleSystemGeometry particleSystem;
	CPxPlaneGeometry plane;
	CPxSphereGeometry sphere;
	CPxTetrahedronMeshGeometry tetrahedronMesh;
	CPxTriangleMeshGeometry triangleMesh;
};

struct CPxConstraintShaderTable
{
	CPxConstraintSolverPrep* solverPrep;
	CPxConstraintVisualize* visualize;
	CPxConstraintFlag flag;
};

struct CPxDeformableAttachmentData
{
	CPxActor* actor0;
	CPxActor* actor1;
	CPxDeformableAttachmentTargetType type0;
	CPxDeformableAttachmentTargetType type1;
	CPxTypedBoundedDataU32 indices0;
	CPxTypedBoundedDataU32 indices1;
	CPxTypedBoundedDataVec4 coords0;
	CPxTypedBoundedDataVec4 coords1;
	CPxTransform pose0;
	CPxTransform pose1;
};

struct CPxDeformableElementFilterData
{
	CPxActor* actor0;
	CPxActor* actor1;
	CPxTypedBoundedDataU32 groupElementCounts0;
	CPxTypedBoundedDataU32 groupElementCounts1;
	CPxTypedBoundedDataU32 groupElementIndices0;
	CPxTypedBoundedDataU32 groupElementIndices1;
};

struct CPxDominanceGroupPair
{
	CPxU8 dominance0;
	CPxU8 dominance1;
};

struct CPxContactPair
{
	CPxShape* shapes0;
	CPxShape* shapes1;
	CPxU8* contactPatches;
	CPxU8* contactPoints;
	CPxReal* contactImpulses;
	CPxU8* frictionPatches;
	CPxU32 requiredBufferSize;
	CPxU8 contactCount;
	CPxU8 patchCount;
	CPxU8 contactStreamSize;
	CPxContactPairFlag flags;
	CPxPairFlag events;
	CPxU32 internalData0;
	CPxU32 internalData1;
};

struct CPxContactPairHeader
{
	CPxActor* actors0;
	CPxActor* actors1;
	CPxU8* extraDataStream;
	CPxU16 extraDataStreamSize;
	CPxContactPairHeaderFlag flags;
	CPxContactPair* pairs;
	CPxU32 nbPairs;
};

struct CPxGpuDynamicsMemoryConfigStatistics
{
	CPxU64 tempBufferCapacity;
	CPxU32 rigidContactCount;
	CPxU32 rigidPatchCount;
	CPxU32 foundLostPairs;
	CPxU32 foundLostAggregatePairs;
	CPxU32 totalAggregatePairs;
	CPxU32 deformableSurfaceContacts;
	CPxU32 deformableVolumeContacts;
	CPxU32 softbodyContacts;
	CPxU32 particleContacts;
	CPxU32 collisionStackSize;
};

struct CPxSimulationStatistics
{
	CPxU32 nbActiveConstraints;
	CPxU32 nbActiveDynamicBodies;
	CPxU32 nbActiveKinematicBodies;
	CPxU32 nbStaticBodies;
	CPxU32 nbDynamicBodies;
	CPxU32 nbKinemaicBodies;
	CPxU32 nbShapes[static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)];
	CPxU32 nbAggregates;
	CPxU32 nbArticulations;
	CPxU32 nbAxisSolverConstraints;
	CPxU32 compressedContactSize;
	CPxU32 requiredContactConstraintMemory;
	CPxU32 peakConstraintMemory;
	CPxU32 nbDiscreteContactPairsTotal;
	CPxU32 nbDiscreteContactPairsWithCacheHits;
	CPxU32 nbDiscreteContactPairsWithContacts;
	CPxU32 nbNewPairs;
	CPxU32 nbLostPairs;
	CPxU32 nbNewTouches;
	CPxU32 nbLostTouches;
	CPxU32 nbPartitions;
	CPxU64 gpuMemParticles;
	CPxU64 gpuMemDeformableSurfaces;
	CPxU64 gpuMemDeformableVolumes;
	CPxU64 gpuMemHeap;
	CPxU64 gpuMemHeapBroadPhase;
	CPxU64 gpuMemHeapNarrowPhase;
	CPxU64 gpuMemHeapSolver;
	CPxU64 gpuMemHeapArticulation;
	CPxU64 gpuMemHeapSimulation;
	CPxU64 gpuMemHeapSimulationArticulation;
	CPxU64 gpuMemHeapSimulationParticles;
	CPxU64 gpuMemHeapSimulationDeformableSurface;
	CPxU64 gpuMemHeapSimulationDeformableVolume;
	CPxU64 gpuMemHeapParticles;
	CPxU64 gpuMemHeapDeformableSurfaces;
	CPxU64 gpuMemHeapDeformableVolumes;
	CPxU64 gpuMemHeapOther;
	CPxGpuDynamicsMemoryConfigStatistics gpuDynamicsConfigStatistics;
	CPxU32 nbBroadPhaseAdds;
	CPxU32 nbBroadPhaseRemoves;
	CPxU32 nbDiscreteContactPairs[static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)][static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)];
	CPxU32 nbCCDPairs[static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)][static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)];
	CPxU32 nbModifiedContactPairs[static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)][static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)];
	CPxU32 nbTriggerPairs[static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)][static_cast<CPxU64>(CPxGeometryType::eGEOMETRY_COUNT)];
};

struct CPxBroadPhaseCaps
{
	CPxU32 mMaxNbRegions;
};

struct CPxBroadPhaseRegion
{
	CPxBounds3 mBounds;
	void* mUserData;
};

struct CPxBroadPhaseRegionInfo
{
	CPxBroadPhaseRegion mRegion;
	CPxU32 mNbStaticObjects;
	CPxU32 mMbDynamicObjects;
	bool mActive;
	bool mOverlap;
};

struct CPxFilterData
{
	CPxU32 word0;
	CPxU32 word1;
	CPxU32 word2;
	CPxU32 word3;
};

struct CPxQueryFilterData
{
	CPxFilterData data;
	CPxQueryFlag flags;
};

struct CPxQueryCache
{
	CPxShape* shape;
	CPxRigidActor* actor;
	CPxU32 faceIndex;
};
