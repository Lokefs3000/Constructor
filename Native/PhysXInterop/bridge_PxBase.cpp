#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxBase.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxBase, cpp)

	void CPxBase_release(CPxBase* cpp)
	{
		THIS->release();
	}

	const char* CPxBase_getConcreteTypeName(CPxBase* cpp)
	{
		return THIS->getConcreteTypeName();
	}

	CPxType CPxBase_getConcreteType(CPxBase* cpp)
	{
		return THIS->getConcreteType();
	}

	void CPxBase_setBaseFlag(CPxBase* cpp, CPxBaseFlag flag, bool value)
	{
		THIS->setBaseFlag(CHANGE(physx::PxBaseFlag::Enum, flag), value);
	}

	void CPxBase_setBaseFlags(CPxBase* cpp, CPxBaseFlag inFlags)
	{
		THIS->setBaseFlags(CHANGE(physx::PxBaseFlag::Enum, inFlags));
	}

	CPxBaseFlag CPxBase_getBaseFlags(CPxBase* cpp)
	{
		return (CPxBaseFlag)(physx::PxU16)THIS->getBaseFlags();
	}

	bool CPxBase_isReleasable(CPxBase* cpp)
	{
		return THIS->isReleasable();
	}
}