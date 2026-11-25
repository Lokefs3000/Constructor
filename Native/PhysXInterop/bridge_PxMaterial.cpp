#include "pch.h"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxMaterial.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxMaterial, cpp)

	void CPxMaterial_setDynamicFriction(CPxMaterial* cpp, CPxReal coef)
	{
		THIS->setDynamicFriction(coef);
	}

	CPxReal CPxMaterial_getDynamicFriction(CPxMaterial* cpp)
	{
		return THIS->getDynamicFriction();
	}

	void CPxMaterial_setStaticFriction(CPxMaterial* cpp, CPxReal coef)
	{
		THIS->setStaticFriction(coef);
	}

	CPxReal CPxMateriak_getStaticFriction(CPxMaterial* cpp)
	{
		return THIS->getStaticFriction();
	}

	void CPxMaterial_setRestitution(CPxMaterial* cpp, CPxReal rest)
	{
		THIS->setRestitution(rest);
	}

	CPxReal CPxMaterial_getRestitution(CPxMaterial* cpp)
	{
		return THIS->getRestitution();
	}

	void CPxMaterial_setDamping(CPxMaterial* cpp, CPxReal damping)
	{
		THIS->setDamping(damping);
	}

	CPxReal CPxMaterial_getDamping(CPxMaterial* cpp)
	{
		return THIS->getDamping();
	}

	void CPxMaterial_setFlag(CPxMaterial* cpp, CPxMaterialFlag flag, bool b)
	{
		THIS->setFlag(CHANGE(physx::PxMaterialFlag::Enum, flag), b);
	}

	void CPxMaterial_setFlags(CPxMaterial* cpp, CPxMaterialFlag flags)
	{
		THIS->setFlags(CHANGE(physx::PxMaterialFlag::Enum, flags));
	}

	CPxMaterialFlag CPxMaterial_getFlags(CPxMaterial* cpp)
	{
		return (CPxMaterialFlag)(physx::PxU16)THIS->getFlags();
	}

	void CPxMaterial_setFrictionCombineMode(CPxMaterial* cpp, CPxCombineMode combMode)
	{
		THIS->setFrictionCombineMode(CHANGE(physx::PxCombineMode::Enum, combMode));
	}

	CPxCombineMode CPxMaterial_getFrictionCombineMode(CPxMaterial* cpp)
	{
		return (CPxCombineMode)THIS->getFrictionCombineMode();
	}

	void CPxMaterial_setRestitutionCombineMode(CPxMaterial* cpp, CPxCombineMode combMode)
	{
		THIS->setRestitutionCombineMode(CHANGE(physx::PxCombineMode::Enum, combMode));
	}

	CPxCombineMode CPxMaterial_getRestitutionCombineMode(CPxMaterial* cpp)
	{
		return (CPxCombineMode)THIS->getRestitutionCombineMode();
	}

	void CPxMaterial_setDampingCombineMode(CPxMaterial* cpp, CPxCombineMode combMode)
	{
		THIS->setDampingCombineMode(CHANGE(physx::PxCombineMode::Enum, combMode));
	}

	CPxCombineMode CPxMaterial_getDampingCombineMode(CPxMaterial* cpp)
	{
		return (CPxCombineMode)THIS->getDampingCombineMode();
	}

	const char* CPxMaterial_getConcreteTypeName(CPxRigidStatic* cpp)
	{
		return THIS->getConcreteTypeName();
	}
}
