#include "pch.h"
#include "shared.hpp"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxFoundation.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxFoundation, cpp)

	void CPxFoundation_release(CPxFoundation* cpp)
	{
		THIS->release();
	}

	CPxErrorCallback* CPxFoundation_getErrorCallback(CPxFoundation* cpp)
	{
		return &THIS->getErrorCallback();
	}

	void CPxFoundation_setErrorLevel(CPxFoundation* cpp, CPxErrorCode mask)
	{
		THIS->setErrorLevel(CHANGE(physx::PxErrorCode::Enum, mask));
	}

	CPxErrorCode CPxFoundation_getErrorLevel(CPxFoundation* cpp)
	{
		return (CPxErrorCode)THIS->getErrorLevel();
	}

	CPxAllocatorCallback* CPxFoundation_getAllocatorCallback(CPxFoundation* cpp)
	{
		return &THIS->getAllocatorCallback();
	}

	bool CPxFoundation_getReportAllocationNames(CPxFoundation* cpp)
	{
		return THIS->getReportAllocationNames();
	}

	void CPxFoundation_setReportAllocationNames(CPxFoundation* cpp, bool value)
	{
		THIS->setReportAllocationNames(value);
	}

	void CPxFoundation_registerAllocationListener(CPxFoundation* cpp, CPxAllocationListener* listener)
	{
		THIS->registerAllocationListener(*GET(physx::PxAllocationListener, listener));
	}

	void CPxFoundation_deregisterAllocationListener(CPxFoundation* cpp, CPxAllocationListener* listener)
	{
		THIS->deregisterAllocationListener(*GET(physx::PxAllocationListener, listener));
	}

	void CPxFoundation_registerErrorCallback(CPxFoundation* cpp, CPxErrorCallback* callback)
	{
		THIS->registerErrorCallback(*GET(physx::PxErrorCallback, callback));
	}

	void CPxFoundation_deregisterErrorCallback(CPxFoundation* cpp, CPxErrorCallback* callback)
	{
		THIS->deregisterErrorCallback(*GET(physx::PxErrorCallback, callback));
	}

	CPxFoundation* CPxCreateFoundation(CPxAllocatorCallback* allocator, CPxErrorCallback* errorCallback)
	{
		return PxCreateFoundation(PX_PHYSICS_VERSION, *GET(physx::PxAllocatorCallback, allocator), *GET(physx::PxErrorCallback, errorCallback));
	}

	void CPxSetFoundationInstance(CPxFoundation* foundation)
	{
		PxSetFoundationInstance(*GET(physx::PxFoundation, foundation));
	}

	CPxFoundation* CPxGetFoundation()
	{
		return &PxGetFoundation();
	}

	CPxFoundation*CPxIsFoundationValid()
	{
		return PxIsFoundationValid();
	}

	CPxProfilerCallback* CPxGetProfilerCallback()
	{
		return PxGetProfilerCallback();
	}

	void CPxSetProfilerCallback(CPxProfilerCallback* profiler)
	{
		PxSetProfilerCallback(GET(physx::PxProfilerCallback, profiler));
	}

	CPxAllocatorCallback* CPxGetAllocatorCallback()
	{
		return PxGetAllocatorCallback();
	}

	CPxAllocatorCallback* CPxGetBroadcastAllocator(bool* reportAllocationNames)
	{
		return PxGetBroadcastAllocator(reportAllocationNames);
	}

	CPxErrorCallback* CPxGetErrorCallback()
	{
		return PxGetErrorCallback();
	}

	CPxErrorCallback* CPxGetBroadcastError()
	{
		return PxGetBroadcastAllocator();
	}

	CPxU32 CPxGetWarnOnceTimeStamp()
	{
		return PxGetWarnOnceTimeStamp();
	}

	void CPxDecFoundationRefCount()
	{
		PxDecFoundationRefCount();
	}

	void CPxIncFoundationRefCount()
	{
		PxIncFoundationRefCount();
	}
}
