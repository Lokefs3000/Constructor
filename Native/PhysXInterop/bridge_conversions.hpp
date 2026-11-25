#pragma once

#include <cassert>

#include "shared.hpp"
#include "bridge_types.hpp"

#include <PxPhysicsAPI.h>

inline physx::PxVec3 getVec3Native(CPxVec3 val)
{
	return reinterpret_cast<physx::PxVec3&>(val);
}

inline CPxVec3 getVec3Aliased(physx::PxVec3 val)
{
	return reinterpret_cast<CPxVec3&>(val);
}

inline physx::PxBounds3 getBounds3Native(CPxBounds3 val)
{
	return reinterpret_cast<physx::PxBounds3&>(val);
}

inline CPxBounds3 getBounds3Aliased(physx::PxBounds3 val)
{
	return reinterpret_cast<CPxBounds3&>(val);
}

inline physx::PxSceneLimits getSceneLimitsNative(CPxSceneLimits val)
{
	physx::PxSceneLimits limits;
	limits.maxNbActors = val.maxNbActors;
	limits.maxNbBodies = val.maxNbBodies;
	limits.maxNbStaticShapes = val.maxNbStaticShapes;
	limits.maxNbDynamicShapes = val.maxNbDynamicShapes;
	limits.maxNbAggregates = val.maxNbAggregates;
	limits.maxNbConstraints = val.maxNbConstraints;
	limits.maxNbRegions = val.maxNbRegions;
	limits.maxNbBroadPhaseOverlaps = val.maxNbBroadPhaseOverlaps;

	return limits;
}

inline CPxSceneLimits getSceneLimitsAliased(physx::PxSceneLimits val)
{
	CPxSceneLimits limits;
	limits.maxNbActors = val.maxNbActors;
	limits.maxNbBodies = val.maxNbBodies;
	limits.maxNbStaticShapes = val.maxNbStaticShapes;
	limits.maxNbDynamicShapes = val.maxNbDynamicShapes;
	limits.maxNbAggregates = val.maxNbAggregates;
	limits.maxNbConstraints = val.maxNbConstraints;
	limits.maxNbRegions = val.maxNbRegions;
	limits.maxNbBroadPhaseOverlaps = val.maxNbBroadPhaseOverlaps;

	return limits;
}

inline physx::PxGpuDynamicsMemoryConfig getGpuDynamicsMemoryConfigNative(CPxGpuDynamicsMemoryConfig val)
{
	physx::PxGpuDynamicsMemoryConfig memoryConfig;
	memoryConfig.tempBufferCapacity = val.tempBufferCapacity;
	memoryConfig.maxRigidContactCount = val.maxRigidContactCount;
	memoryConfig.maxRigidPatchCount = val.maxRigidPatchCount;
	memoryConfig.heapCapacity = val.heapCapacity;
	memoryConfig.foundLostPairsCapacity = val.foundLostPairsCapacity;
	memoryConfig.foundLostAggregatePairsCapacity = val.foundLostAggregatePairsCapacity;
	memoryConfig.totalAggregatePairsCapacity = val.totalAggregatePairsCapacity;
	memoryConfig.maxDeformableSurfaceContacts = val.maxDeformableSurfaceContacts;
	memoryConfig.maxDeformableVolumeContacts = val.maxDeformableVolumeContacts;
	memoryConfig.maxParticleContacts = val.maxParticleContacts;
	memoryConfig.collisionStackSize = val.collisionStackSize;

	return memoryConfig;
}

inline CPxGpuDynamicsMemoryConfig getGpuDynamicsMemoryConfigAliased(physx::PxGpuDynamicsMemoryConfig val)
{
	CPxGpuDynamicsMemoryConfig memoryConfig;
	memoryConfig.tempBufferCapacity = val.tempBufferCapacity;
	memoryConfig.maxRigidContactCount = val.maxRigidContactCount;
	memoryConfig.maxRigidPatchCount = val.maxRigidPatchCount;
	memoryConfig.heapCapacity = val.heapCapacity;
	memoryConfig.foundLostPairsCapacity = val.foundLostPairsCapacity;
	memoryConfig.foundLostAggregatePairsCapacity = val.foundLostAggregatePairsCapacity;
	memoryConfig.totalAggregatePairsCapacity = val.totalAggregatePairsCapacity;
	memoryConfig.maxDeformableSurfaceContacts = val.maxDeformableSurfaceContacts;
	memoryConfig.maxDeformableVolumeContacts = val.maxDeformableVolumeContacts;
	memoryConfig.maxParticleContacts = val.maxParticleContacts;
	memoryConfig.collisionStackSize = val.collisionStackSize;

	return memoryConfig;
}

inline physx::PxTransform getTransformNative(CPxTransform val)
{
	return reinterpret_cast<physx::PxTransform&>(val);
}

inline CPxTransform getTransformAliased(physx::PxTransform val)
{
	return reinterpret_cast<CPxTransform&>(val);
}

inline physx::PxMeshScale getMeshScaleNative(CPxMeshScale val)
{
	return reinterpret_cast<physx::PxMeshScale&>(val);
}

inline CPxMeshScale getMeshScaleAliased(physx::PxMeshScale val)
{
	return reinterpret_cast<CPxMeshScale&>(val);
}

inline physx::PxSphereGeometry getSphereGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eSPHERE);
	CPxSphereGeometry& castGeometry = *(CPxSphereGeometry*)geometry;
	return physx::PxSphereGeometry(castGeometry.radius);
}

inline physx::PxPlaneGeometry getPlaneGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::ePLANE);
	return physx::PxPlaneGeometry();
}

inline physx::PxCapsuleGeometry getCapsuleGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eCAPSULE);
	CPxCapsuleGeometry& castGeometry = *(CPxCapsuleGeometry*)geometry;
	return physx::PxCapsuleGeometry(castGeometry.radius, castGeometry.halfHeight);
}

inline physx::PxBoxGeometry getBoxGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eBOX);
	CPxBoxGeometry& castGeometry = *(CPxBoxGeometry*)geometry;
	return physx::PxBoxGeometry(getVec3Native(castGeometry.halfExtents));
}

inline physx::PxConvexCoreGeometry getConvexCoreGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eCONVEXCORE);
	return physx::PxConvexCoreGeometry();
}

inline physx::PxConvexMeshGeometry getConvexMeshGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eCONVEXMESH);
	CPxConvexMeshGeometry& castGeometry = *(CPxConvexMeshGeometry*)geometry;
	return physx::PxConvexMeshGeometry(
		GET(physx::PxConvexMesh, castGeometry.convexMesh),
		getMeshScaleNative(castGeometry.scale),
		CHANGE(physx::PxConvexMeshGeometryFlags, castGeometry.meshFlags));
}

inline physx::PxParticleSystemGeometry getParticleSystemGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::ePARTICLESYSTEM);
	return physx::PxParticleSystemGeometry();
}

inline physx::PxTetrahedronMeshGeometry getTetrahedronGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eTETRAHEDRONMESH);
	CPxTetrahedronMeshGeometry& castGeometry = *(CPxTetrahedronMeshGeometry*)geometry;
	return physx::PxTetrahedronMeshGeometry(GET(physx::PxTetrahedronMesh, castGeometry.tetrahedronMesh));
}

inline physx::PxTriangleMeshGeometry getTriangleMeshGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eTRIANGLEMESH);
	CPxTriangleMeshGeometry& castGeometry = *(CPxTriangleMeshGeometry*)geometry;
	return physx::PxTriangleMeshGeometry(
		GET(physx::PxTriangleMesh, castGeometry.triangleMesh),
		getMeshScaleNative(castGeometry.scale),
		CHANGE(physx::PxMeshGeometryFlags, castGeometry.meshFlags));
}

inline physx::PxHeightFieldGeometry getHeightFieldGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eHEIGHTFIELD);
	CPxHeightFieldGeometry& castGeometry = *(CPxHeightFieldGeometry*)geometry;
	return physx::PxHeightFieldGeometry(
		GET(physx::PxHeightField, castGeometry.heightField),
		CHANGE(physx::PxMeshGeometryFlags, castGeometry.heightFieldFlags),
		castGeometry.heightScale,
		castGeometry.rowScale,
		castGeometry.columnScale);
}

inline physx::PxCustomGeometry getCustomGeometry(const CPxGeometry* geometry)
{
	assert(geometry->type == CPxGeometryType::eCUSTOM);
	abort();
}

inline physx::PxTypedBoundedData<const physx::PxU32> getTypedBoundedDataU32Native(CPxTypedBoundedDataU32 val)
{
	physx::PxTypedBoundedData<const physx::PxU32> native(val.data, val.stride, val.count);
	return native;
}

inline physx::PxTypedBoundedData<const physx::PxVec4> getTypedBoundedDataVec4Native(CPxTypedBoundedDataVec4 val)
{
	physx::PxTypedBoundedData<const physx::PxVec4> native((const physx::PxVec4*)val.data, val.stride, val.count);
	return native;
}

inline physx::PxTolerancesScale getTolerancesScaleNative(CPxTolerancesScale val)
{
	physx::PxTolerancesScale native(val.length, val.speed);
	return native;
}

inline physx::PxDominanceGroupPair getDominanceGroupPairNative(CPxDominanceGroupPair val)
{
	return reinterpret_cast<physx::PxDominanceGroupPair&>(val);
}

inline CPxDominanceGroupPair getDominanceGroupPairAliased(physx::PxDominanceGroupPair val)
{
	return reinterpret_cast<CPxDominanceGroupPair&>(val);
}

inline physx::PxFilterData getFilterDataNative(CPxFilterData val)
{
	return reinterpret_cast<physx::PxFilterData&>(val);
}

inline CPxFilterData getFilterDataAliased(physx::PxFilterData val)
{
	return reinterpret_cast<CPxFilterData&>(val);
}