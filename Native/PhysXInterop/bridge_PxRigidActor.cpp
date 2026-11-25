#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxRigidActor.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxRigidActor, cpp)

	void CPxRigidActor_release(CPxRigidActor* cpp)
	{
		THIS->release();
	}

	CPxU32 CPxRigidActor_getInternalActorIndex(CPxRigidActor* cpp)
	{
		return THIS->getInternalActorIndex();
	}

	CPxTransform CPxRigidActor_getGlobalPose(CPxRigidActor* cpp)
	{
		return getTransformAliased(THIS->getGlobalPose());
	}

	void CPxRigidActor_setGlobalPose(CPxRigidActor* cpp, const CPxTransform* pose, bool autowake)
	{
		THIS->setGlobalPose(getTransformNative(*pose), autowake);
	}

	bool CPxRigidActor_attachShape(CPxRigidActor* cpp, CPxShape* shape)
	{
		return THIS->attachShape(*GET(physx::PxShape, shape));
	}

	void CPxRigidActor_detachShape(CPxRigidActor* cpp, CPxShape* shape, bool wakeOnLostTouch)
	{
		THIS->detachShape(*GET(physx::PxShape, shape), wakeOnLostTouch);
	}

	CPxU32 CPxRigidActor_getNbShapes(CPxRigidActor* cpp)
	{
		return THIS->getNbShapes();
	}

	CPxU32 CPxRigidActor_getShapes(CPxRigidActor* cpp, CPxShape** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getShapes((physx::PxShape**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxRigidActor_getNbConstraints(CPxRigidActor* cpp)
	{
		return THIS->getNbConstraints();
	}

	CPxU32 CPxRigidActor_getConstraints(CPxRigidActor* cpp, CPxConstraint** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getConstraints((physx::PxConstraint**)userBuffer, bufferSize, startIndex);
	}
}