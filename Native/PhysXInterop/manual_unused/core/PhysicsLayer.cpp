#include "pch.h"
#include "PhysicsLayer.hpp"

#include <common/PxTolerancesScale.h>

#include "AllocatorLayer.hpp"
#include "ErrorLayer.hpp"

#include "world/PhysicsScene.hpp"

#include "geometry/BoxGeometry.hpp"

PhysicsLayer::PhysicsLayer(const PhysicsLayer::Config& config)
{
#ifdef WIN32
	// Stupid ass windows loads all but this DLL fine
	::LoadLibrary(TEXT("runtimes/win-x64/native/PhysXCommon_64.dll"));
#endif

	m_allocator = config.allocator;
	m_error = config.error;
	
	m_foundation = PxCreateFoundation(PX_PHYSICS_VERSION, *config.allocator, *config.error);
	if (m_foundation == nullptr)
		abort();

	m_physics = PxCreatePhysics(PX_PHYSICS_VERSION, *m_foundation, physx::PxTolerancesScale(), config.trackAllocations);
	if (m_physics == nullptr)
		abort();

	// if (!PxInitExtensions(*m_physics, nullptr))
	// 	abort();

	m_vtable = PhysicsLayer::MethodTable{
		.getPhysXVersion = &PhysicsLayer::getPhysXVersion,
		.createBoxGeometry = &PhysicsLayer::createBoxGeometry
	};
}

PhysicsLayer::~PhysicsLayer()
{
	// PxCloseExtensions();
	
	m_physics->release();
	m_foundation->release();
}

const char* PhysicsLayer::getPhysXVersion()
{
	return "5.12.0";
}

PhysicsScene* PhysicsLayer::createScene()
{
	physx::PxSceneDesc desc = physx::PxSceneDesc(physx::PxTolerancesScale());
	desc.setToDefault(physx::PxTolerancesScale());

	physx::PxScene* scene = m_physics->createScene(desc);

	return new PhysicsScene(scene);
}

BoxGeometry* PhysicsLayer::createBoxGeometry(const physx::PxVec3& halfExtents)
{
	physx::PxBoxGeometry box(halfExtents);
	physx::PxShape* shape = m_physics->createShape(
		box, 
		(physx::PxMaterial*const*)nullptr, 
		0, 
		true, 
		physx::PxShapeFlag::eVISUALIZATION | physx::PxShapeFlag::eSCENE_QUERY_SHAPE | physx::PxShapeFlag::eSIMULATION_SHAPE);

	return new BoxGeometry(box, shape);
}

extern "C"
{
	INTEROP_API PhysicsLayer* core_CreatePhysics(const PhysicsLayer::Config* config)
	{
		return new PhysicsLayer(*config);
	}

	INTEROP_API void core_DestroyPhysics(PhysicsLayer* object)
	{
		delete object;
	}

	INTEROP_API const char* phys_getPhysXVersion(PhysicsLayer* object)
	{
		return object->getPhysXVersion();
	}
}
