#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Destroys the instance it is called on.

	The operation will fail, if there are still modules referencing the foundation object. Release all dependent modules
	prior to calling this method.

	\see PxCreateFoundation()
	*/
	INTEROP_API void CPxFoundation_release(CPxFoundation* cpp);

	/**
	retrieves error callback
	*/
	INTEROP_API CPxErrorCallback* CPxFoundation_getErrorCallback(CPxFoundation* cpp);

	/**
	Sets mask of errors to report.
	*/
	INTEROP_API void CPxFoundation_setErrorLevel(CPxFoundation* cpp, CPxErrorCode mask = CPxErrorCode::eMASK_ALL);

	/**
	Retrieves mask of errors to be reported.
	*/
	INTEROP_API CPxErrorCode CPxFoundation_getErrorLevel(CPxFoundation* cpp);

	/**
	Retrieves the allocator this object was created with.
	*/
	INTEROP_API CPxAllocatorCallback* CPxFoundation_getAllocatorCallback(CPxFoundation* cpp);

	/**
	Retrieves if allocation names are being passed to allocator callback.
	*/
	INTEROP_API bool CPxFoundation_getReportAllocationNames(CPxFoundation* cpp);

	/**
	Set if allocation names are being passed to allocator callback.
	\details Enabled by default in debug and checked build, disabled by default in profile and release build.
	*/
	INTEROP_API void CPxFoundation_setReportAllocationNames(CPxFoundation* cpp, bool value);

	INTEROP_API void CPxFoundation_registerAllocationListener(CPxFoundation* cpp, CPxAllocationListener* listener);
	INTEROP_API void CPxFoundation_deregisterAllocationListener(CPxFoundation* cpp, CPxAllocationListener* listener);
	INTEROP_API void CPxFoundation_registerErrorCallback(CPxFoundation* cpp, CPxErrorCallback* callback);
	INTEROP_API void CPxFoundation_deregisterErrorCallback(CPxFoundation* cpp, CPxErrorCallback* callback);

	/**
	\brief Creates an instance of the foundation class
	
	The foundation class is needed to initialize higher level SDKs. There may be only one instance per process.
	Calling this method after an instance has been created already will result in an error message and NULL will be
	returned.
	
	\param version Version number we are expecting (should be #PX_PHYSICS_VERSION)
	\param allocator User supplied interface for allocating memory(see #PxAllocatorCallback)
	\param errorCallback User supplied interface for reporting errors and displaying messages(see #PxErrorCallback)
	\return Foundation instance on success, NULL if operation failed
	
	\see PxFoundation
	*/
	INTEROP_API CPxFoundation* CPxCreateFoundation(CPxAllocatorCallback* allocator, CPxErrorCallback* errorCallback);

	INTEROP_API void CPxSetFoundationInstance(CPxFoundation* foundation);

	/**
	\brief Retrieves the Foundation SDK after it has been created.
	
	\note The behavior of this method is undefined if the foundation instance has not been created already.
	
	\see PxCreateFoundation(), PxIsFoundationValid()
	*/
	INTEROP_API CPxFoundation* CPxGetFoundation();

	/**
	\brief Similar to PxGetFoundation() except it handles the case if the foundation was not created already.
	\return Pointer to the foundation if an instance is currently available, otherwise null.
	
	\see PxCreateFoundation(), PxGetFoundation()
	*/
	INTEROP_API CPxFoundation* CPxIsFoundationValid();

	/**
	\brief Get the callback that will be used for all profiling.
	*/
	INTEROP_API CPxProfilerCallback* CPxGetProfilerCallback();

	/**
	\brief Set the callback that will be used for all profiling.
	*/
	INTEROP_API void CPxSetProfilerCallback(CPxProfilerCallback* profiler);

	/**
	\brief Get the allocator callback
	*/
	INTEROP_API CPxAllocatorCallback* CPxGetAllocatorCallback();

	/**
	\brief Get the broadcasting allocator callback
	*/
	INTEROP_API CPxAllocatorCallback* CPxGetBroadcastAllocator(bool* reportAllocationNames = nullptr);

	/**
	\brief Get the error callback
	*/
	INTEROP_API CPxErrorCallback* CPxGetErrorCallback();

	/**
	\brief Get the broadcasting error callback
	*/
	INTEROP_API CPxErrorCallback* CPxGetBroadcastError();

	/**
	\brief Get the warn once timestamp
	*/
	INTEROP_API CPxU32 CPxGetWarnOnceTimeStamp();

	/**
	\brief Decrement the ref count of PxFoundation
	*/
	INTEROP_API void CPxDecFoundationRefCount();

	/**
	\brief Increment the ref count of PxFoundation
	*/
	INTEROP_API void CPxIncFoundationRefCount();
}