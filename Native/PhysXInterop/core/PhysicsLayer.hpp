#pragma once

#include <foundation/PxFoundation.h>
#include <PxPhysics.h>

class AllocatorLayer;
class ErrorLayer;

class PhysicsLayer
{
public:
	struct Config
	{
		bool trackAllocations;

		AllocatorLayer* allocator;
		ErrorLayer* error;
	};
public:
	PhysicsLayer(const Config& config);
	~PhysicsLayer();

private:
	AllocatorLayer* m_allocator;
	ErrorLayer* m_error;

	physx::PxFoundation* m_foundation;
	physx::PxPhysics* m_physics;
};

extern "C"
{
	__declspec(dllexport) PhysicsLayer* core_CreatePhysics(const PhysicsLayer::Config* config)
		{}
}