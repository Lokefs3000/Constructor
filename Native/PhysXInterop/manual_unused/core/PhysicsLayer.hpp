#pragma once

#include "Config.hpp"

#include <foundation/PxFoundation.h>
#include <PxPhysics.h>

class AllocatorLayer;
class ErrorLayer;

class PhysicsScene;

class BoxGeometry;

class PhysicsLayer
{
public:
	struct MethodTable;

	struct Config
	{
		bool trackAllocations;

		AllocatorLayer* allocator;
		ErrorLayer* error;
	};
public:
	PhysicsLayer(const Config& config);
	~PhysicsLayer();

	const char* getPhysXVersion();

	PhysicsScene* createScene();

	BoxGeometry* createBoxGeometry(const physx::PxVec3& halfExtents);

	///////////////////////////////////////////////

	struct MethodTable
	{
		MTABLE_FUNCTION_WITH(const char*, PhysicsLayer, getPhysXVersion);
		MTABLE_FUNCTION_WITH(BoxGeometry*, PhysicsLayer, createBoxGeometry, const physx::PxVec3&);
	};
private:
	MethodTable m_vtable;

	AllocatorLayer* m_allocator;
	ErrorLayer* m_error;

	physx::PxFoundation* m_foundation;
	physx::PxPhysics* m_physics;
};

extern "C"
{
	INTEROP_API PhysicsLayer* core_CreatePhysics(const PhysicsLayer::Config* config);
	INTEROP_API void core_DestroyPhysics(PhysicsLayer* object);

	INTEROP_API const char* phys_getPhysXVersion(PhysicsLayer* object);
}