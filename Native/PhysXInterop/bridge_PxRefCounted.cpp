#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxMaterial.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxRefCounted, cpp)

	void CPxRefCounted_release(CPxRefCounted* cpp)
	{
		THIS->release();
	}

	CPxU32 CPxRefCounted_getReferenceCount(CPxRefCounted* cpp)
	{
		return THIS->getReferenceCount();
	}

	void CPxRefCounted_acquireReference(CPxRefCounted* cpp)
	{
		THIS->acquireReference();
	}
}