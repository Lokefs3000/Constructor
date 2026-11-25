#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxSceneQuerySystemBase.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxSceneQuerySystemBase, cpp)

	void CPxSceneQuerySystemBase_setDynamicTreeRebuildRateHint(CPxSceneQuerySystemBase* cpp, CPxU32 dynamicTreeRebuildRateHint)
	{
		THIS->setDynamicTreeRebuildRateHint(dynamicTreeRebuildRateHint);
	}

	CPxU32 CPxSceneQuerySystemBase_getDynamicTreeRebuildRateHint(CPxSceneQuerySystemBase* cpp)
	{
		return THIS->getDynamicTreeRebuildRateHint();
	}

	void CPxSceneQuerySystemBase_forceRebuildDynamicTree(CPxSceneQuerySystemBase* cpp, CPxU32 prunerIndex)
	{
		THIS->forceRebuildDynamicTree(prunerIndex);
	}

	void CPxSceneQuerySystemBase_setUpdateMode(CPxSceneQuerySystemBase* cpp, CPxSceneQueryUpdateMode updateMode)
	{
		THIS->setUpdateMode(CHANGE(physx::PxSceneQueryUpdateMode::Enum, updateMode));
	}

	CPxSceneQueryUpdateMode CPxSceneQuerySystemBase_getUpdateMode(CPxSceneQuerySystemBase* cpp)
	{
		return (CPxSceneQueryUpdateMode)THIS->getUpdateMode();
	}

	CPxU32 CPxSceneQuerySystemBase_getStaticTimestamp(CPxSceneQuerySystemBase* cpp)
	{
		return THIS->getStaticTimestamp();
	}

	void CPxSceneQuerySystemBase_flushUpdates(CPxSceneQuerySystemBase* cpp)
	{
		THIS->flushUpdates();
	}

	bool CPxSceneQuerySystemBase_raycast(CPxSceneQuerySystemBase* cpp, const CPxVec3* origin, const CPxVec3* unitDir, const CPxReal distance, CPxRaycastCallback* hitCall, CPxHitFlag hitFlags, const CPxQueryFilterData* filterData, CPxQueryFilterCallback* filterCall, const CPxQueryCache* cache, CPxGeometryQueryFlag queryFlags)
	{
		physx::PxQueryFilterData realFilterData = physx::PxQueryFilterData();
		if (filterData != nullptr)
			realFilterData = *(physx::PxQueryFilterData*)filterData;

		return THIS->raycast(getVec3Native(*origin), getVec3Native(*unitDir), distance, *GET(physx::PxRaycastCallback, hitCall), CHANGE(physx::PxHitFlag::Enum, hitFlags), realFilterData, GET(physx::PxQueryFilterCallback, filterCall), (const physx::PxQueryCache*)cache, CHANGE(physx::PxGeometryQueryFlag::Enum, queryFlags));
	}

	bool CPxSceneQuerySystemBase_sweep(CPxSceneQuerySystemBase* cpp, const CPxGeometry* geometry, const CPxTransform* pose, const CPxVec3* unitDir, const CPxReal distance, CPxSweepCallback* hitCall, CPxHitFlag hitFlags, const CPxQueryFilterData* filterData, CPxQueryFilterCallback* filterCall, const CPxQueryCache* cache, const CPxReal inflation, CPxGeometryQueryFlag queryFlags)
	{
		physx::PxQueryFilterData realFilterData = physx::PxQueryFilterData();
		if (filterData != nullptr)
			realFilterData = *(physx::PxQueryFilterData*)filterData;

#define FUNC(geo) THIS->sweep(geo, getTransformNative(*pose), getVec3Native(*unitDir), distance, *GET(physx::PxSweepCallback, hitCall), CHANGE(physx::PxHitFlag::Enum, hitFlags), realFilterData, GET(physx::PxQueryFilterCallback, filterCall), (const physx::PxQueryCache*)cache, inflation, CHANGE(physx::PxGeometryQueryFlag::Enum, queryFlags))
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return FUNC(getSphereGeometry(geometry));
			case CPxGeometryType::ePLANE: return FUNC(getPlaneGeometry(geometry));
			case CPxGeometryType::eCAPSULE: return FUNC(getCapsuleGeometry(geometry));
			case CPxGeometryType::eBOX: return FUNC(getBoxGeometry(geometry));
			case CPxGeometryType::eCONVEXCORE: return FUNC(getConvexCoreGeometry(geometry));
			case CPxGeometryType::eCONVEXMESH: return FUNC(getConvexMeshGeometry(geometry));
			case CPxGeometryType::ePARTICLESYSTEM: return FUNC(getParticleSystemGeometry(geometry));
			case CPxGeometryType::eTETRAHEDRONMESH: return FUNC(getTetrahedronGeometry(geometry));
			case CPxGeometryType::eTRIANGLEMESH: return FUNC(getTriangleMeshGeometry(geometry));
			case CPxGeometryType::eHEIGHTFIELD: return FUNC(getHeightFieldGeometry(geometry));
			case CPxGeometryType::eCUSTOM: return FUNC(getCustomGeometry(geometry));
			default: abort();
		}

#undef FUNC
	}

	bool CPxSceneQuerySystemBase_overlap(CPxSceneQuerySystemBase* cpp, const CPxGeometry* geometry, const CPxTransform* pose, CPxOverlapCallback* hitCall, const CPxQueryFilterData* filterData, CPxQueryFilterCallback* filterCall, const CPxQueryCache* cache, CPxGeometryQueryFlag queryFlags)
	{
		physx::PxQueryFilterData realFilterData = physx::PxQueryFilterData();
		if (filterData != nullptr)
			realFilterData = *(physx::PxQueryFilterData*)filterData;

#define FUNC(geo) THIS->overlap(geo, getTransformNative(*pose), *GET(physx::PxOverlapCallback, hitCall), realFilterData, GET(physx::PxQueryFilterCallback, filterCall), (const physx::PxQueryCache*)cache, CHANGE(physx::PxGeometryQueryFlag::Enum, queryFlags))
		switch (geometry->type)
		{
			case CPxGeometryType::eSPHERE: return FUNC(getSphereGeometry(geometry));
			case CPxGeometryType::ePLANE: return FUNC(getPlaneGeometry(geometry));
			case CPxGeometryType::eCAPSULE: return FUNC(getCapsuleGeometry(geometry));
			case CPxGeometryType::eBOX: return FUNC(getBoxGeometry(geometry));
			case CPxGeometryType::eCONVEXCORE: return FUNC(getConvexCoreGeometry(geometry));
			case CPxGeometryType::eCONVEXMESH: return FUNC(getConvexMeshGeometry(geometry));
			case CPxGeometryType::ePARTICLESYSTEM: return FUNC(getParticleSystemGeometry(geometry));
			case CPxGeometryType::eTETRAHEDRONMESH: return FUNC(getTetrahedronGeometry(geometry));
			case CPxGeometryType::eTRIANGLEMESH: return FUNC(getTriangleMeshGeometry(geometry));
			case CPxGeometryType::eHEIGHTFIELD: return FUNC(getHeightFieldGeometry(geometry));
			case CPxGeometryType::eCUSTOM: return FUNC(getCustomGeometry(geometry));
			default: abort();
		}

#undef FUNC
	}
}
