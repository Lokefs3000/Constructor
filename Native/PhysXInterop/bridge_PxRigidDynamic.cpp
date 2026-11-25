#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxRigidDynamic.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxRigidDynamic, cpp)

	void CPxRigidDynamic_setKinematicTarget(CPxRigidDynamic* cpp, const CPxTransform* destination)
	{
		THIS->setKinematicTarget(getTransformNative(*destination));
	}

	bool CPxRigidDynamic_getKinematicTarget(CPxRigidDynamic* cpp, CPxTransform* target)
	{
		return THIS->getKinematicTarget(*(physx::PxTransform*)target);
	}

	bool CPxRigidDynamic_isSleeping(CPxRigidDynamic* cpp)
	{
		return THIS->isSleeping();
	}

	void CPxRigidDynamic_setSleepThreshold(CPxRigidDynamic* cpp, CPxReal threshold)
	{
		THIS->setSleepThreshold(threshold);
	}

	CPxReal CPxRigidDynamic_getSleepThreshold(CPxRigidDynamic* cpp)
	{
		return THIS->getSleepThreshold();
	}

	void CPxRigidDynamic_setStabilizationThreshold(CPxRigidDynamic* cpp, CPxReal threshold)
	{
		THIS->setStabilizationThreshold(threshold);
	}

	CPxReal CPxRigidDynamic_getStabilizationThreshold(CPxRigidDynamic* cpp)
	{
		return THIS->getStabilizationThreshold();
	}

	void CPxRigidDynamic_setWakeCounter(CPxRigidDynamic* cpp, CPxReal wakeCounterValue)
	{
		THIS->setWakeCounter(wakeCounterValue);
	}

	CPxReal CPxRigidDynamic_getWakeCounter(CPxRigidDynamic* cpp)
	{
		return THIS->getWakeCounter();
	}

	void CPxRigidDynamic_wakeUp(CPxRigidDynamic* cpp)
	{
		THIS->wakeUp();
	}

	void CPxRigidDynamic_putToSleep(CPxRigidDynamic* cpp)
	{
		THIS->putToSleep();
	}

	CPxRigidDynamicLockFlag CPxRigidDynamic_getRigidDynamicLockFlags(CPxRigidDynamic* cpp)
	{
		return (CPxRigidDynamicLockFlag)(physx::PxU8)THIS->getRigidDynamicLockFlags();
	}

	void CPxRigidDynamic_setRigidDynamicLockFlag(CPxRigidDynamic* cpp, CPxRigidDynamicLockFlag flag, bool value)
	{
		THIS->setRigidDynamicLockFlag(CHANGE(physx::PxRigidDynamicLockFlag::Enum, flag), value);
	}

	void CPxRigidDynamic_setRigidDynamicLockFlags(CPxRigidDynamic* cpp, CPxRigidDynamicLockFlag flags)
	{
		THIS->setRigidDynamicLockFlags(CHANGE(physx::PxRigidDynamicLockFlag::Enum, flags));
	}

	CPxVec3 CPxRigidDynamic_getLinearVelocity(CPxRigidDynamic* cpp)
	{
		return getVec3Aliased(THIS->getLinearVelocity());
	}

	void CPxRigidDynamic_setLinearVelocity(CPxRigidDynamic* cpp, const CPxVec3* linVel, bool autowake)
	{
		THIS->setLinearVelocity(getVec3Native(*linVel), autowake);
	}

	CPxVec3 CPxRigidDynamic_getAngularVelocity(CPxRigidDynamic* cpp)
	{
		return getVec3Aliased(THIS->getAngularVelocity());
	}

	void CPxRigidDynamic_setAngularVelocity(CPxRigidDynamic* cpp, const CPxVec3* angVel, bool autowake)
	{ 
		THIS->setAngularVelocity(getVec3Native(*angVel), autowake);
	}

	void CPxRigidDynamic_setSolverIterationCounts(CPxRigidDynamic* cpp, CPxU32 minPositionIters, CPxU32 minVelocityIters)
	{
		THIS->setSolverIterationCounts(minPositionIters, minVelocityIters);
	}

	void CPxRigidDynamic_getSolverIterationCounts(CPxRigidDynamic* cpp, CPxU32* minPositionIters, CPxU32* minVelocityIters)
	{
		THIS->getSolverIterationCounts(*minPositionIters, *minVelocityIters);
	}

	CPxReal CPxRigidDynamic_getContactReportThreshold(CPxRigidDynamic* cpp)
	{
		return THIS->getContactReportThreshold();
	}

	void CPxRigidDynamic_setContactReportThreshold(CPxRigidDynamic* cpp, CPxReal threshold)
	{
		THIS->setContactReportThreshold(threshold);
	}

	CPxRigidDynamicGPUIndex CPxRigidDynamic_getGPUIndex(CPxRigidDynamic* cpp)
	{
		return THIS->getGPUIndex();
	}

	const char* CPxRigidDynamic_getConcreteTypeName(CPxRigidDynamic* cpp)
	{
		return THIS->getConcreteTypeName();
	}
}
