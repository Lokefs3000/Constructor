#pragma once

#include <foundation/PxAllocatorCallback.h>

typedef void* UserAllocatePFN(size_t numBytes, const char* typeName, const char* filename, int line);
typedef void UserDeallocatePFN(void* ptr);

class AllocatorLayer : public physx::PxAllocatorCallback
{
public:
	AllocatorLayer(UserAllocatePFN allocateFn, UserDeallocatePFN deallocateFn);
	~AllocatorLayer();

	void* allocate(size_t size, const char* typeName, const char* filename, int line) override;
	void deallocate(void* ptr) override;
private:
	UserAllocatePFN* m_allocateFn;
	UserDeallocatePFN* m_deallocateFn;
};

extern "C"
{
	__declspec(dllexport) AllocatorLayer* core_CreateAllocator(UserAllocatePFN* alloc, UserDeallocatePFN* dealloc);
	__declspec(dllexport) void core_DestroyAllocator(AllocatorLayer* layer);
}