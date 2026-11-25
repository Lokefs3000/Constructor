#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxRigidStatic.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxRigidStatic, cpp)

	const char* CPxRigidStatic_getConcreteTypeName(CPxRigidStatic* cpp)
	{
		return THIS->getConcreteTypeName();
	}
}