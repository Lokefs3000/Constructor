#include "pch.h"
#include "PhysicsLayer.hpp"

#include <common/PxTolerancesScale.h>

#include "AllocatorLayer.hpp"
#include "ErrorLayer.hpp"

PhysicsLayer::PhysicsLayer(const PhysicsLayer::Config& config)
{
	m_allocator = config.allocator;
	m_error = config.error;

	m_foundation = PxCreateFoundation(PX_PHYSICS_VERSION, *config.allocator, *config.error);
	if (m_foundation == nullptr)
		abort();

	m_physics = PxCreatePhysics(PX_PHYSICS_VERSION, *m_foundation, physx::PxTolerancesScale(), config.trackAllocations);
	if (m_physics == nullptr)
		abort();
}

PhysicsLayer::~PhysicsLayer()
{
	m_physics->release();
	m_foundation->release();
}
