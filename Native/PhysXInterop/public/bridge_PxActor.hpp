#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Deletes the actor.
	
	Do not keep a reference to the deleted instance.

	If the actor belongs to a #PxAggregate object, it is automatically removed from the aggregate.

	\see PxBase.release(), PxAggregate
	*/
	INTEROP_API void CPxActor_release(CPxActor* cpp);

	/**
	\brief Retrieves the type of actor.

	\return The actor type of the actor.

	\see PxActorType
	*/
	INTEROP_API CPxActorType CPxActor_getType(CPxActor* cpp);

	/**
	\brief Retrieves the scene which this actor belongs to.

	\return Owner Scene. NULL if not part of a scene.

	\see PxScene
	*/
	INTEROP_API CPxScene* CPxActor_getScene(CPxActor* cpp);

	/**
	\brief Sets a name string for the object that can be retrieved with getName().

	This is for debugging and is not used by the SDK. The string is not copied by the SDK,
	only the pointer is stored.

	\param[in] name String to set the objects name to.

	<b>Default:</b> NULL

	\see getName()
	*/
	INTEROP_API void CPxActor_setName(CPxActor* cpp, const char* name);

	/**
	\brief Retrieves the name string set with setName().

	\return Name string associated with object.

	\see setName()
	*/
	INTEROP_API const char* CPxActor_getName(CPxActor* cpp);

	/**
	\brief Retrieves the axis aligned bounding box enclosing the actor.

	\note It is not allowed to use this method while the simulation is running (except during PxScene::collide(),
	in PxContactModifyCallback or in contact report callbacks).

	\param[in] inflation  Scale factor for computed world bounds. Box extents are multiplied by this value.

	\return The actor's bounding box.

	\see PxBounds3
	*/
	INTEROP_API CPxBounds3 CPxActor_getWorldBounds(CPxActor* cpp, float inflation = 1.01f);

	/**
	\brief Raises or clears a particular actor flag.

	See the list of flags #PxActorFlag

	<b>Sleeping:</b> Does <b>NOT</b> wake the actor up automatically.

	\param[in] flag  The PxActor flag to raise(set) or clear. See #PxActorFlag.
	\param[in] value The boolean value to assign to the flag.

	\see PxActorFlag getActorFlags()
	*/
	INTEROP_API void CPxActor_setActorFlag(CPxActor* cpp, CPxActorFlag flag, bool value);

	/**
	\brief Sets the actor flags.

	See the list of flags #PxActorFlag
	\see PxActorFlag setActorFlag()
	*/
	INTEROP_API void CPxActor_setActorFlags(CPxActor* cpp, CPxActorFlag inFlags);

	/**
	\brief Reads the PxActor flags.

	See the list of flags #PxActorFlag

	\return The values of the PxActor flags.

	\see PxActorFlag setActorFlag()
	*/
	INTEROP_API CPxActorFlag CPxActor_getActorFlags(CPxActor* cpp);

	/**
	\brief Assigns dynamic actors a dominance group identifier.

	PxDominanceGroup is a 5 bit group identifier (legal range from 0 to 31).

	The PxScene::setDominanceGroupPair() lets you set certain behaviors for pairs of dominance groups.
	By default every dynamic actor is created in group 0.

	<b>Default:</b> 0

	<b>Sleeping:</b> Changing the dominance group does <b>NOT</b> wake the actor up automatically.

	\param[in] dominanceGroup The dominance group identifier. <b>Range:</b> [0..31]

	\see getDominanceGroup() PxDominanceGroup PxScene::setDominanceGroupPair()
	*/
	INTEROP_API void CPxActor_setDominanceGroup(CPxActor* cpp, CPxDominanceGroup dominanceGroup);

	/**
	\brief Retrieves the value set with setDominanceGroup().

	\return The dominance group of this actor.

	\see setDominanceGroup() PxDominanceGroup PxScene::setDominanceGroupPair()
	*/
	INTEROP_API CPxDominanceGroup CPxActor_getDominanceGroup(CPxActor* cpp);

	/**
	\brief Sets the owner client of an actor.

	This cannot be done once the actor has been placed into a scene.

	<b>Default:</b> PX_DEFAULT_CLIENT

	\see PxClientID PxScene::createClient()
	*/
	INTEROP_API void CPxActor_setOwnerClient(CPxActor* cpp, CPxClientID inClient);

	/**
	\brief Returns the owner client that was specified at creation time.

	This value cannot be changed once the object is placed into the scene.

	\see PxClientID PxScene::createClient()
	*/
	INTEROP_API CPxClientID CPxActor_getOwnerClient(CPxActor* cpp);

	/**
	\brief Retrieves the aggregate the actor might be a part of.

	\return The aggregate the actor is a part of, or NULL if the actor does not belong to an aggregate.

	\see PxAggregate
	*/
	INTEROP_API CPxAggregate* CPxActor_getAggregate(CPxActor* cpp);

	/**
	\brief Sets the environment ID for this actor.

	The environment ID is an extra built-in filter group for the GPU broadphase. Actors will only collide with each-other if they have the
	same environment ID.

	The default value is PX_INVALID_U32. Actors with this ID will collide with other actors, regardless of which environment they are a part of.

	The environment ID must be set before adding the actor to a scene, and cannot change while the actor is in the scene.

	If it is not PX_INVALID_U32, the environment ID must be smaller than 1<<24, i.e. the system does not support more than 1<<24 environments.

	<b>Default:</b> PX_INVALID_U32

	\note	This is not available for CPU broadphases.

	\param[in]	envID	 Environment ID for this actor.
	\return True if success.

	\see getEnvironmentID()
	*/
	INTEROP_API bool CPxActor_setEnviormentID(CPxActor* cpp, CPxU32 envID);

	/**
	\brief Returns the environment ID for this actor.

	\return Environment ID for this actor.

	\see setEnvironmentID()
	*/
	INTEROP_API CPxU32 CPxActor_getEnviormentID(CPxActor* cpp);

	INTEROP_API void CPxActor_userData_set(CPxActor* cpp, void* userData);
	INTEROP_API void* CPxActor_userData_get(CPxActor* cpp);
	INTEROP_API void** CPxActor_userData_ref(CPxActor* cpp);
}