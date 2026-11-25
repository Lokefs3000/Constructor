#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxActor.hpp"

#include <PxPhysicsAPI.h>

typedef void* (AllocateDelegate)(size_t size, const char* typeName, const char* filename, int line);
typedef void (DeallocateDelegate)(void* ptr);

class ProxyAllocatorCallback : physx::PxAllocatorCallback
{
public:
    ProxyAllocatorCallback(AllocateDelegate* allocate, DeallocateDelegate* deallocate)
    {
        m_allocate = allocate;
        m_deallocate = deallocate;
    }

    void* allocate(size_t size, const char* typeName, const char* filename, int line) override
    {
        return m_allocate(size, typeName, filename, line);
    }

    void deallocate(void* ptr) override
    {
        return m_deallocate(ptr);
    }

private:
    AllocateDelegate* m_allocate;
    DeallocateDelegate* m_deallocate;
};

typedef void (ReportErrorDelegate)(physx::PxErrorCode::Enum code, const char* message, const char* file, int line);

class ProxyErrorCallback : physx::PxErrorCallback
{
public:
    ProxyErrorCallback(ReportErrorDelegate* reportError)
    {
        m_reportError = reportError;
    }

    void reportError(physx::PxErrorCode::Enum code, const char* message, const char* file, int line) override
    {
        m_reportError(code, message, file, line);
    }

private:
    ReportErrorDelegate* m_reportError;
};

extern "C"
{
    INTEROP_API CPxAllocatorCallback* CCreateAllocatorCallback(void* pfnAllocate, void* pfnDeallocate)
    {
        return new ProxyAllocatorCallback((AllocateDelegate*)pfnAllocate, (DeallocateDelegate*)pfnDeallocate);
    }

    INTEROP_API void CDestroyAllocatorCallback(CPxAllocatorCallback* ptr)
    {
        delete ptr;
    }

    INTEROP_API CPxErrorCallback* CCreateErrorCallback(void* pfnErrorCallback)
    {
        return new ProxyErrorCallback((ReportErrorDelegate*)pfnErrorCallback);
    }

    INTEROP_API void CDestroyErrorCallback(CPxErrorCallback* ptr)
    {
        delete ptr;
    }
}
