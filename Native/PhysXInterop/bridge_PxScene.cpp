#include "pch.h"
#include "shared.hpp"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxScene.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxScene, cpp)

	void CPxScene_release(CPxScene* cpp)
	{
		THIS->release();
	}

	void CPxScene_setFlag(CPxScene* cpp, CPxSceneFlag flag, bool value)
	{
		THIS->setFlag(CHANGE(physx::PxSceneFlag::Enum, flag), value);
	}

	CPxSceneFlag CPxScene_getFlags(CPxScene* cpp)
	{
		return (CPxSceneFlag)(CPxU16)THIS->getFlags();
	}

	void CPxScene_setLimits(CPxScene* cpp, CPxSceneLimits limits)
	{
		physx::PxSceneLimits realSceneLimits = getSceneLimitsNative(limits);
		THIS->setLimits(realSceneLimits);
	}

	CPxSceneLimits CPxScene_getLimits(CPxScene* cpp)
	{
		return getSceneLimitsAliased(THIS->getLimits());
	}

	CPxPhysics* CPxScene_getPhysics(CPxScene* cpp)
	{
		return &THIS->getPhysics();
	}

	CPxU32 CPxScene_getTimestamp(CPxScene* cpp)
	{
		return THIS->getTimestamp();
	}

	void CPxScene_setName(CPxScene* cpp, const char* name)
	{
		THIS->setName(name);
	}

	const char* CPxScene_getName(CPxScene* cpp)
	{
		return THIS->getName();
	}

	bool CPxScene_addArticulation(CPxScene* cpp, CPxArticulationReducedCoordinate* articulation)
	{
		return THIS->addArticulation(*GET(physx::PxArticulationReducedCoordinate, articulation));
	}

	void CPxScene_removeArticulation(CPxScene* cpp, CPxArticulationReducedCoordinate* articulation, bool wakeOnLostTouch)
	{
		THIS->removeArticulation(*GET(physx::PxArticulationReducedCoordinate, articulation), wakeOnLostTouch);
	}

	bool CPxScene_addActor(CPxScene* cpp, CPxActor* actor, CPxBVH* bvh)
	{
		return THIS->addActor(*GET(physx::PxActor, actor), GET(physx::PxBVH, bvh));
	}

	bool CPxScene_addActors(CPxScene* cpp, CPxActor** actors, CPxU32 nbActors)
	{
		return THIS->addActors((physx::PxActor* const*)actors, nbActors);
	}

	bool CPxScene_addActors_pruningStructure(CPxScene* cpp, CPxPruningStructure* pruningStructure)
	{
		return THIS->addActors(*GET(physx::PxPruningStructure, pruningStructure));
	}

	void CPxScene_removeActor(CPxScene* cpp, CPxActor* actor, bool wakeOnLostTouch)
	{
		THIS->removeActor(*GET(physx::PxActor, actor), wakeOnLostTouch);
	}

	void CPxScene_removeActors(CPxScene* cpp, CPxActor** actors, CPxU32 nbActors, bool wakeOnLostTouch)
	{
		THIS->removeActors((physx::PxActor* const*)actors, nbActors, wakeOnLostTouch);
	}

	bool CPxScene_addAggregate(CPxScene* cpp, CPxAggregate* aggregate)
	{
		return THIS->addAggregate(*GET(physx::PxAggregate, aggregate));
	}

	void CPxScene_removeAggregate(CPxScene* cpp, CPxAggregate* aggregate, bool wakeOnLostTouch)
	{
		THIS->removeAggregate(*GET(physx::PxAggregate, aggregate), wakeOnLostTouch);
	}

	bool CPxScene_addCollection(CPxScene* cpp, CPxCollection* collection)
	{
		return THIS->addCollection(*GET(physx::PxCollection, collection));
	}

	CPxU32 CPxScene_getNbActors(CPxScene* cpp, CPxActorTypeFlag types)
	{
		return THIS->getNbActors(CHANGE(physx::PxActorTypeFlag::Enum, types));
	}

	CPxU32 CPxScene_getActors(CPxScene* cpp, CPxActorTypeFlag types, CPxActor** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getActors(CHANGE(physx::PxActorTypeFlag::Enum, types), (physx::PxActor**)userBuffer, bufferSize, startIndex);
	}

	CPxActor** CPxScene_getActiveActors(CPxScene* cpp, CPxU32* nbActorsOut)
	{
		return (CPxActor**)THIS->getActiveActors(*nbActorsOut);
	}

	CPxU32 CPxScene_getNbDeformableSurfaces(CPxScene* cpp)
	{
		return THIS->getNbDeformableSurfaces();
	}

	CPxU32 CPxScene_getDeformableSurfaces(CPxScene* cpp, CPxDeformableSurface** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getDeformableSurfaces((physx::PxDeformableSurface**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxScene_getNbDeformableVolumes(CPxScene* cpp)
	{
		return THIS->getNbDeformableVolumes();
	}

	CPxU32 CPxScene_getDeformableVolumes(CPxScene* cpp, CPxDeformableVolume** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getDeformableVolumes((physx::PxDeformableVolume**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxScene_getNbPBDParticleSystems(CPxScene* cpp)
	{
		return THIS->getNbPBDParticleSystems();
	}

	CPxU32 CPxScene_getPDBParticleSystems(CPxScene* cpp, CPxPBDParticleSystem** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getPBDParticleSystems((physx::PxPBDParticleSystem**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxScene_getNbArticulations(CPxScene* cpp)
	{
		return THIS->getNbArticulations();
	}

	CPxU32 CPxScene_getArticulations(CPxScene* cpp, CPxArticulationReducedCoordinate** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getArticulations((physx::PxArticulationReducedCoordinate**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxScene_getNbConstraints(CPxScene* cpp)
	{
		return THIS->getNbConstraints();
	}

	CPxU32 CPxScene_getConstraints(CPxScene* cpp, CPxConstraint** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getConstraints((physx::PxConstraint**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxScene_getNbAggregates(CPxScene* cpp)
	{
		return THIS->getNbAggregates();
	}

	CPxU32 CPxScene_getAggregates(CPxScene* cpp, CPxAggregate** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getAggregates((physx::PxAggregate**)userBuffer, bufferSize, startIndex);
	}

	void CPxScene_setDominanceGroupPair(CPxScene* cpp, CPxDominanceGroup group1, CPxDominanceGroup group2, CPxDominanceGroupPair dominance)
	{
		physx::PxDominanceGroupPair realDominance = getDominanceGroupPairNative(dominance);
		THIS->setDominanceGroupPair(group1, group2, realDominance);
	}

	CPxDominanceGroupPair CPxScene_getDominanceGroupPair(CPxScene* cpp, CPxDominanceGroup group1, CPxDominanceGroup group2)
	{
		return getDominanceGroupPairAliased(THIS->getDominanceGroupPair(group1, group2));
	}

	CPxCpuDispatcher* CPxScene_getCpuDispatcher(CPxScene* cpp)
	{
		return THIS->getCpuDispatcher();
	}

	CPxCudaContextManager* CPxScene_getCudaContextManager(CPxScene* cpp)
	{
		return THIS->getCudaContextManager();
	}

	CPxClientID CPxScene_createClient(CPxScene* cpp)
	{
		return THIS->createClient();
	}

	void CPxScene_setSimulationEventCallback(CPxScene* cpp, CPxSimulationEventCallback* callback)
	{
		THIS->setSimulationEventCallback(GET(physx::PxSimulationEventCallback, callback));
	}

	CPxSimulationEventCallback* CPxScene_getSimulationEventCallback(CPxScene* cpp)
	{
		return THIS->getSimulationEventCallback();
	}

	void CPxScene_setContactModifyCallback(CPxScene* cpp, CPxContactModifyCallback* callback)
	{
		THIS->setContactModifyCallback(GET(physx::PxContactModifyCallback, callback));
	}

	void CPxScene_setCCDContactModifyCallback(CPxScene* cpp, CPxCCDContactModifyCallback* callback)
	{
		THIS->setCCDContactModifyCallback(GET(physx::PxCCDContactModifyCallback, callback));
	}

	CPxContactModifyCallback* CPxScene_getContactModifyCallback(CPxScene* cpp)
	{
		return THIS->getContactModifyCallback();
	}

	CPxCCDContactModifyCallback* CPxScene_getCCDContactModifyCallback(CPxScene* cpp)
	{
		return THIS->getCCDContactModifyCallback();
	}

	void CPxScene_setBroadPhaseCallback(CPxScene* cpp, CPxBroadPhaseCallback* callback)
	{
		THIS->setBroadPhaseCallback(GET(physx::PxBroadPhaseCallback, callback));
	}

	CPxBroadPhaseCallback* CPxScene_getBroadPhaseCallback(CPxScene* cpp)
	{
		return THIS->getBroadPhaseCallback();
	}

	void CPxScene_setFilterShaderData(CPxScene* cpp, const void* data, CPxU32 dataSize)
	{
		THIS->setFilterShaderData(data, dataSize);
	}

	const void* CPxScene_getFilterShaderData(CPxScene* cpp)
	{
		return THIS->getFilterShaderData();
	}

	CPxU32 CPxScene_getFilterShaderDataSize(CPxScene* cpp)
	{
		return THIS->getFilterShaderDataSize();
	}

	CPxSimulationFilterShader* CPxScene_getFilterShader(CPxScene* cpp)
	{
		return THIS->getFilterShader();
	}

	CPxSimulationFilterCallback* CPxScene_getFilterCallback(CPxScene* cpp)
	{
		return THIS->getFilterCallback();
	}

	bool CPxScene_resetFiltering(CPxScene* cpp, CPxActor* actor)
	{
		return THIS->resetFiltering(*GET(physx::PxActor, actor));
	}

	bool CPxScene_resetFiltering_shapes(CPxScene* cpp, CPxRigidActor* actor, CPxShape** shapes, CPxU32 shapeCount)
	{
		return THIS->resetFiltering(*GET(physx::PxRigidActor, actor), (physx::PxShape* const*)shapes, shapeCount);
	}

	CPxPairFilteringMode CPxScene_getKinematicKinematicFilteringMode(CPxScene* cpp)
	{
		return (CPxPairFilteringMode)THIS->getKinematicKinematicFilteringMode();
	}

	CPxPairFilteringMode CPxScene_getStaticKinematicFilteringMode(CPxScene* cpp)
	{
		return (CPxPairFilteringMode)THIS->getStaticKinematicFilteringMode();
	}

	bool CPxScene_simulate(CPxScene* cpp, CPxReal elapsedTime, CPxBaseTask* completionTask, void* scratchMemBlock, CPxU32 scratchMemBlockSize, bool controlSimulation)
	{
		return THIS->simulate(elapsedTime, GET(physx::PxBaseTask, completionTask), scratchMemBlock, scratchMemBlockSize, controlSimulation);
	}

	bool CPxScene_advance(CPxScene* cpp, CPxBaseTask* completionTask)
	{
		return THIS->advance(GET(physx::PxBaseTask, completionTask));
	}

	bool CPxScene_collide(CPxScene* cpp, CPxReal elapsedTime, CPxBaseTask* completionTask, void* scratchMemBlock, CPxU32 scratchMemBlockSize, bool controlSimulation)
	{
		return THIS->collide(elapsedTime, GET(physx::PxBaseTask, completionTask), scratchMemBlock, scratchMemBlockSize, controlSimulation);;
	}

	bool CPxScene_checkResults(CPxScene* cpp, bool block)
	{
		return THIS->checkResults(block);
	}

	bool CPxScene_fetchCollision(CPxScene* cpp, bool block)
	{
		return THIS->fetchCollision(block);
	}

	bool CPxScene_fetchResults(CPxScene* cpp, bool block, CPxU32* errorState)
	{
		return THIS->fetchResults(block, GET(physx::PxU32, errorState));
	}

	bool CPxScene_fetchResultsStart(CPxScene* cpp, CPxContactPairHeader** contactPairs, CPxU32* nbContactPairs, bool block)
	{
		return THIS->fetchResultsStart(*(const physx::PxContactPairHeader**)contactPairs, *nbContactPairs, block);
	}

	void CPxScene_processCallbacks(CPxScene* cpp, CPxBaseTask* continuation)
	{
		THIS->processCallbacks(GET(physx::PxBaseTask, continuation));
	}

	void CPxScene_fetchResultsFinish(CPxScene* cpp, CPxU32* errorState)
	{
		THIS->fetchResultsFinish(GET(physx::PxU32, errorState));
	}

	void CPxScene_fetchResultsParticleSystem(CPxScene* cpp)
	{
		THIS->fetchResultsParticleSystem();
	}

	void CPxScene_flushSimulation(CPxScene* cpp, bool sendPendingReports)
	{
		THIS->flushSimulation(sendPendingReports);
	}

	void CPxScene_setGravity(CPxScene* cpp, const CPxVec3 vec)
	{
		THIS->setGravity(getVec3Native(vec));
	}

	CPxVec3 CPxScene_getGravity(CPxScene* cpp)
	{
		return getVec3Aliased(THIS->getGravity());
	}

	void CPxScene_setBounceThresholdVelocity(CPxScene* cpp, const CPxReal t)
	{
		THIS->setBounceThresholdVelocity(t);
	}

	CPxReal CPxScene_getBounceThresholdVelocity(CPxScene* cpp)
	{
		return THIS->getBounceThresholdVelocity();
	}

	void CPxScene_setCCDMaxPasses(CPxScene* cpp, CPxU32 ccdMaxPasses)
	{
		THIS->setCCDMaxPasses(ccdMaxPasses);
	}

	CPxU32 CPxScene_getCCDMaxPasses(CPxScene* cpp)
	{
		return THIS->getCCDMaxPasses();
	}

	void CPxScene_setCCDMaxSeparation(CPxScene* cpp, const CPxReal t)
	{
		THIS->setCCDMaxSeparation(t);
	}

	CPxReal CPxScene_getCCDMaxSeparation(CPxScene* cpp)
	{
		return THIS->getCCDMaxSeparation();
	}

	void CPxScene_setCCDThreshold(CPxScene* cpp, const CPxReal t)
	{
		THIS->setCCDThreshold(t);
	}

	CPxReal CPxScene_getCCDThreshold(CPxScene* cpp)
	{
		return THIS->getCCDThreshold();
	}

	void CPxScene_setMaxBiasCoeffecient(CPxScene* cpp, const CPxReal t)
	{
		THIS->setMaxBiasCoefficient(t);
	}

	CPxReal CPxScene_getMaxBiasCoeffecient(CPxScene* cpp)
	{
		return THIS->getMaxBiasCoefficient();
	}

	void CPxScene_setFrictionOffsetThreshold(CPxScene* cpp, const CPxReal t)
	{
		THIS->setFrictionOffsetThreshold(t);
	}

	CPxReal CPxScene_getFrictionOffsetThreshold(CPxScene* cpp)
	{
		return THIS->getFrictionOffsetThreshold();
	}

	void CPxScene_setFrictionCorrelationDistance(CPxScene* cpp, const CPxReal t)
	{
		THIS->setFrictionCorrelationDistance(t);
	}

	CPxReal CPxScene_getFrictionCorrelationDistance(CPxScene* cpp)
	{
		return THIS->getFrictionCorrelationDistance();
	}

	CPxSolverType CPxScene_getSolverType(CPxScene* cpp)
	{
		return (CPxSolverType)THIS->getSolverType();
	}

	bool CPxScene_setVisualizationParameter(CPxScene* cpp, CPxVisualizationParameter param, CPxReal value)
	{
		return THIS->setVisualizationParameter(CHANGE(physx::PxVisualizationParameter::Enum, param), value);
	}

	CPxReal CPxScene_getVisualizationParameter(CPxScene* cpp, CPxVisualizationParameter paramEnum)
	{
		return THIS->getVisualizationParameter(CHANGE(physx::PxVisualizationParameter::Enum, paramEnum));
	}

	void CPxScene_setVisualizationCullingBox(CPxScene* cpp, const CPxBounds3 box)
	{
		THIS->setVisualizationCullingBox(getBounds3Native(box));
	}

	CPxBounds3 CPxScene_getVisualizationCullingBox(CPxScene* cpp)
	{
		return getBounds3Aliased(THIS->getVisualizationCullingBox());
	}

	CPxRenderBuffer CPxScene_getRenderBuffer(CPxScene* cpp)
	{
		return (CPxRenderBuffer)&THIS->getRenderBuffer();
	}

	void CPxScene_getSimulationStatistics(CPxScene* cpp, CPxSimulationStatistics* stats)
	{
		THIS->getSimulationStatistics(*(physx::PxSimulationStatistics*)stats);
	}

	CPxBroadPhaseType CPxScene_getBroadPhaseType(CPxScene* cpp)
	{
		return (CPxBroadPhaseType)THIS->getBroadPhaseType();
	}

	bool CPxScene_getBroadPhaseCaps(CPxScene* cpp, CPxBroadPhaseCaps* caps)
	{
		return THIS->getBroadPhaseCaps(*(physx::PxBroadPhaseCaps*)caps);
	}

	CPxU32 CPxScene_getNbBroadPhaseRegions(CPxScene* cpp)
	{
		return THIS->getNbBroadPhaseRegions();
	}

	CPxU32 CPxScene_getBroadPhaseRegions(CPxScene* cpp, CPxBroadPhaseRegionInfo** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getBroadPhaseRegions((physx::PxBroadPhaseRegionInfo*)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxScene_addBroadPhaseRegions(CPxScene* cpp, const CPxBroadPhaseRegion region, bool populateRegion)
	{
		return THIS->addBroadPhaseRegion(*(const physx::PxBroadPhaseRegion*)&region, populateRegion);
	}

	bool CPxScene_removeBroadPhaseRegion(CPxScene* cpp, CPxU32 handle)
	{
		return THIS->removeBroadPhaseRegion(handle);
	}

	CPxTaskManager* CPxScene_getTaskManager(CPxScene* cpp)
	{
		return THIS->getTaskManager();
	}

	void CPxScene_lockRead(CPxScene* cpp, const char* file, CPxU32 line)
	{
		THIS->lockRead(file, line);
	}

	void CPxScene_unlockRead(CPxScene* cpp)
	{
		THIS->unlockRead();
	}

	void CPxScene_lockWrite(CPxScene* cpp, const char* file, CPxU32 line)
	{
		THIS->lockWrite(file, line);
	}

	void CPxScene_unlockWrite(CPxScene* cpp)
	{
		THIS->unlockWrite();
	}

	void CPxScene_setNbContactDataBlocks(CPxScene* cpp, CPxU32 numBlocks)
	{
		THIS->setNbContactDataBlocks(numBlocks);
	}

	CPxU32 CPxScene_getNbContactDataBlocksUsed(CPxScene* cpp)
	{
		return THIS->getNbContactDataBlocksUsed();
	}

	CPxU32 CPxScene_getMaxNbContactDataBlocksUsed(CPxScene* cpp)
	{
		return THIS->getMaxNbContactDataBlocksUsed();
	}

	CPxU32 CPxScene_getContactReportStreamBufferSize(CPxScene* cpp)
	{
		return THIS->getContactReportStreamBufferSize();
	}

	void CPxScene_setSolverBatchSize(CPxScene* cpp, CPxU32 solverBatchSize)
	{
		THIS->setSolverBatchSize(solverBatchSize);
	}

	CPxU32 CPxScene_getSolverBatchSize(CPxScene* cpp)
	{
		return THIS->getSolverBatchSize();
	}

	void CPxScene_setSolverArticulationBatchSize(CPxScene* cpp, CPxU32 solverBatchSize)
	{
		THIS->setSolverArticulationBatchSize(solverBatchSize);
	}

	CPxU32 CPxScene_getSolverArticulationBatchSize(CPxScene* cpp)
	{
		return THIS->getSolverArticulationBatchSize();
	}

	CPxReal CPxScene_getWakeCounterResetValue(CPxScene* cpp)
	{
		return THIS->getWakeCounterResetValue();
	}

	void CPxScene_shiftOrigin(CPxScene* cpp, const CPxVec3 shift)
	{
		THIS->shiftOrigin(getVec3Native(shift));
	}

	CPxPvdSceneClient* CPxScene_getScenePvdClient(CPxScene* cpp)
	{
		return THIS->getScenePvdClient();
	}

	CPxGpuDynamicsMemoryConfig CPxScene_getGpuDynamicsConfig(CPxScene* cpp)
	{
		return getGpuDynamicsMemoryConfigAliased(THIS->getGpuDynamicsConfig());
	}

	CPxDirectGPUAPI* CPxScene_getDirectGPUAPI(CPxScene* cpp)
	{
		return &THIS->getDirectGPUAPI();
	}

	void CPxScene_setDeformableSurfaceGpuPostSolveCallback(CPxScene* cpp, CPxPostSolveCallback* postSolveCallback)
	{
		THIS->setDeformableSurfaceGpuPostSolveCallback(GET(physx::PxPostSolveCallback, postSolveCallback));
	}

	void CPxScene_setDeformableVolumeGpuPostSolveCallback(CPxScene* cpp, CPxPostSolveCallback* postSolveCallback)
	{
		THIS->setDeformableVolumeGpuPostSolveCallback(GET(physx::PxPostSolveCallback, postSolveCallback));
	}

	void CPxScene_userData_set(CPxScene* cpp, void* userData)
	{
		THIS->userData = userData;
	}

	void* CPxScene_userData_get(CPxScene* cpp)
	{
		return THIS->userData;
	}

	void** CPxScene_userData_ref(CPxScene* cpp)
	{
		return &THIS->userData;
	}
}
