#pragma once

#include "Config.hpp"

#include <PxPhysicsAPI.h>

class BoxGeometry
{
public:
	struct MethodTable;

public:
	BoxGeometry(const physx::PxBoxGeometry& geometry, physx::PxShape* shape);
	~BoxGeometry();

	void updateGeometry();

	void setContactOffset(float contactOffset);
	float getContactOffset() const;

	void setRestOffset(float restOffset);
	float getRestOffset() const;

	void setLocalPose(const physx::PxTransform& pose);
	physx::PxTransform getLocalPose() const;

	void setName(const char* name);

	void setHalfExtents(const physx::PxVec3& halfExtents);

	///////////////////////////////////////////////

	struct MethodTable
	{
		MTABLE_FUNCTION_VOID(BoxGeometry, updateGeometry);
		MTABLE_FUNCTION_VOID(BoxGeometry, setContactOffset, float);
		MTABLE_FUNCTION_WITH(float, BoxGeometry, getContactOffset) const;
		MTABLE_FUNCTION_VOID(BoxGeometry, setRestOffset, float);
		MTABLE_FUNCTION_WITH(float, BoxGeometry, getRestOffset) const;
		MTABLE_FUNCTION_VOID(BoxGeometry, setLocalPose, const physx::PxTransform&);
		MTABLE_FUNCTION_WITH(physx::PxTransform, BoxGeometry, getLocalPose) const;
		MTABLE_FUNCTION_VOID(BoxGeometry, setName, const char*);
		MTABLE_FUNCTION_VOID(BoxGeometry, setHalfExtents, const physx::PxVec3&);
	};
private:
	MethodTable m_mtable;

	physx::PxBoxGeometry m_geometry;
	physx::PxShape* m_shape;
};
