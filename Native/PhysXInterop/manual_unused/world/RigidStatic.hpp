#pragma once

#include "Config.hpp"

#include <PxPhysicsAPI.h>

class RigidStatic
{
public:
	struct MethodTable;

public:
	RigidStatic(physx::PxScene* scene);
	~RigidStatic();

	physx::PxTransform getGlobalPose();
	void setGlobalPose(const physx::PxTransform& pose, bool autowake);

	void attachShape()

	///////////////////////////////////////////////

	struct MethodTable
	{

	};
private:
	MethodTable m_mtable;

	physx::PxRigidStatic* m_scene;
};
