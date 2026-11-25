#include "pch.h"
#include "AllocatorLayer.hpp"

AllocatorLayer::AllocatorLayer(UserAllocatePFN allocateFn, UserDeallocatePFN deallocateFn)
{
	m_allocateFn = allocateFn;
	m_deallocateFn = deallocateFn;
}

AllocatorLayer::~AllocatorLayer()
{
	m_allocateFn = nullptr;
	m_deallocateFn = nullptr;
}

void* AllocatorLayer::allocate(size_t size, const char* typeName, const char* filename, int line)
{
	return m_allocateFn(size, typeName, filename, line);
}

void AllocatorLayer::deallocate(void* ptr)
{
	m_deallocateFn(ptr);
}

extern "C"
{
	__declspec(dllexport) AllocatorLayer* core_CreateAllocator(UserAllocatePFN* alloc, UserDeallocatePFN* dealloc)
	{
		return new AllocatorLayer(alloc, dealloc);
	}

	__declspec(dllexport) void core_DestroyAllocator(AllocatorLayer* layer)
	{
		delete layer;
	}
}