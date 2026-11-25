#include "pch.h"
#include "BoxGeometry.hpp"

BoxGeometry::BoxGeometry(const physx::PxBoxGeometry& geometry, physx::PxShape* shape)
{
	m_mtable = BoxGeometry::MethodTable{
		.updateGeometry = &BoxGeometry::updateGeometry,
		.setContactOffset = &BoxGeometry::setContactOffset,
		.getContactOffset = &BoxGeometry::getContactOffset,
		.setRestOffset = &BoxGeometry::setRestOffset,
		.getRestOffset = &BoxGeometry::getRestOffset,
		.setLocalPose = &BoxGeometry::setLocalPose,
		.getLocalPose = &BoxGeometry::getLocalPose,
		.setName = &BoxGeometry::setName,
		.setHalfExtents = &BoxGeometry::setHalfExtents
	};

	m_geometry = geometry;
	m_shape = shape;

	shape->userData = this;
}

BoxGeometry::~BoxGeometry()
{
	m_shape->userData = nullptr;

	m_geometry = {};
	m_shape->release();
	m_shape = nullptr;
}

void BoxGeometry::updateGeometry()
{
	m_shape->setGeometry(m_geometry);
}

void BoxGeometry::setContactOffset(float contactOffset)
{
	m_shape->setContactOffset(contactOffset);
}

float BoxGeometry::getContactOffset() const
{
	return m_shape->getContactOffset();
}

void BoxGeometry::setRestOffset(float restOffset)
{
	m_shape->setRestOffset(restOffset);
}

float BoxGeometry::getRestOffset() const
{
	return m_shape->getRestOffset();
}

void BoxGeometry::setLocalPose(const physx::PxTransform& pose)
{
	m_shape->setLocalPose(pose);
}

physx::PxTransform BoxGeometry::getLocalPose() const
{
	return m_shape->getLocalPose();
}

void BoxGeometry::setName(const char* name)
{
	m_shape->setName(name);
}

void BoxGeometry::setHalfExtents(const physx::PxVec3& halfExtents)
{
	m_geometry.halfExtents = halfExtents;
}
