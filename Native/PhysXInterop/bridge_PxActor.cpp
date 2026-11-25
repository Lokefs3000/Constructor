#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxActor.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxActor, cpp)

	void CPxActor_release(CPxActor* cpp)
	{
		THIS->release();
	}

	CPxActorType CPxActor_getType(CPxActor* cpp)
	{
		return (CPxActorType)THIS->getType();
	}

	CPxScene* CPxActor_getScene(CPxActor* cpp)
	{
		return THIS->getScene();
	}

	void CPxActor_setName(CPxActor* cpp, const char* name)
	{
		THIS->setName(name);
	}

	const char* CPxActor_getName(CPxActor* cpp)
	{
		return THIS->getName();
	}

	CPxBounds3 CPxActor_getWorldBounds(CPxActor* cpp, float inflation)
	{
		return getBounds3Aliased(THIS->getWorldBounds(inflation));
	}

	void CPxActor_setActorFlag(CPxActor* cpp, CPxActorFlag flag, bool value)
	{
		THIS->setActorFlag(CHANGE(physx::PxActorFlag::Enum, flag), value);
	}

	void CPxActor_setActorFlags(CPxActor* cpp, CPxActorFlag inFlags)
	{
		THIS->setActorFlags(CHANGE(physx::PxActorFlag::Enum, inFlags));
	}

	CPxActorFlag CPxActor_getActorFlags(CPxActor* cpp)
	{
		return (CPxActorFlag)(physx::PxU8)THIS->getActorFlags();
	}

	void CPxActor_setDominanceGroup(CPxActor* cpp, CPxDominanceGroup dominanceGroup)
	{
		THIS->setDominanceGroup(dominanceGroup);
	}

	CPxDominanceGroup CPxActor_getDominanceGroup(CPxActor* cpp)
	{
		return (CPxDominanceGroup)THIS->getDominanceGroup();
	}

	void CPxActor_setOwnerClient(CPxActor* cpp, CPxClientID inClient)
	{
		THIS->setOwnerClient(inClient);
	}

	CPxClientID CPxActor_getOwnerClient(CPxActor* cpp)
	{
		return (CPxClientID)THIS->getOwnerClient();
	}

	CPxAggregate* CPxActor_getAggregate(CPxActor* cpp)
	{
		return THIS->getAggregate();
	}

	bool CPxActor_setEnviormentID(CPxActor* cpp, CPxU32 envID)
	{
		return THIS->setEnvironmentID(envID);
	}

	CPxU32 CPxActor_getEnviormentID(CPxActor* cpp)
	{
		return THIS->getEnvironmentID();
	}

	void CPxActor_userData_set(CPxActor* cpp, void* userData)
	{
		THIS->userData = userData;
	}

	void* CPxActor_userData_get(CPxActor* cpp)
	{
		return THIS->userData;
	}

	void** CPxActor_userData_ref(CPxActor* cpp)
	{
		return &THIS->userData;
	}
}
