#include "pch.h"
#include "shared.hpp"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxPhysics.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxPhysics, cpp)

	CPxPhysics* CPxCreatePhysics(CPxFoundation* foundation, CPxTolerancesScale scale, bool trackOutstandingAllocations, CPxPvd* pvd)
	{
		return PxCreatePhysics(PX_PHYSICS_VERSION, *GET(physx::PxFoundation, foundation), getTolerancesScaleNative(scale), trackOutstandingAllocations, GET(physx::PxPvd, pvd));
	}

	void CPxPhysics_release(CPxPhysics* cpp)
	{
		THIS->release();
	}

	CPxAggregate* CPxPhysics_createAggregate(CPxPhysics* cpp, CPxU32 maxActor, CPxU32 maxShape, CPxAggregateFilterHint filterHint)
	{
		return THIS->createAggregate(maxActor, maxShape, filterHint);
	}

	CPxU32 CPxPhysics_getNbAggregates(CPxPhysics* cpp)
	{
		return THIS->getNbAggregates();
	}

	CPxTriangleMesh* CPxPhysics_createTriangleMesh(CPxPhysics* cpp, CPxInputStream* stream)
	{
		return THIS->createTriangleMesh(*GET(physx::PxInputStream, stream));
	}

	CPxU32 CPxPhysics_getNbTriangleMeshes(CPxPhysics* cpp)
	{
		return THIS->getNbTriangleMeshes();
	}

	CPxU32 CPxPhysics_getTriangleMeshes(CPxPhysics* cpp, CPxTriangleMesh** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getTriangleMeshes((physx::PxTriangleMesh**)userBuffer, bufferSize, startIndex);
	}

	CPxTetrahedronMesh* CPxPhysics_createTetrahedronMesh(CPxPhysics* cpp, CPxInputStream* stream)
	{
		return THIS->createTetrahedronMesh(*GET(physx::PxInputStream, stream));
	}

	CPxU32 CPxPhysics_getNbTetrahedronMeshes(CPxPhysics* cpp)
	{
		return THIS->getNbTetrahedronMeshes();
	}

	CPxU32 CPxPhysics_getTetrahedronMeshes(CPxPhysics* cpp, CPxTetrahedronMesh** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getTetrahedronMeshes((physx::PxTetrahedronMesh**)userBuffer, bufferSize, startIndex);
	}

	CPxHeightField* CPxPhysics_createHeightField(CPxPhysics* cpp, CPxInputStream* stream)
	{
		return THIS->createHeightField(*GET(physx::PxInputStream, stream));
	}

	CPxU32 CPxPhysics_getNbHeightFields(CPxPhysics* cpp)
	{
		return THIS->getNbHeightFields();
	}

	CPxU32 CPxPhysics_getHeightFields(CPxPhysics* cpp, CPxHeightField** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getHeightFields((physx::PxHeightField**)userBuffer, bufferSize, startIndex);
	}

	CPxConvexMesh* CPxPhysics_createConvexMesh(CPxPhysics* cpp, CPxInputStream* stream)
	{
		return THIS->createHeightField(*GET(physx::PxInputStream, stream));
	}

	CPxU32 CPxPhysics_getNbConvexMeshes(CPxPhysics* cpp)
	{
		return THIS->getNbConvexMeshes();
	}

	CPxU32 CPxPhysics_getConvexMeshes(CPxPhysics* cpp, CPxConvexMesh** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getConvexMeshes((physx::PxConvexMesh**)userBuffer, bufferSize, startIndex);
	}

	CPxDeformableVolumeMesh* CPxPhysics_createDeformableVolumeMesh(CPxPhysics* cpp, CPxInputStream* stream)
	{
		return THIS->createDeformableVolumeMesh(*GET(physx::PxInputStream, stream));
	}

	CPxBVH* CPxPhysics_createBVH(CPxPhysics* cpp, CPxInputStream* stream)
	{
		return THIS->createBVH(*GET(physx::PxInputStream, stream));
	}

	CPxU32 CPxPhysics_getNbBVHs(CPxPhysics* cpp)
	{
		return THIS->getNbBVHs();
	}

	CPxU32 CPxPhysics_getBVHs(CPxPhysics* cpp, CPxBVH** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getBVHs((physx::PxBVH**)userBuffer, bufferSize, startIndex);
	}

	CPxScene* CPxPhysics_createScene(CPxPhysics* cpp, CPxSceneDesc sceneDesc)
	{
		physx::PxSceneDesc realSceneDesc = physx::PxSceneDesc(THIS->getTolerancesScale());
		realSceneDesc.gravity = getVec3Native(sceneDesc.gravity);
		realSceneDesc.simulationEventCallback = GET(physx::PxSimulationEventCallback, sceneDesc.simulationEventCallback);
		realSceneDesc.contactModifyCallback = GET(physx::PxContactModifyCallback, sceneDesc.contactModifyCallback);
		realSceneDesc.ccdContactModifyCallback = GET(physx::PxCCDContactModifyCallback, sceneDesc.ccdContactModifyCallback);
		realSceneDesc.deformableSurfacePostSolveCallback = GET(physx::PxPostSolveCallback, sceneDesc.deformableSurfacePostSolveCallback);
		realSceneDesc.deformableVolumePostSolveCallback = GET(physx::PxPostSolveCallback, sceneDesc.deformableVolumePostSolveCallback);
		realSceneDesc.filterShaderData = sceneDesc.filterShaderData;
		realSceneDesc.filterShaderDataSize = sceneDesc.filterShaderDataSize;
		realSceneDesc.filterShader = *GET(physx::PxSimulationFilterShader, sceneDesc.filterShader);
		realSceneDesc.filterCallback = GET(physx::PxSimulationFilterCallback, sceneDesc.filterCallback);
		realSceneDesc.kineKineFilteringMode = CHANGE(physx::PxPairFilteringMode::Enum, sceneDesc.kineKineFilteringMode);
		realSceneDesc.staticKineFilteringMode = CHANGE(physx::PxPairFilteringMode::Enum, sceneDesc.staticKineFilteringMode);
		realSceneDesc.broadPhaseType = CHANGE(physx::PxBroadPhaseType::Enum, sceneDesc.broadPhaseType);
		realSceneDesc.broadPhaseCallback = GET(physx::PxBroadPhaseCallback, sceneDesc.broadPhaseCallback);
		realSceneDesc.gpuBroadPhaseDesc = GET(physx::PxGpuBroadPhaseDesc, sceneDesc.gpuBroadPhaseDesc);
		realSceneDesc.limits = getSceneLimitsNative(sceneDesc.limits);
		realSceneDesc.solverType = CHANGE(physx::PxSolverType::Enum, sceneDesc.solverType);
		realSceneDesc.bounceThresholdVelocity = sceneDesc.bounceThresholdVelocity;
		realSceneDesc.frictionOffsetThreshold = sceneDesc.frictionOffsetThreshold;
		realSceneDesc.frictionCorrelationDistance = sceneDesc.frictionCorrelationDistance;
		realSceneDesc.flags = CHANGE(physx::PxSceneFlag::Enum, sceneDesc.flags);
		realSceneDesc.cpuDispatcher = GET(physx::PxCpuDispatcher, sceneDesc.cpuDispatcher);
		realSceneDesc.cudaContextManager = GET(physx::PxCudaContextManager, sceneDesc.cudaContextManager);
		realSceneDesc.userData = sceneDesc.userData;
		realSceneDesc.solverBatchSize = sceneDesc.solverBatchSize;
		realSceneDesc.solverArticulationBatchSize = sceneDesc.solverArticulationBatchSize;
		realSceneDesc.nbContactDataBlocks = sceneDesc.nbContactDataBlocks;
		realSceneDesc.maxNbContactDataBlocks = sceneDesc.maxNbContactDataBlocks;
		realSceneDesc.maxBiasCoefficient = sceneDesc.maxBiasCoefficient;
		realSceneDesc.contactReportStreamBufferSize = sceneDesc.contactReportStreamBufferSize;
		realSceneDesc.ccdMaxPasses = sceneDesc.ccdMaxPasses;
		realSceneDesc.ccdThreshold = sceneDesc.ccdThreshold;
		realSceneDesc.ccdMaxSeparation = sceneDesc.ccdMaxSeparation;
		realSceneDesc.wakeCounterResetValue = sceneDesc.wakeCounterResetValue;
		realSceneDesc.sanityBounds = getBounds3Native(sceneDesc.sanityBounds);
		realSceneDesc.gpuDynamicsConfig = getGpuDynamicsMemoryConfigNative(sceneDesc.gpuDynamicsConfig);
		realSceneDesc.gpuMaxNumPartitions = sceneDesc.gpuMaxNumPartitions;
		realSceneDesc.gpuMaxNumStaticPartitions = sceneDesc.gpuMaxNumStaticPartitions;
		realSceneDesc.contactPairSlabSize = sceneDesc.contactPairSlabSize;
		realSceneDesc.sceneQuerySystem = GET(physx::PxSceneQuerySystem, sceneDesc.sceneQuerySystem);
		realSceneDesc.staticStructure = CHANGE(physx::PxPruningStructureType::Enum, sceneDesc.staticStructure);
		realSceneDesc.dynamicStructure = CHANGE(physx::PxPruningStructureType::Enum, sceneDesc.dynamicStructure);
		realSceneDesc.dynamicTreeRebuildRateHint = sceneDesc.dynamicTreeRebuildRateHint;
		realSceneDesc.dynamicTreeSecondaryPruner = CHANGE(physx::PxDynamicTreeSecondaryPruner::Enum, sceneDesc.dynamicTreeSecondaryPruner);
		realSceneDesc.staticBVHBuildStrategy = CHANGE(physx::PxBVHBuildStrategy::Enum, sceneDesc.staticBVHBuildStrategy);
		realSceneDesc.dynamicBVHBuildStrategy = CHANGE(physx::PxBVHBuildStrategy::Enum, sceneDesc.dynamicBVHBuildStrategy);
		realSceneDesc.staticNbObjectsPerNode = sceneDesc.staticNbObjectsPerNode;
		realSceneDesc.dynamicNbObjectsPerNode = sceneDesc.dynamicNbObjectsPerNode;
		realSceneDesc.sceneQueryUpdateMode = CHANGE(physx::PxSceneQueryUpdateMode::Enum, sceneDesc.sceneQueryUpdateMode);

		return THIS->createScene(realSceneDesc);
	}

	CPxU32 CPxPhysics_getNbScenes(CPxPhysics* cpp)
	{
		return THIS->getNbScenes();
	}

	CPxU32 CPxPhysics_getScenes(CPxPhysics* cpp, CPxScene** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getScenes((physx::PxScene**)userBuffer, bufferSize, startIndex);
	}

	CPxRigidStatic* CPxPhysics_createRigidStatic(CPxPhysics* cpp, CPxTransform pose)
	{
		physx::PxTransform realPose = getTransformNative(pose);
		return THIS->createRigidStatic(realPose);
	}

	CPxRigidDynamic* CPxPhysics_createRigidDynamic(CPxPhysics* cpp, CPxTransform pose)
	{
		physx::PxTransform realPose = getTransformNative(pose);
		return THIS->createRigidDynamic(realPose);
	}

	CPxPruningStructure* CPxPhysics_createPruningStructure(CPxPhysics* cpp, CPxRigidActor** actors, CPxU32 nbActors)
	{
		return THIS->createPruningStructure((physx::PxRigidActor* const*)actors, nbActors);
	}

	CPxShape* CPxPhysics_createShape_material(CPxPhysics* cpp, CPxGeometry* geometry, CPxMaterial* material, bool isExclusive, CPxShapeFlag shapeFlags)
	{
		physx::PxMaterial& realMaterial = *GET(physx::PxMaterial, material);
		physx::PxShapeFlags realShapeFlags = CHANGE(physx::PxShapeFlags, shapeFlags);
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return THIS->createShape(getSphereGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::ePLANE: return THIS->createShape(getPlaneGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCAPSULE: return THIS->createShape(getCapsuleGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eBOX: return THIS->createShape(getBoxGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXCORE: return THIS->createShape(getConvexCoreGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXMESH: return THIS->createShape(getConvexMeshGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::ePARTICLESYSTEM: return THIS->createShape(getParticleSystemGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eTETRAHEDRONMESH: return THIS->createShape(getTetrahedronGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eTRIANGLEMESH: return THIS->createShape(getTriangleMeshGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eHEIGHTFIELD: return THIS->createShape(getHeightFieldGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCUSTOM: return THIS->createShape(getCustomGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			default: abort();
		}
	}

	CPxShape* CPxPhysics_createShape_deformableVolume(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableVolumeMaterial* material, bool isExclusive, CPxShapeFlag shapeFlags)
	{
		physx::PxDeformableVolumeMaterial& realMaterial = *GET(physx::PxDeformableVolumeMaterial, material);
		physx::PxShapeFlags realShapeFlags = CHANGE(physx::PxShapeFlags, shapeFlags);
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return THIS->createShape(getSphereGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::ePLANE: return THIS->createShape(getPlaneGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCAPSULE: return THIS->createShape(getCapsuleGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eBOX: return THIS->createShape(getBoxGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXCORE: return THIS->createShape(getConvexCoreGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXMESH: return THIS->createShape(getConvexMeshGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::ePARTICLESYSTEM: return THIS->createShape(getParticleSystemGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eTETRAHEDRONMESH: return THIS->createShape(getTetrahedronGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eTRIANGLEMESH: return THIS->createShape(getTriangleMeshGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eHEIGHTFIELD: return THIS->createShape(getHeightFieldGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCUSTOM: return THIS->createShape(getCustomGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			default: abort();
		}
	}

	CPxShape* CPxPhysics_createShape_deformableSurface(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableSurfaceMaterial* material, bool isExclusive, CPxShapeFlag shapeFlags)
	{
		physx::PxDeformableSurfaceMaterial& realMaterial = *GET(physx::PxDeformableSurfaceMaterial, material);
		physx::PxShapeFlags realShapeFlags = CHANGE(physx::PxShapeFlags, shapeFlags);
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return THIS->createShape(getSphereGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::ePLANE: return THIS->createShape(getPlaneGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCAPSULE: return THIS->createShape(getCapsuleGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eBOX: return THIS->createShape(getBoxGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXCORE: return THIS->createShape(getConvexCoreGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXMESH: return THIS->createShape(getConvexMeshGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::ePARTICLESYSTEM: return THIS->createShape(getParticleSystemGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eTETRAHEDRONMESH: return THIS->createShape(getTetrahedronGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eTRIANGLEMESH: return THIS->createShape(getTriangleMeshGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eHEIGHTFIELD: return THIS->createShape(getHeightFieldGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			case CPxGeometryType::eCUSTOM: return THIS->createShape(getCustomGeometry(geometry), realMaterial, isExclusive, realShapeFlags);
			default: abort();
		}
	}

	CPxShape* CPxPhysics_createShape_materials(CPxPhysics* cpp, CPxGeometry* geometry, CPxMaterial** materials, CPxU16 materialCount, bool isExclusive, CPxShapeFlag shapeFlags)
	{
		physx::PxMaterial* const* realMaterials = (physx::PxMaterial* const*)materials;
		physx::PxShapeFlags realShapeFlags = CHANGE(physx::PxShapeFlags, shapeFlags);
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return THIS->createShape(getSphereGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::ePLANE: return THIS->createShape(getPlaneGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCAPSULE: return THIS->createShape(getCapsuleGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eBOX: return THIS->createShape(getBoxGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXCORE: return THIS->createShape(getConvexCoreGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXMESH: return THIS->createShape(getConvexMeshGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::ePARTICLESYSTEM: return THIS->createShape(getParticleSystemGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eTETRAHEDRONMESH: return THIS->createShape(getTetrahedronGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eTRIANGLEMESH: return THIS->createShape(getTriangleMeshGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eHEIGHTFIELD: return THIS->createShape(getHeightFieldGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCUSTOM: return THIS->createShape(getCustomGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			default: abort();
		}
	}

	CPxShape* CPxPhysics_createShape_deformableVolumes(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableVolumeMaterial** materials, CPxU16 materialCount, bool isExclusive, CPxShapeFlag shapeFlags)
	{
		physx::PxDeformableVolumeMaterial* const* realMaterials = (physx::PxDeformableVolumeMaterial* const*)materials;
		physx::PxShapeFlags realShapeFlags = CHANGE(physx::PxShapeFlags, shapeFlags);
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return THIS->createShape(getSphereGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::ePLANE: return THIS->createShape(getPlaneGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCAPSULE: return THIS->createShape(getCapsuleGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eBOX: return THIS->createShape(getBoxGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXCORE: return THIS->createShape(getConvexCoreGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXMESH: return THIS->createShape(getConvexMeshGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::ePARTICLESYSTEM: return THIS->createShape(getParticleSystemGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eTETRAHEDRONMESH: return THIS->createShape(getTetrahedronGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eTRIANGLEMESH: return THIS->createShape(getTriangleMeshGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eHEIGHTFIELD: return THIS->createShape(getHeightFieldGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCUSTOM: return THIS->createShape(getCustomGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			default: abort();
		}
	}

	CPxShape* CPxPhysics_createShape_deformableSurfaces(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableSurfaceMaterial** materials, CPxU16 materialCount, bool isExclusive, CPxShapeFlag shapeFlags)
	{
		physx::PxDeformableSurfaceMaterial* const* realMaterials = (physx::PxDeformableSurfaceMaterial* const*)materials;
		physx::PxShapeFlags realShapeFlags = CHANGE(physx::PxShapeFlags, shapeFlags);
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return THIS->createShape(getSphereGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::ePLANE: return THIS->createShape(getPlaneGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCAPSULE: return THIS->createShape(getCapsuleGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eBOX: return THIS->createShape(getBoxGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXCORE: return THIS->createShape(getConvexCoreGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCONVEXMESH: return THIS->createShape(getConvexMeshGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::ePARTICLESYSTEM: return THIS->createShape(getParticleSystemGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eTETRAHEDRONMESH: return THIS->createShape(getTetrahedronGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eTRIANGLEMESH: return THIS->createShape(getTriangleMeshGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eHEIGHTFIELD: return THIS->createShape(getHeightFieldGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			case CPxGeometryType::eCUSTOM: return THIS->createShape(getCustomGeometry(geometry), realMaterials, materialCount, isExclusive, realShapeFlags);
			default: abort();
		}
	}

	CPxU32 CPxPhysics_getNbShapes(CPxPhysics* cpp)
	{
		return THIS->getNbShapes();
	}

	CPxU32 CPxPhysics_getShapes(CPxPhysics* cpp, CPxShape** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getShapes((physx::PxShape**)userBuffer, bufferSize, startIndex);
	}

	CPxConstraint* CPxPhysics_createConstraint(CPxPhysics* cpp, CPxRigidActor* actor0, CPxRigidActor* actor1, CPxConstraintConnector* connector, CPxConstraintShaderTable shaders, CPxU32 dataSize)
	{
		physx::PxConstraintShaderTable realShaders = {
			.solverPrep = *GET(physx::PxConstraintSolverPrep, shaders.solverPrep),
			.visualize = *GET(physx::PxConstraintVisualize, shaders.visualize),
			.flag = CHANGE(physx::PxConstraintFlag::Enum, shaders.flag)
		};

		return THIS->createConstraint(GET(physx::PxRigidActor, actor0), GET(physx::PxRigidActor, actor1), *GET(physx::PxConstraintConnector, connector), realShaders, dataSize);
	}

	CPxU32 CPxPhysics_getNbConstraints(CPxPhysics* cpp)
	{
		return THIS->getNbConstraints();
	}

	CPxU32 CPxPhysics_getConstraints(CPxPhysics* cpp, CPxConstraint** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getConstraints((physx::PxConstraint**)userBuffer, bufferSize, startIndex);
	}

	CPxArticulationReducedCoordinate* CPxPhysics_createArticulationReducedCoordinate(CPxPhysics* cpp)
	{
		return THIS->createArticulationReducedCoordinate();
	}

	CPxU32 CPxPhysics_getNbArticulations(CPxPhysics* cpp)
	{
		return THIS->getNbArticulations();
	}

	CPxDeformableAttachment* CPxPhysics_createDeformableAttachment(CPxPhysics* cpp, CPxDeformableAttachmentData data)
	{
		physx::PxDeformableAttachmentData realData;
		realData.actor[0] = GET(physx::PxActor, data.actor0);
		realData.type[0] = CHANGE(physx::PxDeformableAttachmentTargetType::Enum, data.type0);
		realData.indices[0] = getTypedBoundedDataU32Native(data.indices0);
		realData.coords[0] = getTypedBoundedDataVec4Native(data.coords0);

		realData.actor[1] = GET(physx::PxActor, data.actor1);
		realData.type[1] = CHANGE(physx::PxDeformableAttachmentTargetType::Enum, data.type1);
		realData.indices[1] = getTypedBoundedDataU32Native(data.indices1);
		realData.coords[1] = getTypedBoundedDataVec4Native(data.coords1);

		return THIS->createDeformableAttachment(realData);
	}

	CPxDeformableElementFilter* CPxPhysics_createDeformableElementFilter(CPxPhysics* cpp, CPxDeformableElementFilterData data)
	{
		physx::PxDeformableElementFilterData realData;
		realData.actor[0] = GET(physx::PxActor, data.actor0);
		realData.groupElementCounts[0] = getTypedBoundedDataU32Native(data.groupElementCounts0);
		realData.groupElementIndices[0] = getTypedBoundedDataU32Native(data.groupElementIndices0);

		realData.actor[1] = GET(physx::PxActor, data.actor1);
		realData.groupElementCounts[1] = getTypedBoundedDataU32Native(data.groupElementCounts1);
		realData.groupElementIndices[1] = getTypedBoundedDataU32Native(data.groupElementIndices1);

		return THIS->createDeformableElementFilter(realData);
	}

	CPxDeformableSurface* CPxPhysics_createDeformableSurface(CPxPhysics* cpp, CPxCudaContextManager* cudaContextManager)
	{
		return THIS->createDeformableSurface(*GET(physx::PxCudaContextManager, cudaContextManager));
	}

	CPxDeformableVolume* CPxPhysics_createDeformableVolume(CPxPhysics* cpp, CPxCudaContextManager* cudaContextManager)
	{
		return THIS->createDeformableVolume(*GET(physx::PxCudaContextManager, cudaContextManager));
	}

	CPxPBDParticleSystem* CPxPhysics_createPBDParticleSystem(CPxPhysics* cpp, CPxCudaContextManager* cudaContextManager, CPxU32 maxNeighborhood, CPxReal neighborhoodScale)
	{
		return THIS->createPBDParticleSystem(*GET(physx::PxCudaContextManager, cudaContextManager), maxNeighborhood, neighborhoodScale);
	}

	CPxParticleBuffer* CPxPhysics_createParticleBuffer(CPxPhysics* cpp, CPxU32 maxParticles, CPxCudaContextManager* cudaContextManager)
	{
		return THIS->createParticleBuffer(maxParticles, GET(physx::PxCudaContextManager, cudaContextManager));
	}

	CPxParticleAndDiffuseBuffer* CPxPhysics_createParticleAndDiffuseBuffer(CPxPhysics* cpp, CPxU32 maxParticles, CPxU32 maxDiffuseParticles, CPxCudaContextManager* cudaContextManager)
	{
		return THIS->createParticleAndDiffuseBuffer(maxParticles, maxDiffuseParticles, GET(physx::PxCudaContextManager, cudaContextManager));
	}

	CPxMaterial* CPxPhysics_createMaterial(CPxPhysics* cpp, CPxReal staticFriction, CPxReal dynamicFriction, CPxReal restitution)
	{
		return THIS->createMaterial(staticFriction, dynamicFriction, restitution);
	}

	CPxU32 CPxPhysics_getNbMaterials(CPxPhysics* cpp)
	{
		return THIS->getNbMaterials();
	}

	CPxU32 CPxPhysics_getMaterials(CPxPhysics* cpp, CPxMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getMaterials((physx::PxMaterial**)userBuffer, bufferSize, startIndex);
	}

	CPxDeformableSurfaceMaterial* CPxPhysics_createDeformableSurfaceMaterial(CPxPhysics* cpp, CPxReal youngs, CPxReal poissons, CPxReal dynamicFriction, CPxReal thickness, CPxReal bendingStiffness, CPxReal elasticityDamping, CPxReal bendingDamping)
	{
		return THIS->createDeformableSurfaceMaterial(youngs, poissons, dynamicFriction, thickness, bendingStiffness, elasticityDamping, bendingDamping);
	}

	CPxU32 CPxPhysics_getNbDeformableSurfaceMaterials(CPxPhysics* cpp)
	{
		return THIS->getNbDeformableSurfaceMaterials();
	}

	CPxU32 CPxPhysics_getDeformableSurfaceMaterials(CPxPhysics* cpp, CPxDeformableSurfaceMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getDeformableSurfaceMaterials((physx::PxDeformableSurfaceMaterial**)userBuffer, bufferSize, startIndex);
	}

	CPxDeformableVolumeMaterial* CPxPhysics_createDeformableVolumeMaterial(CPxPhysics* cpp, CPxReal youngs, CPxReal poissons, CPxReal dynamicFriction, CPxReal elasticityDamping)
	{
		return THIS->createDeformableVolumeMaterial(youngs, poissons, dynamicFriction, elasticityDamping);
	}

	CPxU32 CPxPhysics_getNbDeformableVolumeMaterials(CPxPhysics* cpp)
	{
		return THIS->getNbDeformableVolumeMaterials();
	}

	CPxU32 CPxPhysics_getDeformableVolumeMaterials(CPxPhysics* cpp, CPxDeformableVolumeMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getDeformableVolumeMaterials((physx::PxDeformableVolumeMaterial**)userBuffer, bufferSize, startIndex);
	}

	CPxPBDMaterial* CPxPhysics_createPDBMaterial(CPxPhysics* cpp, CPxReal friction, CPxReal damping, CPxReal adhesion, CPxReal viscosity, CPxReal vorticityConfinement, CPxReal surfaceTension, CPxReal cohesion, CPxReal lift, CPxReal drag, CPxReal cflCoefficient, CPxReal gravityScale)
	{
		return THIS->createPBDMaterial(friction, damping, adhesion, viscosity, vorticityConfinement, surfaceTension, cohesion, lift, drag, cflCoefficient, gravityScale);
	}

	CPxU32 CPxPhysics_getNbPBDMaterials(CPxPhysics* cpp)
	{
		return THIS->getNbPBDMaterials();
	}

	CPxU32 CPxPhysics_getPBDMaterials(CPxPhysics* cpp, CPxPBDMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getPBDMaterials((physx::PxPBDMaterial**)userBuffer, bufferSize, startIndex);
	}

	void CPxPhysics_registerDeletionListener(CPxPhysics* cpp, CPxDeletionListener* observer, CPxDeletionEventFlag deletionEvents, bool restrictedObjectSet)
	{
		THIS->registerDeletionListener(*GET(physx::PxDeletionListener, observer), CHANGE(physx::PxDeletionEventFlags, deletionEvents), restrictedObjectSet);
	}

	void CPxPhysics_unregisterDeletionListener(CPxPhysics* cpp, CPxDeletionListener* observer)
	{
		THIS->unregisterDeletionListener(*GET(physx::PxDeletionListener, observer));
	}

	void CPxPhysics_registerDeletionListenerObjects(CPxPhysics* cpp, CPxDeletionListener* observer, CPxBase** observables, CPxU32 observableCount)
	{
		THIS->registerDeletionListenerObjects(*GET(physx::PxDeletionListener, observer), (const physx::PxBase* const*)observables, observableCount);
	}

	void CPxPhysics_unregisterDeletionListenerObjects(CPxPhysics* cpp, CPxDeletionListener* observer, CPxBase** observables, CPxU32 observableCount)
	{
		THIS->unregisterDeletionListenerObjects(*GET(physx::PxDeletionListener, observer), (const physx::PxBase* const*)observables, observableCount);
	}

	CPxPhysics* CPxGetPhysics()
	{
		return &PxGetPhysics();
	}
}