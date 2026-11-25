#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	INTEROP_API CPxAllocatorCallback* CCreateAllocatorCallback(void* pfnAllocate, void* pfnDeallocate);
	INTEROP_API void CDestroyAllocatorCallback(CPxAllocatorCallback* ptr);

	INTEROP_API CPxErrorCallback* CCreateErrorCallback(void* pfnErrorCallback);
	INTEROP_API void CDestroyErrorCallback(CPxErrorCallback* ptr);
}