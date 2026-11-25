#include "pch.h"
#include "PhysicsScene.hpp"

PhysicsScene::PhysicsScene(physx::PxScene* scene)
{
	m_mtable = PhysicsScene::MethodTable{

	};

	m_scene = scene;
	scene->userData = this;
}

PhysicsScene::~PhysicsScene()
{
	m_scene->userData = nullptr;
	m_scene->release();
}
