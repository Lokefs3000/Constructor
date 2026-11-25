#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Sets the rebuild rate of the dynamic tree pruning structures.

	\param[in] dynamicTreeRebuildRateHint Rebuild rate of the dynamic tree pruning structures.

	\see PxSceneQueryDesc.dynamicTreeRebuildRateHint getDynamicTreeRebuildRateHint() forceRebuildDynamicTree()
	*/
	INTEROP_API void CPxSceneQuerySystemBase_setDynamicTreeRebuildRateHint(CPxSceneQuerySystemBase* cpp, CPxU32 dynamicTreeRebuildRateHint);
	
	/**
	\brief Retrieves the rebuild rate of the dynamic tree pruning structures.

	\return The rebuild rate of the dynamic tree pruning structures.

	\see PxSceneQueryDesc.dynamicTreeRebuildRateHint setDynamicTreeRebuildRateHint() forceRebuildDynamicTree()
	*/
	INTEROP_API CPxU32 CPxSceneQuerySystemBase_getDynamicTreeRebuildRateHint(CPxSceneQuerySystemBase* cpp);

	/**
	\brief Forces dynamic trees to be immediately rebuilt.

	\param[in] prunerIndex	Index of pruner containing the dynamic tree to rebuild

	\note PxScene will call this function with the PX_SCENE_PRUNER_STATIC or PX_SCENE_PRUNER_DYNAMIC value.

	\see PxSceneQueryDesc.dynamicTreeRebuildRateHint setDynamicTreeRebuildRateHint() getDynamicTreeRebuildRateHint()
	*/
	INTEROP_API void CPxSceneQuerySystemBase_forceRebuildDynamicTree(CPxSceneQuerySystemBase* cpp, CPxU32 prunerIndex);

	/**
	\brief Sets scene query update mode

	\param[in] updateMode	Scene query update mode.

	\see PxSceneQueryUpdateMode::Enum
	*/
	INTEROP_API void CPxSceneQuerySystemBase_setUpdateMode(CPxSceneQuerySystemBase* cpp, CPxSceneQueryUpdateMode updateMode);

	/**
	\brief Gets scene query update mode

	\return Current scene query update mode.

	\see PxSceneQueryUpdateMode::Enum
	*/
	INTEROP_API CPxSceneQueryUpdateMode CPxSceneQuerySystemBase_getUpdateMode(CPxSceneQuerySystemBase* cpp);

	/**
	\brief Retrieves the system's internal scene query timestamp, increased each time a change to the
	static scene query structure is performed.

	\return scene query static timestamp
	*/
	INTEROP_API CPxU32 CPxSceneQuerySystemBase_getStaticTimestamp(CPxSceneQuerySystemBase* cpp);

	/**
	\brief Flushes any changes to the scene query representation.

	This method updates the state of the scene query representation to match changes in the scene state.

	By default, these changes are buffered until the next query is submitted. Calling this function will not change
	the results from scene queries, but can be used to ensure that a query will not perform update work in the course of
	its execution.

	A thread performing updates will hold a write lock on the query structure, and thus stall other querying threads. In multithread
	scenarios it can be useful to explicitly schedule the period where this lock may be held for a significant period, so that
	subsequent queries issued from multiple threads will not block.
	*/
	INTEROP_API void CPxSceneQuerySystemBase_flushUpdates(CPxSceneQuerySystemBase* cpp);

	/**
	\brief Performs a raycast against objects in the scene, returns results in a PxRaycastBuffer object
	or via a custom user callback implementation inheriting from PxRaycastCallback.

	\note	Touching hits are not ordered.
	\note	Shooting a ray from within an object leads to different results depending on the shape type. Please check the details in user guide article SceneQuery. User can ignore such objects by employing one of the provided filter mechanisms.

	\param[in] origin		Origin of the ray.
	\param[in] unitDir		Normalized direction of the ray.
	\param[in] distance		Length of the ray. Has to be in the [0, inf) range.
	\param[out] hitCall		Raycast hit buffer or callback object used to report raycast hits.
	\param[in] hitFlags		Specifies which properties per hit should be computed and returned via the hit callback.
	\param[in] filterData	Filtering data passed to the filter shader.
	\param[in] filterCall	Custom filtering logic (optional). Only used if the corresponding #PxQueryFlag flags are set. If NULL, all hits are assumed to be blocking.
	\param[in] cache		Cached hit shape (optional). Ray is tested against cached shape first. If no hit is found the ray gets queried against the scene.
							Note: Filtering is not executed for a cached shape if supplied; instead, if a hit is found, it is assumed to be a blocking hit.
							Note: Using past touching hits as cache will produce incorrect behavior since the cached hit will always be treated as blocking.
	\param[in] queryFlags	Optional flags controlling the query.

	\return True if any touching or blocking hits were found or any hit was found in case PxQueryFlag::eANY_HIT was specified.

	\see PxRaycastCallback PxRaycastBuffer PxQueryFilterData PxQueryFilterCallback PxQueryCache PxRaycastHit PxQueryFlag PxQueryFlag::eANY_HIT PxGeometryQueryFlag
	*/
	INTEROP_API bool CPxSceneQuerySystemBase_raycast(CPxSceneQuerySystemBase* cpp, const CPxVec3* origin, const CPxVec3* unitDir, const CPxReal distance, CPxRaycastCallback* hitCall, CPxHitFlag hitFlags = CPxHitFlag::eDEFAULT, const CPxQueryFilterData* filterData = nullptr, CPxQueryFilterCallback* filterCall = nullptr, const CPxQueryCache* cache = nullptr, CPxGeometryQueryFlag queryFlags = CPxGeometryQueryFlag::eDEFAULT);

	/**
	\brief Performs a sweep test against objects in the scene, returns results in a PxSweepBuffer object
	or via a custom user callback implementation inheriting from PxSweepCallback.

	\note	Touching hits are not ordered.
	\note	If a shape from the scene is already overlapping with the query shape in its starting position,
			the hit is returned unless eASSUME_NO_INITIAL_OVERLAP was specified.

	\param[in] geometry		Geometry of object to sweep (supported types are: box, sphere, capsule, convex core, convex mesh).
	\param[in] pose			Pose of the sweep object.
	\param[in] unitDir		Normalized direction of the sweep.
	\param[in] distance		Sweep distance. Needs to be in [0, inf) range and >0 if eASSUME_NO_INITIAL_OVERLAP was specified. Will be clamped to PX_MAX_SWEEP_DISTANCE.
	\param[out] hitCall		Sweep hit buffer or callback object used to report sweep hits.
	\param[in] hitFlags		Specifies which properties per hit should be computed and returned via the hit callback.
	\param[in] filterData	Filtering data and simple logic.
	\param[in] filterCall	Custom filtering logic (optional). Only used if the corresponding #PxQueryFlag flags are set. If NULL, all hits are assumed to be blocking.
	\param[in] cache		Cached hit shape (optional). Sweep is performed against cached shape first. If no hit is found the sweep gets queried against the scene.
							Note: Filtering is not executed for a cached shape if supplied; instead, if a hit is found, it is assumed to be a blocking hit.
							Note: Using past touching hits as cache will produce incorrect behavior since the cached hit will always be treated as blocking.
	\param[in] inflation	This parameter creates a skin around the swept geometry which increases its extents for sweeping. The sweep will register a hit as soon as the skin touches a shape, and will return the corresponding distance and normal.
							Note: ePRECISE_SWEEP doesn't support inflation. Therefore the sweep will be performed with zero inflation.
	\param[in] queryFlags	Optional flags controlling the query.

	\return True if any touching or blocking hits were found or any hit was found in case PxQueryFlag::eANY_HIT was specified.

	\see PxSweepCallback PxSweepBuffer PxQueryFilterData PxQueryFilterCallback PxSweepHit PxQueryCache PxGeometryQueryFlag
	*/
	INTEROP_API bool CPxSceneQuerySystemBase_sweep(CPxSceneQuerySystemBase* cpp, const CPxGeometry* geometry, const CPxTransform* pose, const CPxVec3* unitDir, const CPxReal distance, CPxSweepCallback* hitCall, CPxHitFlag hitFlags = CPxHitFlag::eDEFAULT, const CPxQueryFilterData* filterData = nullptr, CPxQueryFilterCallback* filterCall = nullptr, const CPxQueryCache* cache = nullptr, const CPxReal inflation = 0.0f, CPxGeometryQueryFlag queryFlags = CPxGeometryQueryFlag::eDEFAULT);

	/**
	\brief Performs an overlap test of a given geometry against objects in the scene, returns results in a PxOverlapBuffer object
	or via a custom user callback implementation inheriting from PxOverlapCallback.

	\note Filtering: returning eBLOCK from user filter for overlap queries will cause a warning (see #PxQueryHitType).

	\param[in] geometry		Geometry of object to check for overlap (supported types are: box, sphere, capsule, convex core, convex mesh).
	\param[in] pose			Pose of the object.
	\param[out] hitCall		Overlap hit buffer or callback object used to report overlap hits.
	\param[in] filterData	Filtering data and simple logic. See #PxQueryFilterData #PxQueryFilterCallback
	\param[in] filterCall	Custom filtering logic (optional). Only used if the corresponding #PxQueryFlag flags are set. If NULL, all hits are assumed to overlap.
	\param[in] cache		Cached hit shape (optional). Overlap is performed against cached shape first. If no hit is found the overlap gets queried against the scene.
	\param[in] queryFlags	Optional flags controlling the query.
	Note: Filtering is not executed for a cached shape if supplied; instead, if a hit is found, it is assumed to be a blocking hit.
	Note: Using past touching hits as cache will produce incorrect behavior since the cached hit will always be treated as blocking.

	\return True if any touching or blocking hits were found or any hit was found in case PxQueryFlag::eANY_HIT was specified.

	\note eBLOCK should not be returned from user filters for overlap(). Doing so will result in undefined behavior, and a warning will be issued.
	\note If the PxQueryFlag::eNO_BLOCK flag is set, the eBLOCK will instead be automatically converted to an eTOUCH and the warning suppressed.

	\see PxOverlapCallback PxOverlapBuffer PxHitFlags PxQueryFilterData PxQueryFilterCallback PxGeometryQueryFlag
	*/
	INTEROP_API bool CPxSceneQuerySystemBase_overlap(CPxSceneQuerySystemBase* cpp, const CPxGeometry* geometry, const CPxTransform* pose, CPxOverlapCallback* hitCall, const CPxQueryFilterData* filterData = nullptr, CPxQueryFilterCallback* filterCall = nullptr, const CPxQueryCache* cache = nullptr, CPxGeometryQueryFlag queryFlags = CPxGeometryQueryFlag::eDEFAULT);
}