using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxScene
    {
        /// <inheritdoc cref="PhysXInterop.PxScene_release()" />
        public void release()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setFlag(PxSceneFlagbool)" />
        public void setFlag(PxSceneFlag flag, bool value)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getFlags()" />
        public PxSceneFlag getFlags()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setLimits(PxSceneLimits)" />
        public void setLimits(PxSceneLimits limits)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setLimits(@this, limits);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getLimits()" />
        public PxSceneLimits getLimits()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getLimits(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getPhysics()" />
        public PxPhysics* getPhysics()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getPhysics(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getTimestamp()" />
        public uint getTimestamp()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getTimestamp(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setName(byte*)" />
        public void setName(byte* name)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setName(@this, name);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getName()" />
        public byte* getName()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_addArticulation(PxArticulationReducedCoordinate*)" />
        public bool addArticulation(PxArticulationReducedCoordinate* articulation)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_addArticulation(@this, articulation);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_removeArticulation(PxArticulationReducedCoordinate*bool)" />
        public void removeArticulation(PxArticulationReducedCoordinate* articulation, bool wakeOnLostTouch = true)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_removeArticulation(@this, articulation, wakeOnLostTouch);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_addActor(PxActor*PxBVH*)" />
        public bool addActor(PxActor* actor, PxBVH* bvh = null)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_addActor(@this, actor, bvh);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_addActors(PxActor**uint)" />
        public bool addActors(PxActor** actors, uint nbActors)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_addActors(@this, actors, nbActors);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_addActors_pruningStructure(PxPruningStructure*)" />
        public bool addActors(PxPruningStructure* pruningStructure)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_addActors_pruningStructure(@this, pruningStructure);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_removeActor(PxActor*bool)" />
        public void removeActor(PxActor* actor, bool wakeOnLostTouch = true)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_removeActor(@this, actor, wakeOnLostTouch);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_removeActors(PxActor**uintbool)" />
        public void removeActors(PxActor** actors, uint nbActors, bool wakeOnLostTouch = true)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_removeActors(@this, actors, nbActors, wakeOnLostTouch);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_addAggregate(PxAggregate*)" />
        public bool addAggregate(PxAggregate* aggregate)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_addAggregate(@this, aggregate);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_removeAggregate(PxAggregate*bool)" />
        public void removeAggregate(PxAggregate* aggregate, bool wakeOnLostTouch = true)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_removeAggregate(@this, aggregate, wakeOnLostTouch);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_addCollection(PxCollection*)" />
        public bool addCollection(PxCollection* collection)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_addCollection(@this, collection);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbActors(PxActorTypeFlag)" />
        public uint getNbActors(PxActorTypeFlag types)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbActors(@this, types);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getActors(PxActorTypeFlagPxActor**uintuint)" />
        public uint getActors(PxActorTypeFlag types, PxActor** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getActors(@this, types, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getActiveActors(uint*)" />
        public PxActor** getActiveActors(uint* nbActorsOut)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getActiveActors(@this, nbActorsOut);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbDeformableSurfaces()" />
        public uint getNbDeformableSurfaces()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbDeformableSurfaces(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getDeformableSurfaces(PxDeformableSurface**uintuint)" />
        public uint getDeformableSurfaces(PxDeformableSurface** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getDeformableSurfaces(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbDeformableVolumes()" />
        public uint getNbDeformableVolumes()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbDeformableVolumes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getDeformableVolumes(PxDeformableVolume**uintuint)" />
        public uint getDeformableVolumes(PxDeformableVolume** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getDeformableVolumes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbPBDParticleSystems()" />
        public uint getNbPBDParticleSystems()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbPBDParticleSystems(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getPDBParticleSystems(PxPBDParticleSystem**uintuint)" />
        public uint getPDBParticleSystems(PxPBDParticleSystem** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getPDBParticleSystems(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbArticulations()" />
        public uint getNbArticulations()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbArticulations(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getArticulations(PxArticulationReducedCoordinate**uintuint)" />
        public uint getArticulations(PxArticulationReducedCoordinate** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getArticulations(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbConstraints()" />
        public uint getNbConstraints()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbConstraints(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getConstraints(PxConstraint**uintuint)" />
        public uint getConstraints(PxConstraint** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getConstraints(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbAggregates()" />
        public uint getNbAggregates()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbAggregates(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getAggregates(PxAggregate**uintuint)" />
        public uint getAggregates(PxAggregate** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getAggregates(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setDominanceGroupPair(PxDominanceGroupPxDominanceGroupPxDominanceGroupPair)" />
        public void setDominanceGroupPair(PxDominanceGroup group1, PxDominanceGroup group2, PxDominanceGroupPair dominance)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setDominanceGroupPair(@this, group1, group2, dominance);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getDominanceGroupPair(PxDominanceGroupPxDominanceGroup)" />
        public PxDominanceGroupPair getDominanceGroupPair(PxDominanceGroup group1, PxDominanceGroup group2)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getDominanceGroupPair(@this, group1, group2);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getCpuDispatcher()" />
        public PxCpuDispatcher* getCpuDispatcher()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getCpuDispatcher(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getCudaContextManager()" />
        public PxCudaContextManager* getCudaContextManager()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getCudaContextManager(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_createClient()" />
        public PxClientID createClient()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_createClient(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setSimulationEventCallback(PxSimulationEventCallback*)" />
        public void setSimulationEventCallback(PxSimulationEventCallback* callback)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setSimulationEventCallback(@this, callback);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getSimulationEventCallback()" />
        public PxSimulationEventCallback* getSimulationEventCallback()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getSimulationEventCallback(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setContactModifyCallback(PxContactModifyCallback*)" />
        public void setContactModifyCallback(PxContactModifyCallback* callback)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setContactModifyCallback(@this, callback);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setCCDContactModifyCallback(PxCCDContactModifyCallback*)" />
        public void setCCDContactModifyCallback(PxCCDContactModifyCallback* callback)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setCCDContactModifyCallback(@this, callback);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getContactModifyCallback()" />
        public PxContactModifyCallback* getContactModifyCallback()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getContactModifyCallback(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getCCDContactModifyCallback()" />
        public PxCCDContactModifyCallback* getCCDContactModifyCallback()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getCCDContactModifyCallback(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setBroadPhaseCallback(PxBroadPhaseCallback*)" />
        public void setBroadPhaseCallback(PxBroadPhaseCallback* callback)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setBroadPhaseCallback(@this, callback);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getBroadPhaseCallback()" />
        public PxBroadPhaseCallback* getBroadPhaseCallback()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getBroadPhaseCallback(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setFilterShaderData(void*uint)" />
        public void setFilterShaderData(void* data, uint dataSize)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setFilterShaderData(@this, data, dataSize);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getFilterShaderData()" />
        public void* getFilterShaderData()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getFilterShaderData(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getFilterShaderDataSize()" />
        public uint getFilterShaderDataSize()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getFilterShaderDataSize(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getFilterShader()" />
        public PxSimulationFilterShader* getFilterShader()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getFilterShader(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getFilterCallback()" />
        public PxSimulationFilterCallback* getFilterCallback()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getFilterCallback(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_resetFiltering(PxActor*)" />
        public bool resetFiltering(PxActor* actor)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_resetFiltering(@this, actor);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_resetFiltering_shapes(PxRigidActor*PxShape**uint)" />
        public bool resetFiltering(PxRigidActor* actor, PxShape** shapes, uint shapeCount)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_resetFiltering_shapes(@this, actor, shapes, shapeCount);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getKinematicKinematicFilteringMode()" />
        public PxPairFilteringMode getKinematicKinematicFilteringMode()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getKinematicKinematicFilteringMode(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getStaticKinematicFilteringMode()" />
        public PxPairFilteringMode getStaticKinematicFilteringMode()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getStaticKinematicFilteringMode(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_simulate(floatPxBaseTask*void*uintbool)" />
        public bool simulate(float elapsedTime, PxBaseTask* completionTask = null, void* scratchMemBlock = null, uint scratchMemBlockSize = 0, bool controlSimulation = true)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_simulate(@this, elapsedTime, completionTask, scratchMemBlock, scratchMemBlockSize, controlSimulation);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_advance(PxBaseTask*)" />
        public bool advance(PxBaseTask* completionTask = null)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_advance(@this, completionTask);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_collide(floatPxBaseTask*void*uintbool)" />
        public bool collide(float elapsedTime, PxBaseTask* completionTask = null, void* scratchMemBlock = null, uint scratchMemBlockSize = 0, bool controlSimulation = true)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_collide(@this, elapsedTime, completionTask, scratchMemBlock, scratchMemBlockSize, controlSimulation);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_checkResults(bool)" />
        public bool checkResults(bool block = false)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_checkResults(@this, block);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_fetchCollision(bool)" />
        public bool fetchCollision(bool block = false)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_fetchCollision(@this, block);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_fetchResults(booluint*)" />
        public bool fetchResults(bool block = false, uint* errorState = null)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_fetchResults(@this, block, errorState);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_fetchResultsStart(PxContactPairHeader**uint*bool)" />
        public bool fetchResultsStart(PxContactPairHeader** contactPairs, uint* nbContactPairs, bool block = false)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_fetchResultsStart(@this, contactPairs, nbContactPairs, block);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_processCallbacks(PxBaseTask*)" />
        public void processCallbacks(PxBaseTask* continuation)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_processCallbacks(@this, continuation);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_fetchResultsFinish(uint*)" />
        public void fetchResultsFinish(uint* errorState = null)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_fetchResultsFinish(@this, errorState);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_fetchResultsParticleSystem()" />
        public void fetchResultsParticleSystem()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_fetchResultsParticleSystem(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_flushSimulation(bool)" />
        public void flushSimulation(bool sendPendingReports = false)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_flushSimulation(@this, sendPendingReports);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setGravity(PxVec3)" />
        public void setGravity(PxVec3 vec)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setGravity(@this, vec);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getGravity()" />
        public PxVec3 getGravity()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getGravity(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setBounceThresholdVelocity(float)" />
        public void setBounceThresholdVelocity(float t)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setBounceThresholdVelocity(@this, t);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getBounceThresholdVelocity()" />
        public float getBounceThresholdVelocity()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getBounceThresholdVelocity(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setCCDMaxPasses(uint)" />
        public void setCCDMaxPasses(uint ccdMaxPasses)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setCCDMaxPasses(@this, ccdMaxPasses);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getCCDMaxPasses()" />
        public uint getCCDMaxPasses()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getCCDMaxPasses(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setCCDMaxSeparation(float)" />
        public void setCCDMaxSeparation(float t)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setCCDMaxSeparation(@this, t);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getCCDMaxSeparation()" />
        public float getCCDMaxSeparation()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getCCDMaxSeparation(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setCCDThreshold(float)" />
        public void setCCDThreshold(float t)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setCCDThreshold(@this, t);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getCCDThreshold()" />
        public float getCCDThreshold()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getCCDThreshold(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setMaxBiasCoeffecient(float)" />
        public void setMaxBiasCoeffecient(float t)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setMaxBiasCoeffecient(@this, t);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getMaxBiasCoeffecient()" />
        public float getMaxBiasCoeffecient()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getMaxBiasCoeffecient(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setFrictionOffsetThreshold(float)" />
        public void setFrictionOffsetThreshold(float t)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setFrictionOffsetThreshold(@this, t);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getFrictionOffsetThreshold()" />
        public float getFrictionOffsetThreshold()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getFrictionOffsetThreshold(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setFrictionCorrelationDistance(float)" />
        public void setFrictionCorrelationDistance(float t)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setFrictionCorrelationDistance(@this, t);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getFrictionCorrelationDistance()" />
        public float getFrictionCorrelationDistance()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getFrictionCorrelationDistance(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getSolverType()" />
        public PxSolverType getSolverType()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getSolverType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setVisualizationParameter(PxVisualizationParameterfloat)" />
        public bool setVisualizationParameter(PxVisualizationParameter param, float value)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_setVisualizationParameter(@this, param, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getVisualizationParameter(PxVisualizationParameter)" />
        public float getVisualizationParameter(PxVisualizationParameter paramEnum)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getVisualizationParameter(@this, paramEnum);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setVisualizationCullingBox(PxBounds3)" />
        public void setVisualizationCullingBox(PxBounds3 box)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setVisualizationCullingBox(@this, box);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getVisualizationCullingBox()" />
        public PxBounds3 getVisualizationCullingBox()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getVisualizationCullingBox(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getRenderBuffer()" />
        public PxRenderBuffer getRenderBuffer()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getRenderBuffer(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getSimulationStatistics(PxSimulationStatistics*)" />
        public void getSimulationStatistics(PxSimulationStatistics* stats)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_getSimulationStatistics(@this, stats);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getBroadPhaseType()" />
        public PxBroadPhaseType getBroadPhaseType()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getBroadPhaseType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getBroadPhaseCaps(PxBroadPhaseCaps*)" />
        public bool getBroadPhaseCaps(PxBroadPhaseCaps* caps)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getBroadPhaseCaps(@this, caps);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbBroadPhaseRegions()" />
        public uint getNbBroadPhaseRegions()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbBroadPhaseRegions(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getBroadPhaseRegions(PxBroadPhaseRegionInfo**uintuint)" />
        public uint getBroadPhaseRegions(PxBroadPhaseRegionInfo** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getBroadPhaseRegions(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_addBroadPhaseRegions(PxBroadPhaseRegionbool)" />
        public uint addBroadPhaseRegions(PxBroadPhaseRegion region, bool populateRegion = false)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_addBroadPhaseRegions(@this, region, populateRegion);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_removeBroadPhaseRegion(uint)" />
        public bool removeBroadPhaseRegion(uint handle)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_removeBroadPhaseRegion(@this, handle);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getTaskManager()" />
        public PxTaskManager* getTaskManager()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getTaskManager(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_lockRead(byte*uint)" />
        public void lockRead(byte* file = null, uint line = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_lockRead(@this, file, line);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_unlockRead()" />
        public void unlockRead()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_unlockRead(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_lockWrite(byte*uint)" />
        public void lockWrite(byte* file = null, uint line = 0)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_lockWrite(@this, file, line);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_unlockWrite()" />
        public void unlockWrite()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_unlockWrite(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setNbContactDataBlocks(uint)" />
        public void setNbContactDataBlocks(uint numBlocks)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setNbContactDataBlocks(@this, numBlocks);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getNbContactDataBlocksUsed()" />
        public uint getNbContactDataBlocksUsed()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getNbContactDataBlocksUsed(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getMaxNbContactDataBlocksUsed()" />
        public uint getMaxNbContactDataBlocksUsed()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getMaxNbContactDataBlocksUsed(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getContactReportStreamBufferSize()" />
        public uint getContactReportStreamBufferSize()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getContactReportStreamBufferSize(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setSolverBatchSize(uint)" />
        public void setSolverBatchSize(uint solverBatchSize)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setSolverBatchSize(@this, solverBatchSize);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getSolverBatchSize()" />
        public uint getSolverBatchSize()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getSolverBatchSize(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setSolverArticulationBatchSize(uint)" />
        public void setSolverArticulationBatchSize(uint solverBatchSize)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setSolverArticulationBatchSize(@this, solverBatchSize);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getSolverArticulationBatchSize()" />
        public uint getSolverArticulationBatchSize()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getSolverArticulationBatchSize(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getWakeCounterResetValue()" />
        public float getWakeCounterResetValue()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getWakeCounterResetValue(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_shiftOrigin(PxVec3)" />
        public void shiftOrigin(PxVec3 shift)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_shiftOrigin(@this, shift);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getScenePvdClient()" />
        public PxPvdSceneClient* getScenePvdClient()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getScenePvdClient(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getGpuDynamicsConfig()" />
        public PxGpuDynamicsMemoryConfig getGpuDynamicsConfig()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getGpuDynamicsConfig(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_getDirectGPUAPI()" />
        public PxDirectGPUAPI* getDirectGPUAPI()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_getDirectGPUAPI(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setDeformableSurfaceGpuPostSolveCallback(PxPostSolveCallback*)" />
        public void setDeformableSurfaceGpuPostSolveCallback(PxPostSolveCallback* postSolveCallback)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setDeformableSurfaceGpuPostSolveCallback(@this, postSolveCallback);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_setDeformableVolumeGpuPostSolveCallback(PxPostSolveCallback*)" />
        public void setDeformableVolumeGpuPostSolveCallback(PxPostSolveCallback* postSolveCallback)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_setDeformableVolumeGpuPostSolveCallback(@this, postSolveCallback);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_userData_set(void*)" />
        public void userData_set(void* userData)
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            PhysXInterop.PxScene_userData_set(@this, userData);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_userData_get()" />
        public void* userData_get()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_userData_get(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxScene_userData_ref()" />
        public void** userData_ref()
        {
            PxScene* @this = (PxScene*)Unsafe.AsPointer(ref Unsafe.AsRef<PxScene>(ref this));
            return PhysXInterop.PxScene_userData_ref(@this);
        }
    }
}
