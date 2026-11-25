#pragma once

#include "Config.hpp"

#include <PxPhysicsAPI.h>

class PhysicsScene
{
public:
	struct MethodTable;

public:
	PhysicsScene(physx::PxScene* scene);
	~PhysicsScene();

	///////////////////////////////////////////////

	struct MethodTable
	{

	};
private:
	MethodTable m_mtable;

	physx::PxScene* m_scene;
};
