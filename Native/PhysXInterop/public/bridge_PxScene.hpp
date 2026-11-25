#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Deletes the scene.

	Removes any actors and constraint shaders from this scene
	(if the user hasn't already done so).

	Be sure	to not keep a reference to this object after calling release.
	Avoid release calls while the scene is simulating (in between simulate() and fetchResults() calls).

	\see PxPhysics.createScene()
	*/
	INTEROP_API void CPxScene_release(CPxScene* cpp);

	/**
	\brief Sets a scene flag. You can only set one flag at a time.

	\note Not all flags are mutable and changing some will result in an error. Please check #PxSceneFlag to see which flags can be changed.

	\see PxSceneFlag
	*/
	INTEROP_API void CPxScene_setFlag(CPxScene* cpp, CPxSceneFlag flag, bool value);

	/**
	\brief Get the scene flags.

	\return The scene flags. See #PxSceneFlag

	\see PxSceneFlag
	*/
	INTEROP_API CPxSceneFlag CPxScene_getFlags(CPxScene* cpp);

	/**
	\brief Set new scene limits.

	\note Increase the maximum capacity of various data structures in the scene. The new capacities will be
	at least as large as required to deal with the objects currently in the scene. Further, these values
	are for preallocation and do not represent hard limits.

	\param[in] limits Scene limits.
	\see PxSceneLimits
	*/
	INTEROP_API void CPxScene_setLimits(CPxScene* cpp, CPxSceneLimits limits);

	/**
	\brief Get current scene limits.
	\return Current scene limits.
	\see PxSceneLimits
	*/
	INTEROP_API CPxSceneLimits CPxScene_getLimits(CPxScene* cpp);

	/**
	\brief Call this method to retrieve the Physics SDK.

	\return The physics SDK this scene is associated with.

	\see PxPhysics
	*/
	INTEROP_API CPxPhysics* CPxScene_getPhysics(CPxScene* cpp);

	/**
	\brief Retrieves the scene's internal timestamp, increased each time a simulation step is completed.

	\return scene timestamp
	*/
	INTEROP_API CPxU32 CPxScene_getTimestamp(CPxScene* cpp);

	/**
	\brief Sets a name string for the Scene that can be retrieved with getName().

	This is for debugging and is not used by the SDK. The string is not copied by the SDK,
	only the pointer is stored.

	\param[in] name String to set the objects name to.

	<b>Default:</b> NULL

	\see getName()
	*/
	INTEROP_API void CPxScene_setName(CPxScene* cpp, const char* name);

	/**
	\brief Retrieves the name string set with setName().

	\return Name string associated with the Scene.

	\see setName()
	*/
	INTEROP_API const char* CPxScene_getName(CPxScene* cpp);

	/**
	\brief Adds an articulation to this scene.

	\note If the articulation is already assigned to a scene (see #PxArticulationReducedCoordinate::getScene), the call is ignored and an error is issued.

	\param[in] articulation The articulation to add to the scene.
	\return True if success

	\see PxArticulationReducedCoordinate
	*/
	INTEROP_API bool CPxScene_addArticulation(CPxScene* cpp, CPxArticulationReducedCoordinate* articulation);

	/**
	\brief Removes an articulation from this scene.

	\note If the articulation is not part of this scene (see #PxArticulationReducedCoordinate::getScene), the call is ignored and an error is issued.

	\note If the articulation is in an aggregate it will be removed from the aggregate.

	\param[in] articulation The articulation to remove from the scene.
	\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame.
	Only applies to PxArticulationReducedCoordinate and PxRigidActor types.

	\see PxArticulationReducedCoordinate, PxAggregate
	*/
	INTEROP_API void CPxScene_removeArticulation(CPxScene* cpp, CPxArticulationReducedCoordinate* articulation, bool wakeOnLostTouch = true);

	/**
	\brief Adds an actor to this scene.

	\note If the actor is already assigned to a scene (see #PxActor::getScene), the call is ignored and an error is issued.
	\note If the actor has an invalid constraint, in checked builds the call is ignored and an error is issued.

	\note You can not add individual articulation links (see #PxArticulationLink) to the scene. Use #addArticulation() instead.

	\note If the actor is a PxRigidActor then each assigned PxConstraint object will get added to the scene automatically if
	it connects to another actor that is part of the scene already.

	\note When a BVH is provided the actor shapes are grouped together.
	The scene query pruning structure inside PhysX SDK will store/update one
	bound per actor. The scene queries against such an actor will query actor
	bounds and then make a local space query against the provided BVH, which is in actor's local space.

	\param[in] actor	Actor to add to scene.
	\param[in] bvh		BVH for actor shapes.
	\return True if success

	\see PxActor, PxConstraint::isValid(), PxBVH
	*/
	INTEROP_API bool CPxScene_addActor(CPxScene* cpp, CPxActor* actor, CPxBVH* bvh = nullptr);

	/**
	\brief Adds actors to this scene. Only supports actors of type PxRigidStatic and PxRigidDynamic.

	\note This method only supports actors of type PxRigidStatic and PxRigidDynamic. For other actors, use addActor() instead.
	For articulation links, use addArticulation().

	\note If one of the actors is already assigned to a scene (see #PxActor::getScene), the call is ignored and an error is issued.

	\note If an actor in the array contains an invalid constraint, in checked builds the call is ignored and an error is issued.
	\note If an actor in the array is a PxRigidActor then each assigned PxConstraint object will get added to the scene automatically if
	it connects to another actor that is part of the scene already.

	\note this method is optimized for high performance.

	\param[in] actors Array of actors to add to scene.
	\param[in] nbActors Number of actors in the array.
	\return True if success

	\see PxActor, PxConstraint::isValid()
	*/
	INTEROP_API bool CPxScene_addActors(CPxScene* cpp, CPxActor** actors, CPxU32 nbActors);

	/**
	\brief Adds a pruning structure together with its actors to this scene. Only supports actors of type PxRigidStatic and PxRigidDynamic.

	\note This method only supports actors of type PxRigidStatic and PxRigidDynamic. For other actors, use addActor() instead.
	For articulation links, use addArticulation().

	\note If an actor in the pruning structure contains an invalid constraint, in checked builds the call is ignored and an error is issued.
	\note For all actors in the pruning structure each assigned PxConstraint object will get added to the scene automatically if
	it connects to another actor that is part of the scene already.

	\note This method is optimized for high performance.

	\note Merging a PxPruningStructure into an active scene query optimization AABB tree might unbalance the tree. A typical use case for
	PxPruningStructure is a large world scenario where blocks of closely positioned actors get streamed in. The merge process finds the
	best node in the active scene query optimization AABB tree and inserts the PxPruningStructure. Therefore using PxPruningStructure
	for actors scattered throughout the world will result in an unbalanced tree.

	\param[in] pruningStructure Pruning structure for a set of actors.
	\return True if success

	\see PxPhysics::createPruningStructure, PxPruningStructure
	*/
	INTEROP_API bool CPxScene_addActors_pruningStructure(CPxScene* cpp, CPxPruningStructure* pruningStructure);

	/**
	\brief Removes an actor from this scene.

	\note If the actor is not part of this scene (see #PxActor::getScene), the call is ignored and an error is issued.

	\note You can not remove individual articulation links (see #PxArticulationLink) from the scene. Use #removeArticulation() instead.

	\note If the actor is a PxRigidActor then all assigned PxConstraint objects will get removed from the scene automatically.

	\note If the actor is in an aggregate it will be removed from the aggregate.

	\param[in] actor Actor to remove from scene.
	\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame. Only applies to PxArticulationReducedCoordinate and PxRigidActor types.

	\see PxActor, PxAggregate
	*/
	INTEROP_API void CPxScene_removeActor(CPxScene* cpp, CPxActor* actor, bool wakeOnLostTouch = true);

	/**
	\brief Removes actors from this scene. Only supports actors of type PxRigidStatic and PxRigidDynamic.

	\note This method only supports actors of type PxRigidStatic and PxRigidDynamic. For other actors, use removeActor() instead.
	For articulation links, use removeArticulation().

	\note If some actor is not part of this scene (see #PxActor::getScene), the actor remove is ignored and an error is issued.

	\note You can not remove individual articulation links (see #PxArticulationLink) from the scene. Use #removeArticulation() instead.

	\note If the actor is a PxRigidActor then all assigned PxConstraint objects will get removed from the scene automatically.

	\param[in] actors Array of actors to add to scene.
	\param[in] nbActors Number of actors in the array.
	\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame. Only applies to PxArticulationReducedCooridnate and PxRigidActor types.

	\see PxActor
	*/
	INTEROP_API void CPxScene_removeActors(CPxScene* cpp, CPxActor** actors, CPxU32 nbActors, bool wakeOnLostTouch = true);

	/**
	\brief Adds an aggregate to this scene.

	\note If the aggregate is already assigned to a scene (see #PxAggregate::getScene), the call is ignored and an error is issued.
	\note If the aggregate contains an actor with an invalid constraint, in checked builds the call is ignored and an error is issued.

	\note If the aggregate already contains actors, those actors are added to the scene as well.

	\param[in] aggregate Aggregate to add to scene.
	\return True if success

	\see PxAggregate, PxConstraint::isValid()
	*/
	INTEROP_API bool CPxScene_addAggregate(CPxScene* cpp, CPxAggregate* aggregate);

	/**
	\brief Removes an aggregate from this scene.

	\note If the aggregate is not part of this scene (see #PxAggregate::getScene), the call is ignored and an error is issued.

	\note If the aggregate contains actors, those actors are removed from the scene as well.

	\param[in] aggregate Aggregate to remove from scene.
	\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame. Only applies to PxArticulationReducedCoordinate and PxRigidActor types.

	\see PxAggregate
	*/
	INTEROP_API void CPxScene_removeAggregate(CPxScene* cpp, CPxAggregate* aggregate, bool wakeOnLostTouch = true);

	/**
	\brief Adds objects in the collection to this scene.

	This function adds the following types of objects to this scene: PxRigidActor (except PxArticulationLink), PxAggregate, PxArticulationReducedCoordinate.
	This method is typically used after deserializing the collection in order to populate the scene with deserialized objects.

	\note If the collection contains an actor with an invalid constraint, in checked builds the call is ignored and an error is issued.

	\param[in] collection Objects to add to this scene. See #PxCollection
	\return True if success

	\see PxCollection, PxConstraint::isValid()
	*/
	INTEROP_API bool CPxScene_addCollection(CPxScene* cpp, CPxCollection* collection);

	/**
	\brief Retrieve the number of actors of certain types in the scene. For supported types, see PxActorTypeFlags.

	\param[in] types Combination of actor types.
	\return the number of actors.

	\see getActors()
	*/
	INTEROP_API CPxU32 CPxScene_getNbActors(CPxScene* cpp, CPxActorTypeFlag types);

	/**
	\brief Retrieve an array of all the actors of certain types in the scene. For supported types, see PxActorTypeFlags.

	\param[in] types Combination of actor types to retrieve.
	\param[out] userBuffer The buffer to receive actor pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first actor pointer to be retrieved
	\return Number of actors written to the buffer.

	\see getNbActors()
	*/
	INTEROP_API CPxU32 CPxScene_getActors(CPxScene* cpp, CPxActorTypeFlag types, CPxActor** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Queries the PxScene for a list of the PxActors whose transforms have been
	updated during the previous simulation step. Only includes actors of type PxRigidDynamic and PxArticulationLink.

	\note PxSceneFlag::eENABLE_ACTIVE_ACTORS must be set.

	\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored and NULL will be returned.

	\note This list may contain actors that have been released after fetchResults() of the previous simulation step. It is the user's
		  responsibility to track such actors and avoid dereferencing the corresponding pointers.

	\param[out] nbActorsOut The number of actors returned.

	\return A pointer to the list of active PxActors generated during the last call to fetchResults().

	\see PxActor
	*/
	INTEROP_API CPxActor** CPxScene_getActiveActors(CPxScene* cpp, CPxU32* nbActorsOut);

	/**
	\brief Retrieve the number of deformable surfaces in the scene.

	\return the number of deformable surfaces.

	See getDeformableSurfaces()
	*/
	INTEROP_API CPxU32 CPxScene_getNbDeformableSurfaces(CPxScene* cpp);

	/**
	\brief Retrieve an array of all the deformable surfaces in the scene.

	\param[out] userBuffer The buffer to write the deformable surface pointers to
	\param[in] bufferSize Size of the provided user buffer
	\param[in] startIndex Index of first deformable surface pointer to be retrieved
	\return Number of deformable surfaces written to the buffer
	*/
	INTEROP_API CPxU32 CPxScene_getDeformableSurfaces(CPxScene* cpp, CPxDeformableSurface** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Retrieve the number of deformable volumes in the scene.

	\return the number of deformable volumes.

	\see getActors()
	*/
	INTEROP_API CPxU32 CPxScene_getNbDeformableVolumes(CPxScene* cpp);

	/**
	\brief Retrieve an array of all the deformable volumes in the scene.

	\param[out] userBuffer The buffer to receive actor pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first actor pointer to be retrieved
	\return Number of actors written to the buffer.

	\see getNbActors()
	*/
	INTEROP_API CPxU32 CPxScene_getDeformableVolumes(CPxScene* cpp, CPxDeformableVolume** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Retrieve the number of particle systems of the requested type in the scene.

	\return the number particle systems.

	\see getPBDParticleSystems()
	*/
	INTEROP_API CPxU32 CPxScene_getNbPBDParticleSystems(CPxScene* cpp);

	/**
	\brief Retrieve an array of all the particle systems of the requested type in the scene.

	\param[out] userBuffer The buffer to receive particle system pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first particle system pointer to be retrieved
	\return Number of particle systems written to the buffer.

	\see getNbPBDParticleSystems()
	*/
	INTEROP_API CPxU32 CPxScene_getPDBParticleSystems(CPxScene* cpp, CPxPBDParticleSystem** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Returns the number of articulations in the scene.

	\return the number of articulations in this scene.

	\see getArticulations()
	*/
	INTEROP_API CPxU32 CPxScene_getNbArticulations(CPxScene* cpp);

	/**
	\brief Retrieve all the articulations in the scene.

	\param[out] userBuffer The buffer to receive articulations pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first articulations pointer to be retrieved
	\return Number of articulations written to the buffer.

	\see getNbArticulations()
	*/
	INTEROP_API CPxU32 CPxScene_getArticulations(CPxScene* cpp, CPxArticulationReducedCoordinate** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Returns the number of constraint shaders in the scene.

	\return the number of constraint shaders in this scene.

	\see getConstraints()
	*/
	INTEROP_API CPxU32 CPxScene_getNbConstraints(CPxScene* cpp);

	/**
	\brief Retrieve all the constraint shaders in the scene.

	\param[out] userBuffer The buffer to receive constraint shader pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first constraint pointer to be retrieved
	\return Number of constraint shaders written to the buffer.

	\see getNbConstraints()
	*/
	INTEROP_API CPxU32 CPxScene_getConstraints(CPxScene* cpp, CPxConstraint** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Returns the number of aggregates in the scene.

	\return the number of aggregates in this scene.

	\see getAggregates()
	*/
	INTEROP_API CPxU32 CPxScene_getNbAggregates(CPxScene* cpp);

	/**
	\brief Retrieve all the aggregates in the scene.

	\param[out] userBuffer The buffer to receive aggregates pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first aggregate pointer to be retrieved
	\return Number of aggregates written to the buffer.

	\see getNbAggregates()
	*/
	INTEROP_API CPxU32 CPxScene_getAggregates(CPxScene* cpp, CPxAggregate** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Specifies the dominance behavior of contacts between two actors with two certain dominance groups.

	It is possible to assign each actor to a dominance groups using #PxActor::setDominanceGroup().

	With dominance groups one can have all contacts created between actors act in one direction only. This is useful, for example, if you
	want an object to push debris out of its way and be unaffected,while still responding physically to forces and collisions
	with non-debris objects.

	Whenever a contact between two actors (a0, a1) needs to be solved, the groups (g0, g1) of both
	actors are retrieved. Then the PxDominanceGroupPair setting for this group pair is retrieved with getDominanceGroupPair(g0, g1).

	In the contact, PxDominanceGroupPair::dominance0 becomes the dominance setting for a0, and
	PxDominanceGroupPair::dominance1 becomes the dominance setting for a1. A dominanceN setting of 1.0f, the default,
	will permit aN to be pushed or pulled by a(1-N) through the contact. A dominanceN setting of 0.0f, will however
	prevent aN to be pushed by a(1-N) via the contact. Thus, a PxDominanceGroupPair of (1.0f, 0.0f) makes
	the interaction one-way.


	The matrix sampled by getDominanceGroupPair(g1, g2) is initialised by default such that:

	if g1 == g2, then (1.0f, 1.0f) is returned
	if g1 <  g2, then (0.0f, 1.0f) is returned
	if g1 >  g2, then (1.0f, 0.0f) is returned

	In other words, we permit actors in higher groups to be pushed around by actors in lower groups by default.

	These settings should cover most applications, and in fact not overriding these settings may likely result in higher performance.

	It is not possible to make the matrix asymetric, or to change the diagonal. In other words:

	* it is not possible to change (g1, g2) if (g1==g2)
	* if you set

	(g1, g2) to X, then (g2, g1) will implicitly and automatically be set to ~X, where:

	~(1.0f, 1.0f) is (1.0f, 1.0f)
	~(0.0f, 1.0f) is (1.0f, 0.0f)
	~(1.0f, 0.0f) is (0.0f, 1.0f)

	These two restrictions are to make sure that contacts between two actors will always evaluate to the same dominance
	setting, regardless of the order of the actors.

	Dominance settings are currently specified as floats 0.0f or 1.0f because in the future we may permit arbitrary
	fractional settings to express 'partly-one-way' interactions.

	<b>Sleeping:</b> Does <b>NOT</b> wake actors up automatically.

	\see getDominanceGroupPair() PxDominanceGroup PxDominanceGroupPair PxActor::setDominanceGroup() PxActor::getDominanceGroup()
	*/
	INTEROP_API void CPxScene_setDominanceGroupPair(CPxScene* cpp, CPxDominanceGroup group1, CPxDominanceGroup group2, CPxDominanceGroupPair dominance);

	/**
	\brief Samples the dominance matrix.

	\see setDominanceGroupPair() PxDominanceGroup PxDominanceGroupPair PxActor::setDominanceGroup() PxActor::getDominanceGroup()
	*/
	INTEROP_API CPxDominanceGroupPair CPxScene_getDominanceGroupPair(CPxScene* cpp, CPxDominanceGroup group1, CPxDominanceGroup group2);

	/**
	\brief Return the cpu dispatcher that was set in PxSceneDesc::cpuDispatcher when creating the scene with PxPhysics::createScene

	\see PxSceneDesc::cpuDispatcher, PxPhysics::createScene
	*/
	INTEROP_API CPxCpuDispatcher* CPxScene_getCpuDispatcher(CPxScene* cpp);

	/**
	\brief Return the CUDA context manager that was set in PxSceneDesc::cudaContextManager when creating the scene with PxPhysics::createScene

	<b>Platform specific:</b> Applies to PC GPU only.

	\see PxSceneDesc::cudaContextManager, PxPhysics::createScene
	*/
	INTEROP_API CPxCudaContextManager* CPxScene_getCudaContextManager(CPxScene* cpp);

	/**
	\brief Reserves a new client ID.

	PX_DEFAULT_CLIENT is always available as the default clientID.
	Additional clients are returned by this function. Clients cannot be released once created.
	An error is reported when more than a supported number of clients (currently 128) are created.

	\see PxClientID
	*/
	INTEROP_API CPxClientID CPxScene_createClient(CPxScene* cpp);

	/**
	\brief Sets a user notify object which receives special simulation events when they occur.

	\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.

	\param[in] callback User notification callback. See #PxSimulationEventCallback.

	\see PxSimulationEventCallback getSimulationEventCallback
	*/
	INTEROP_API void CPxScene_setSimulationEventCallback(CPxScene* cpp, CPxSimulationEventCallback* callback);

	/**
	\brief Retrieves the simulationEventCallback pointer set with setSimulationEventCallback().

	\return The current user notify pointer. See #PxSimulationEventCallback.

	\see PxSimulationEventCallback setSimulationEventCallback()
	*/
	INTEROP_API CPxSimulationEventCallback* CPxScene_getSimulationEventCallback(CPxScene* cpp);

	/**
	\brief Sets a user callback object, which receives callbacks on all contacts generated for specified actors.

	\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.

	\param[in] callback Asynchronous user contact modification callback. See #PxContactModifyCallback.
	*/
	INTEROP_API void CPxScene_setContactModifyCallback(CPxScene* cpp, CPxContactModifyCallback* callback);

	/**
	\brief Sets a user callback object, which receives callbacks on all CCD contacts generated for specified actors.

	\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.

	\param[in] callback Asynchronous user contact modification callback. See #PxCCDContactModifyCallback.
	*/
	INTEROP_API void CPxScene_setCCDContactModifyCallback(CPxScene* cpp, CPxCCDContactModifyCallback* callback);

	/**
	\brief Retrieves the PxContactModifyCallback pointer set with setContactModifyCallback().

	\return The current user contact modify callback pointer. See #PxContactModifyCallback.

	\see PxContactModifyCallback setContactModifyCallback()
	*/
	INTEROP_API CPxContactModifyCallback* CPxScene_getContactModifyCallback(CPxScene* cpp);

	/**
	\brief Retrieves the PxContactModifyCallback pointer set with setContactModifyCallback().
	
	\return The current user contact modify callback pointer. See #PxContactModifyCallback.
	
	\see PxContactModifyCallback setContactModifyCallback()
	*/
	INTEROP_API CPxCCDContactModifyCallback* CPxScene_getCCDContactModifyCallback(CPxScene* cpp);

	/**
	\brief Sets a broad-phase user callback object.

	\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.

	\param[in] callback	Asynchronous broad-phase callback. See #PxBroadPhaseCallback.
	*/
	INTEROP_API void CPxScene_setBroadPhaseCallback(CPxScene* cpp, CPxBroadPhaseCallback* callback);

	/**
	\brief Retrieves the PxBroadPhaseCallback pointer set with setBroadPhaseCallback().

	\return The current broad-phase callback pointer. See #PxBroadPhaseCallback.

	\see PxBroadPhaseCallback setBroadPhaseCallback()
	*/
	INTEROP_API CPxBroadPhaseCallback* CPxScene_getBroadPhaseCallback(CPxScene* cpp);

	/**
	\brief Sets the shared global filter data which will get passed into the filter shader.

	\note It is the user's responsibility to ensure that changing the shared global filter data does not change the filter output value for existing pairs.
		  If the filter output for existing pairs does change nonetheless then such a change will not take effect until the pair gets refiltered.
		  resetFiltering() can be used to explicitly refilter the pairs of specific objects.

	\note The provided data will get copied to internal buffers and this copy will be used for filtering calls.

	\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored.

	\param[in] data The shared global filter shader data.
	\param[in] dataSize Size of the shared global filter shader data (in bytes).

	\see getFilterShaderData() PxSceneDesc.filterShaderData PxSimulationFilterShader
	*/
	INTEROP_API void CPxScene_setFilterShaderData(CPxScene* cpp, const void* data, CPxU32 dataSize);

	/**
	\brief Gets the shared global filter data in use for this scene.

	\note The reference points to a copy of the original filter data specified in #PxSceneDesc.filterShaderData or provided by #setFilterShaderData().

	\return Shared filter data for filter shader.

	\see getFilterShaderDataSize() setFilterShaderData() PxSceneDesc.filterShaderData PxSimulationFilterShader
	*/
	INTEROP_API const void* CPxScene_getFilterShaderData(CPxScene* cpp);

	/**
	\brief Gets the size of the shared global filter data (#PxSceneDesc.filterShaderData)

	\return Size of shared filter data [bytes].

	\see getFilterShaderData() PxSceneDesc.filterShaderDataSize PxSimulationFilterShader
	*/
	INTEROP_API CPxU32 CPxScene_getFilterShaderDataSize(CPxScene* cpp);

	/**
	\brief Gets the custom collision filter shader in use for this scene.

	\return Filter shader class that defines the collision pair filtering.

	\see PxSceneDesc.filterShader PxSimulationFilterShader
	*/
	INTEROP_API CPxSimulationFilterShader* CPxScene_getFilterShader(CPxScene* cpp);

	/**
	\brief Gets the custom collision filter callback in use for this scene.

	\return Filter callback class that defines the collision pair filtering.

	\see PxSceneDesc.filterCallback PxSimulationFilterCallback
	*/
	INTEROP_API CPxSimulationFilterCallback* CPxScene_getFilterCallback(CPxScene* cpp);

	/**
	\brief Marks the object to reset interactions and re-run collision filters in the next simulation step.

	This call forces the object to remove all existing collision interactions, to search anew for existing contact
	pairs and to run the collision filters again for found collision pairs.

	\note The operation is supported for PxRigidActor objects only.

	\note All persistent state of existing interactions will be lost and can not be retrieved even if the same collison pair
	is found again in the next step. This will mean, for example, that you will not get notified about persistent contact
	for such an interaction (see #PxPairFlag::eNOTIFY_TOUCH_PERSISTS), the contact pair will be interpreted as newly found instead.

	\note Lost touch contact reports will be sent for every collision pair which includes this shape, if they have
	been requested through #PxPairFlag::eNOTIFY_TOUCH_LOST or #PxPairFlag::eNOTIFY_THRESHOLD_FORCE_LOST.

	\note This is an expensive operation, don't use it if you don't have to.

	\note Can be used to retrieve collision pairs that were killed by the collision filters (see #PxFilterFlag::eKILL)

	\note It is invalid to use this method if the actor has not been added to a scene already.

	\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.

	\note Do not use this method while the simulation is running.

	<b>Sleeping:</b> Does wake up the actor.

	\param[in] actor The actor for which to re-evaluate interactions.
	\return True if success

	\see PxSimulationFilterShader PxSimulationFilterCallback
	*/
	INTEROP_API bool CPxScene_resetFiltering(CPxScene* cpp, CPxActor* actor);

	/**
	\brief Marks the object to reset interactions and re-run collision filters for specified shapes in the next simulation step.

	This is a specialization of the resetFiltering(PxActor& actor) method and allows to reset interactions for specific shapes of
	a PxRigidActor.

	\note Do not use this method while the simulation is running.

	<b>Sleeping:</b> Does wake up the actor.

	\param[in] actor The actor for which to re-evaluate interactions.
	\param[in] shapes The shapes for which to re-evaluate interactions.
	\param[in] shapeCount Number of shapes in the list.

	\see PxSimulationFilterShader PxSimulationFilterCallback
	*/
	INTEROP_API bool CPxScene_resetFiltering_shapes(CPxScene* cpp, CPxRigidActor* actor, CPxShape** shapes, CPxU32 shapeCount);

	/**
	\brief Gets the pair filtering mode for kinematic-kinematic pairs.

	\return Filtering mode for kinematic-kinematic pairs.

	\see PxPairFilteringMode PxSceneDesc
	*/
	INTEROP_API CPxPairFilteringMode CPxScene_getKinematicKinematicFilteringMode(CPxScene* cpp);

	/**
	\brief Gets the pair filtering mode for static-kinematic pairs.

	\return Filtering mode for static-kinematic pairs.

	\see PxPairFilteringMode PxSceneDesc
	*/
	INTEROP_API CPxPairFilteringMode CPxScene_getStaticKinematicFilteringMode(CPxScene* cpp);

	/**
	\brief Advances the simulation by an elapsedTime time.

	\note Large elapsedTime values can lead to instabilities. In such cases elapsedTime
	should be subdivided into smaller time intervals and simulate() should be called
	multiple times for each interval.

	Calls to simulate() should pair with calls to fetchResults():
	Each fetchResults() invocation corresponds to exactly one simulate()
	invocation; calling simulate() twice without an intervening fetchResults()
	or fetchResults() twice without an intervening simulate() causes an error
	condition.

	scene->simulate();
	...do some processing until physics is computed...
	scene->fetchResults();
	...now results of run may be retrieved.


	\param[in] elapsedTime Amount of time to advance simulation by. The parameter has to be larger than 0, else the resulting behavior will be undefined. <b>Range:</b> (0, PX_MAX_F32)
	\param[in] completionTask if non-NULL, this task will have its refcount incremented in simulate(), then
	decremented when the scene is ready to have fetchResults called. So the task will not run until the
	application also calls removeReference().
	\param[in] scratchMemBlock a memory region for physx to use for temporary data during simulation. This block may be reused by the application
	after fetchResults returns. Must be aligned on a 16-byte boundary
	\param[in] scratchMemBlockSize the size of the scratch memory block. Must be a multiple of 16K.
	\param[in] controlSimulation if true, the scene controls its PxTaskManager simulation state. Leave
	true unless the application is calling the PxTaskManager start/stopSimulation() methods itself.
	\return True if success

	\see fetchResults() checkResults()
	*/
	INTEROP_API bool CPxScene_simulate(CPxScene* cpp, CPxReal elapsedTime, CPxBaseTask* completionTask = nullptr, void* scratchMemBlock = nullptr, CPxU32 scratchMemBlockSize = 0, bool controlSimulation = true);

	/**
	\brief Performs dynamics phase of the simulation pipeline.

	\note Calls to advance() should follow calls to fetchCollision(). An error message will be issued if this sequence is not followed.

	\param[in] completionTask if non-NULL, this task will have its refcount incremented in advance(), then
	decremented when the scene is ready to have fetchResults called. So the task will not run until the
	application also calls removeReference().
	\return True if success
	*/
	INTEROP_API bool CPxScene_advance(CPxScene* cpp, CPxBaseTask* completionTask = nullptr);

	/**
	\brief Performs collision detection for the scene over elapsedTime

	\note Calls to collide() should be the first method called to simulate a frame.


	\param[in] elapsedTime Amount of time to advance simulation by. The parameter has to be larger than 0, else the resulting behavior will be undefined. <b>Range:</b> (0, PX_MAX_F32)
	\param[in] completionTask if non-NULL, this task will have its refcount incremented in collide(), then
	decremented when the scene is ready to have fetchResults called. So the task will not run until the
	application also calls removeReference().
	\param[in] scratchMemBlock a memory region for physx to use for temporary data during simulation. This block may be reused by the application
	after fetchResults returns. Must be aligned on a 16-byte boundary
	\param[in] scratchMemBlockSize the size of the scratch memory block. Must be a multiple of 16K.
	\param[in] controlSimulation if true, the scene controls its PxTaskManager simulation state. Leave
	true unless the application is calling the PxTaskManager start/stopSimulation() methods itself.
	\return True if success
	*/
	INTEROP_API bool CPxScene_collide(CPxScene* cpp, CPxReal elapsedTime, CPxBaseTask* completionTask = nullptr, void* scratchMemBlock = nullptr, CPxU32 scratchMemBlockSize = 0, bool controlSimulation = true);


	/**
	\brief This checks to see if the simulation run has completed.

	This does not cause the data available for reading to be updated with the results of the simulation, it is simply a status check.
	The bool will allow it to either return immediately or block waiting for the condition to be met so that it can return true

	\param[in] block When set to true will block until the condition is met.
	\return True if the results are available.

	\see simulate() fetchResults()
	*/
	INTEROP_API bool CPxScene_checkResults(CPxScene* cpp, bool block = false);

	/**
	This method must be called after collide() and before advance(). It will wait for the collision phase to finish. If the user makes an illegal simulation call, the SDK will issue an error
	message.

	\param[in] block When set to true will block until the condition is met, which is collision must finish running.
	*/
	INTEROP_API bool CPxScene_fetchCollision(CPxScene* cpp, bool block = false);

	/**
	This is the big brother to checkResults() it basically does the following:

	\code
	if ( checkResults(block) )
	{
		fire appropriate callbacks
		swap buffers
		return true
	}
	else
		return false

	\endcode

	\param[in] block When set to true will block until results are available.
	\param[out] errorState Used to retrieve hardware error codes. A non zero value indicates an error.
	\return True if the results have been fetched.

	\see simulate() checkResults()
	*/
	INTEROP_API bool CPxScene_fetchResults(CPxScene* cpp, bool block = false, CPxU32* errorState = nullptr);

	/**
	This call performs the first section of fetchResults, and returns a pointer to the contact streams output by the simulation. It can be used to process contact pairs in parallel, which is often a limiting factor
	for fetchResults() performance.

	After calling this function and processing the contact streams, call fetchResultsFinish(). Note that writes to the simulation are not
	permitted between the start of fetchResultsStart() and the end of fetchResultsFinish().

	\param[in] block When set to true will block until results are available.
	\param[out] contactPairs an array of pointers to contact pair headers
	\param[out] nbContactPairs the number of contact pairs
	\return True if the results have been fetched.

	\see simulate() checkResults() fetchResults() fetchResultsFinish()
	*/
	INTEROP_API bool CPxScene_fetchResultsStart(CPxScene* cpp, CPxContactPairHeader** contactPairs, CPxU32* nbContactPairs, bool block = false);

	/**
	This call processes all event callbacks in parallel. It takes a continuation task, which will be executed once all callbacks have been processed.

	This is a utility function to make it easier to process callbacks in parallel using the PhysX task system. It can only be used in conjunction with
	fetchResultsStart(...) and fetchResultsFinish(...)

	\param[in] continuation The task that will be executed once all callbacks have been processed.
	*/
	INTEROP_API void CPxScene_processCallbacks(CPxScene* cpp, CPxBaseTask* continuation);

	/**
	This call performs the second section of fetchResults.

	It must be called after fetchResultsStart() returns and contact reports have been processed.

	Note that once fetchResultsFinish() has been called, the contact streams returned in fetchResultsStart() will be invalid.

	\param[out] errorState Used to retrieve hardware error codes. A non zero value indicates an error.

	\see simulate() checkResults() fetchResults() fetchResultsStart()
	*/
	INTEROP_API void CPxScene_fetchResultsFinish(CPxScene* cpp, CPxU32* errorState = nullptr);

	/**
	This call performs the synchronization of particle system data copies.
	 */
	INTEROP_API void CPxScene_fetchResultsParticleSystem(CPxScene* cpp);

	/**
	\brief Clear internal buffers and free memory.

	This method can be used to clear buffers and free internal memory without having to destroy the scene. Can be useful if
	the physics data gets streamed in and a checkpoint with a clean state should be created.

	\note It is not allowed to call this method while the simulation is running. The call will fail.

	\param[in] sendPendingReports When set to true pending reports will be sent out before the buffers get cleaned up (for instance lost touch contact/trigger reports due to deleted objects).
	*/
	INTEROP_API void CPxScene_flushSimulation(CPxScene* cpp, bool sendPendingReports = false);

	/**
	\brief Sets a constant gravity for the entire scene.

	\note Do not use this method while the simulation is running.

	<b>Sleeping:</b> Does <b>NOT</b> wake the actor up automatically.

	\param[in] vec A new gravity vector(e.g. PxVec3(0.0f,-9.8f,0.0f) ) <b>Range:</b> force vector

	\see PxSceneDesc.gravity getGravity()
	*/
	INTEROP_API void CPxScene_setGravity(CPxScene* cpp, const CPxVec3 vec);

	/**
	\brief Retrieves the current gravity setting.

	\return The current gravity for the scene.

	\see setGravity() PxSceneDesc.gravity
	*/
	INTEROP_API CPxVec3 CPxScene_getGravity(CPxScene* cpp);

	/**
	\brief Set the bounce threshold velocity.  Collision speeds below this threshold will not cause a bounce.

	\note Do not use this method while the simulation is running.

	\see PxSceneDesc::bounceThresholdVelocity, getBounceThresholdVelocity
	*/
	INTEROP_API void CPxScene_setBounceThresholdVelocity(CPxScene* cpp, const CPxReal t);

	/**
	\brief Return the bounce threshold velocity.

	\see PxSceneDesc.bounceThresholdVelocity, setBounceThresholdVelocity
	*/
	INTEROP_API CPxReal CPxScene_getBounceThresholdVelocity(CPxScene* cpp);

	/**
	\brief Sets the maximum number of CCD passes

	\note Do not use this method while the simulation is running.

	\param[in] ccdMaxPasses Maximum number of CCD passes

	\see PxSceneDesc.ccdMaxPasses getCCDMaxPasses()
	*/
	INTEROP_API void CPxScene_setCCDMaxPasses(CPxScene* cpp, CPxU32 ccdMaxPasses);

	/**
	\brief Gets the maximum number of CCD passes.

	\return The maximum number of CCD passes.

	\see PxSceneDesc::ccdMaxPasses setCCDMaxPasses()
	*/
	INTEROP_API CPxU32 CPxScene_getCCDMaxPasses(CPxScene* cpp);

	/**
	\brief Set the maximum CCD separation.

	\note Do not use this method while the simulation is running.

	\see PxSceneDesc::ccdMaxSeparation, getCCDMaxSeparation
	*/
	INTEROP_API void CPxScene_setCCDMaxSeparation(CPxScene* cpp, const CPxReal t);

	/**
	\brief Gets the maximum CCD separation.

	\return The maximum CCD separation.

	\see PxSceneDesc::ccdMaxSeparation setCCDMaxSeparation()
	*/
	INTEROP_API CPxReal CPxScene_getCCDMaxSeparation(CPxScene* cpp);

	/**
	\brief Set the CCD threshold.

	\note Do not use this method while the simulation is running.

	\see PxSceneDesc::ccdThreshold, getCCDThreshold
	*/
	INTEROP_API void CPxScene_setCCDThreshold(CPxScene* cpp, const CPxReal t);

	/**
	\brief Gets the CCD threshold.

	\return The CCD threshold.

	\see PxSceneDesc::ccdThreshold setCCDThreshold()
	*/
	INTEROP_API CPxReal CPxScene_getCCDThreshold(CPxScene* cpp);

	/**
	\brief Set the max bias coefficient.

	\note Do not use this method while the simulation is running.

	\see PxSceneDesc::maxBiasCoefficient, getMaxBiasCoefficient
	*/
	INTEROP_API void CPxScene_setMaxBiasCoeffecient(CPxScene* cpp, const CPxReal t);

	/**
	\brief Gets the max bias coefficient.

	\return The max bias coefficient.

	\see PxSceneDesc::maxBiasCoefficient setMaxBiasCoefficient()
	*/
	INTEROP_API CPxReal CPxScene_getMaxBiasCoeffecient(CPxScene* cpp);

	/**
	\brief Set the friction offset threshold.

	\note Do not use this method while the simulation is running.

	\see PxSceneDesc::frictionOffsetThreshold, getFrictionOffsetThreshold
	*/
	INTEROP_API void CPxScene_setFrictionOffsetThreshold(CPxScene* cpp, const CPxReal t);

	/**
	\brief Gets the friction offset threshold.

	\see PxSceneDesc::frictionOffsetThreshold, setFrictionOffsetThreshold
	*/
	INTEROP_API CPxReal CPxScene_getFrictionOffsetThreshold(CPxScene* cpp);

	/**
	\brief Set the friction correlation distance.

	\note Do not use this method while the simulation is running.

	\see PxSceneDesc::frictionCorrelationDistance, getFrictionCorrelationDistance
	*/
	INTEROP_API void CPxScene_setFrictionCorrelationDistance(CPxScene* cpp, const CPxReal t);

	/**
	\brief Gets the friction correlation distance.

	\see PxSceneDesc::frictionCorrelationDistance, setFrictionCorrelationDistance
	*/
	INTEROP_API CPxReal CPxScene_getFrictionCorrelationDistance(CPxScene* cpp);

	/**
	\brief Return the solver model.
	\see PxSolverType, PxSceneDesc::solverType
	*/
	INTEROP_API CPxSolverType CPxScene_getSolverType(CPxScene* cpp);

	/**
	\brief Function that lets you set debug visualization parameters.

	Returns false if the value passed is out of range for usage specified by the enum.

	\note Do not use this method while the simulation is running.

	\param[in] param	Parameter to set. See #PxVisualizationParameter
	\param[in] value	The value to set, see #PxVisualizationParameter for allowable values. Setting to zero disables visualization for the specified property, setting to a positive value usually enables visualization and defines the scale factor.
	\return False if the parameter is out of range.

	\see getVisualizationParameter PxVisualizationParameter getRenderBuffer()
	*/
	INTEROP_API bool CPxScene_setVisualizationParameter(CPxScene* cpp, CPxVisualizationParameter param, CPxReal value);

	/**
	\brief Function that lets you query debug visualization parameters.

	\param[in] paramEnum The Parameter to retrieve.
	\return The value of the parameter.

	\see setVisualizationParameter PxVisualizationParameter
	*/
	INTEROP_API CPxReal CPxScene_getVisualizationParameter(CPxScene* cpp, CPxVisualizationParameter paramEnum);

	/**
	\brief Defines a box in world space to which visualization geometry will be (conservatively) culled. Use a non-empty culling box to enable the feature, and an empty culling box to disable it.

	\note Do not use this method while the simulation is running.

	\param[in] box the box to which the geometry will be culled. Empty box to disable the feature.
	\see setVisualizationParameter getVisualizationCullingBox getRenderBuffer()
	*/
	INTEROP_API void CPxScene_setVisualizationCullingBox(CPxScene* cpp, const CPxBounds3 box);

	/**
	\brief Retrieves the visualization culling box.

	\return the box to which the geometry will be culled.
	\see setVisualizationParameter setVisualizationCullingBox
	*/
	INTEROP_API CPxBounds3 CPxScene_getVisualizationCullingBox(CPxScene* cpp);

	/**
	\brief Retrieves the render buffer.
	
	This will contain the results of any active visualization for this scene.

	\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will result in undefined behaviour.

	\return The render buffer.

	\see PxRenderBuffer
	*/
	INTEROP_API CPxRenderBuffer CPxScene_getRenderBuffer(CPxScene* cpp);

	/**
	\brief Call this method to retrieve statistics for the current simulation step.

	\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored.

	\param[out] stats Used to retrieve statistics for the current simulation step.

	\see PxSimulationStatistics
	*/
	INTEROP_API void CPxScene_getSimulationStatistics(CPxScene* cpp, CPxSimulationStatistics* stats);

	/**
	\brief Returns broad-phase type.

	\return Broad-phase type
	*/
	INTEROP_API CPxBroadPhaseType CPxScene_getBroadPhaseType(CPxScene* cpp);

	/**
	\brief Gets broad-phase caps.

	\param[out]	caps	Broad-phase caps
	\return True if success
	*/
	INTEROP_API bool CPxScene_getBroadPhaseCaps(CPxScene* cpp, CPxBroadPhaseCaps* caps);

	/**
	\brief Returns number of regions currently registered in the broad-phase.

	\return Number of regions
	*/
	INTEROP_API CPxU32 CPxScene_getNbBroadPhaseRegions(CPxScene* cpp);

	/**
	\brief Gets broad-phase regions.

	\param[out]	userBuffer	Returned broad-phase regions
	\param[in]	bufferSize	Size of userBuffer
	\param[in]	startIndex	Index of first desired region, in [0 ; getNbRegions()[
	\return Number of written out regions
	*/
	INTEROP_API CPxU32 CPxScene_getBroadPhaseRegions(CPxScene* cpp, CPxBroadPhaseRegionInfo** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Adds a new broad-phase region.

	The bounds for the new region must be non-empty, otherwise an error occurs and the call is ignored.

	Note that by default, objects already existing in the SDK that might touch this region will not be automatically
	added to the region. In other words the newly created region will be empty, and will only be populated with new
	objects when they are added to the simulation, or with already existing objects when they are updated.

	It is nonetheless possible to override this default behavior and let the SDK populate the new region automatically
	with already existing objects overlapping the incoming region. This has a cost though, and it should only be used
	when the game can not guarantee that all objects within the new region will be added to the simulation after the
	region itself.

	Objects automatically move from one region to another during their lifetime. The system keeps tracks of what
	regions a given object is in. It is legal for an object to be in an arbitrary number of regions. However if an
	object leaves all regions, or is created outside of all regions, several things happen:
		- collisions get disabled for this object
		- if a PxBroadPhaseCallback object is provided, an "out-of-bounds" event is generated via that callback
		- if a PxBroadPhaseCallback object is not provided, a warning/error message is sent to the error stream

	If an object goes out-of-bounds and user deletes it during the same frame, neither the out-of-bounds event nor the
	error message is generated.

	\param[in]	region			User-provided region data
	\param[in]	populateRegion	Automatically populate new region with already existing objects overlapping it
	\return Handle for newly created region, or 0xffffffff in case of failure.
	\see	PxBroadPhaseRegion PxBroadPhaseCallback
	*/
	INTEROP_API CPxU32 CPxScene_addBroadPhaseRegions(CPxScene* cpp, const CPxBroadPhaseRegion region, bool populateRegion = false);

	/**
	\brief Removes a new broad-phase region.

	If the region still contains objects, and if those objects do not overlap any region any more, they are not
	automatically removed from the simulation. Instead, the PxBroadPhaseCallback::onObjectOutOfBounds notification
	is used for each object. Users are responsible for removing the objects from the simulation if this is the
	desired behavior.

	If the handle is invalid, or if a valid handle is removed twice, an error message is sent to the error stream.

	\param[in]	handle	Region's handle, as returned by PxScene::addBroadPhaseRegion.
	\return True if success
	*/
	INTEROP_API bool CPxScene_removeBroadPhaseRegion(CPxScene* cpp, CPxU32 handle);

	/**
	\brief Get the task manager associated with this scene

	\return the task manager associated with the scene
	*/
	INTEROP_API CPxTaskManager* CPxScene_getTaskManager(CPxScene* cpp);

	/**
	\brief Lock the scene for reading from the calling thread.

	When the PxSceneFlag::eREQUIRE_RW_LOCK flag is enabled lockRead() must be
	called before any read calls are made on the scene.

	Multiple threads may read at the same time, no threads may read while a thread is writing.
	If a call to lockRead() is made while another thread is holding a write lock
	then the calling thread will be blocked until the writing thread calls unlockWrite().

	\note Lock upgrading is *not* supported, that means it is an error to
	call lockRead() followed by lockWrite().

	\note Recursive locking is supported but each lockRead() call must be paired with an unlockRead().

	\param file String representing the calling file, for debug purposes
	\param line The source file line number, for debug purposes
	*/
	INTEROP_API void CPxScene_lockRead(CPxScene* cpp, const char* file = nullptr, CPxU32 line = 0);

	/**
	\brief Unlock the scene from reading.
	
	\note Each unlockRead() must be paired with a lockRead() from the same thread.
	*/
	INTEROP_API void CPxScene_unlockRead(CPxScene* cpp);

	/**
	\brief Lock the scene for writing from this thread.

	When the PxSceneFlag::eREQUIRE_RW_LOCK flag is enabled lockWrite() must be
	called before any write calls are made on the scene.

	Only one thread may write at a time and no threads may read while a thread is writing.
	If a call to lockWrite() is made and there are other threads reading then the
	calling thread will be blocked until the readers complete.

	Writers have priority. If a thread is blocked waiting to write then subsequent calls to
	lockRead() from other threads will be blocked until the writer completes.

	\note If multiple threads are waiting to write then the thread that is first
	granted access depends on OS scheduling.

	\note Recursive locking is supported but each lockWrite() call must be paired
	with an unlockWrite().

	\note If a thread has already locked the scene for writing then it may call
	lockRead().

	\param file String representing the calling file, for debug purposes
	\param line The source file line number, for debug purposes
	*/
	INTEROP_API void CPxScene_lockWrite(CPxScene* cpp, const char* file = nullptr, CPxU32 line = 0);

	/**
	\brief Unlock the scene from writing.

	\note Each unlockWrite() must be paired with a lockWrite() from the same thread.
	*/
	INTEROP_API void CPxScene_unlockWrite(CPxScene* cpp);

	/**
	\brief set the cache blocks that can be used during simulate().

	Each frame the simulation requires memory to store contact, friction, and contact cache data. This memory is used in blocks of 16K.
	Each frame the blocks used by the previous frame are freed, and may be retrieved by the application using PxScene::flushSimulation()

	This call will force allocation of cache blocks if the numBlocks parameter is greater than the currently allocated number
	of blocks, and less than the max16KContactDataBlocks parameter specified at scene creation time.

	\note Do not use this method while the simulation is running.

	\param[in] numBlocks The number of blocks to allocate.

	\see PxSceneDesc.nbContactDataBlocks PxSceneDesc.maxNbContactDataBlocks flushSimulation() getNbContactDataBlocksUsed getMaxNbContactDataBlocksUsed
	*/
	INTEROP_API void CPxScene_setNbContactDataBlocks(CPxScene* cpp, CPxU32 numBlocks);

	/**
	\brief get the number of cache blocks currently used by the scene

	This function may not be called while the scene is simulating

	\return the number of cache blocks currently used by the scene

	\see PxSceneDesc.nbContactDataBlocks PxSceneDesc.maxNbContactDataBlocks flushSimulation() setNbContactDataBlocks() getMaxNbContactDataBlocksUsed()
	*/
	INTEROP_API CPxU32 CPxScene_getNbContactDataBlocksUsed(CPxScene* cpp);

	/**
	\brief get the maximum number of cache blocks used by the scene

	This function may not be called while the scene is simulating

	\return the maximum number of cache blocks everused by the scene

	\see PxSceneDesc.nbContactDataBlocks PxSceneDesc.maxNbContactDataBlocks flushSimulation() setNbContactDataBlocks() getNbContactDataBlocksUsed()
	*/
	INTEROP_API CPxU32 CPxScene_getMaxNbContactDataBlocksUsed(CPxScene* cpp);

	/**
	\brief Return the value of PxSceneDesc::contactReportStreamBufferSize that was set when creating the scene with PxPhysics::createScene

	\see PxSceneDesc::contactReportStreamBufferSize, PxPhysics::createScene
	*/
	INTEROP_API CPxU32 CPxScene_getContactReportStreamBufferSize(CPxScene* cpp);

	/**
	\brief Sets the number of actors required to spawn a separate rigid body solver thread.

	\note Do not use this method while the simulation is running.

	\param[in] solverBatchSize Number of actors required to spawn a separate rigid body solver thread.

	\see PxSceneDesc.solverBatchSize getSolverBatchSize()
	*/
	INTEROP_API void CPxScene_setSolverBatchSize(CPxScene* cpp, CPxU32 solverBatchSize);

	/**
	\brief Retrieves the number of actors required to spawn a separate rigid body solver thread.

	\return Current number of actors required to spawn a separate rigid body solver thread.

	\see PxSceneDesc.solverBatchSize setSolverBatchSize()
	*/
	INTEROP_API CPxU32 CPxScene_getSolverBatchSize(CPxScene* cpp);

	/**
	\brief Sets the number of articulations required to spawn a separate rigid body solver thread.

	\note Do not use this method while the simulation is running.

	\param[in] solverBatchSize Number of articulations required to spawn a separate rigid body solver thread.

	\see PxSceneDesc.solverBatchSize getSolverArticulationBatchSize()
	*/
	INTEROP_API void CPxScene_setSolverArticulationBatchSize(CPxScene* cpp, CPxU32 solverBatchSize);

	/**
	\brief Retrieves the number of articulations required to spawn a separate rigid body solver thread.

	\return Current number of articulations required to spawn a separate rigid body solver thread.

	\see PxSceneDesc.solverBatchSize setSolverArticulationBatchSize()
	*/
	INTEROP_API CPxU32 CPxScene_getSolverArticulationBatchSize(CPxScene* cpp);

	/**
	\brief Returns the wake counter reset value.

	\return Wake counter reset value

	\see PxSceneDesc.wakeCounterResetValue
	*/
	INTEROP_API CPxReal CPxScene_getWakeCounterResetValue(CPxScene* cpp);

	/**
	\brief Shift the scene origin by the specified vector.

	The poses of all objects in the scene and the corresponding data structures will get adjusted to reflect the new origin location
	(the shift vector will get subtracted from all object positions).

	\note It is the user's responsibility to keep track of the summed total origin shift and adjust all input/output to/from PhysX accordingly.

	\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored.

	\note Make sure to propagate the origin shift to other dependent modules (for example, the character controller module etc.).

	\note This is an expensive operation and we recommend to use it only in the case where distance related precision issues may arise in areas far from the origin.

	\param[in] shift Translation vector to shift the origin by.
	*/
	INTEROP_API void CPxScene_shiftOrigin(CPxScene* cpp, const CPxVec3 shift);

	/**
	\brief Returns the Pvd client associated with the scene.
	\return the client, NULL if no PVD supported.
	*/
	INTEROP_API CPxPvdSceneClient* CPxScene_getScenePvdClient(CPxScene* cpp);

	/**
	\brief Get the PxGpuDynamicsMemoryConfig that was passed into PxPhysics::createScene() as part of PxSceneDesc.

	\note This will return the values passed as initial configuration, for the actual minimal configuration that would be needed
	for a specific simulation of a scene, see PxSimulationStatistics::gpuDynamicsMemoryConfigStatistics.

	\return The PxGpuDynamicsMemoryConfig used during scene creation.

	\see PxSceneDesc::gpuDynamicsConfig, PxSimulationStatistics::gpuDynamicsMemoryConfigStatistics
	*/
	INTEROP_API CPxGpuDynamicsMemoryConfig CPxScene_getGpuDynamicsConfig(CPxScene* cpp);

	/**
	\brief Get the direct-GPU API instance for this scene.

	\see PxDirectGPUAPI for the supported direct-GPU operations.

	Each object of PxDirectGPUAPI is directly associated with a PxScene, and there is only one PxDirectGPUAPI object per scene.
	*/
	INTEROP_API CPxDirectGPUAPI* CPxScene_getDirectGPUAPI(CPxScene* cpp);

	/**
	\brief Sets the post-solve callback for deformable surface GPU computations. Allows to schedule custom work to be done by the GPU as soon as possible after the deformable surface solver finishes.
	\param postSolveCallback Pointer to the callback implementation.
	*/
	INTEROP_API void CPxScene_setDeformableSurfaceGpuPostSolveCallback(CPxScene* cpp, CPxPostSolveCallback* postSolveCallback);

	/**
	\brief Sets the post-solve callback for deformable volume GPU computations. Allows to schedule custom work to be done by the GPU as soon as possible after the deformable volume solver finishes.
	\param postSolveCallback Pointer to the callback implementation.
	*/
	INTEROP_API void CPxScene_setDeformableVolumeGpuPostSolveCallback(CPxScene* cpp, CPxPostSolveCallback* postSolveCallback);

	INTEROP_API void CPxScene_userData_set(CPxScene* cpp, void* userData);
	INTEROP_API void* CPxScene_userData_get(CPxScene* cpp);
	INTEROP_API void** CPxScene_userData_ref(CPxScene* cpp);
}