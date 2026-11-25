#include "pch.h"
#include "shared.hpp"

#include "bridge_types.hpp"
#include "bridge_conversions.hpp"

#include "public/bridge_PxShape.hpp"

#include <PxPhysicsAPI.h>

extern "C"
{
#define THIS GET(physx::PxShape, cpp)

	void CPxShape_setGeometry(CPxShape* cpp, CPxGeometry* geometry)
	{
	
	}

	CPxGeometryHolder CPxShape_getGeometry(CPxShape* cpp)
	{
		physx::PxGeometryHolder holder(THIS->getGeometry());
		switch (holder.getType())
		{
			case physx::PxGeometryType::eSPHERE:
				{
					const physx::PxSphereGeometry& realGeometry = holder.sphere();
					return CPxGeometryHolder{
						.sphere = {
							.base = { .type = CPxGeometryType::eSPHERE },
							.radius = realGeometry.radius
						}
					};
				}
			case physx::PxGeometryType::ePLANE:
				{
					const physx::PxPlaneGeometry& realGeometry = holder.plane();
					return CPxGeometryHolder{
						.plane = {
							.base = { .type = CPxGeometryType::ePLANE },
						}
					};
				}
			case physx::PxGeometryType::eCAPSULE:
				{
					const physx::PxCapsuleGeometry& realGeometry = holder.capsule();
					return CPxGeometryHolder{
						.capsule = {
							.base = { .type = CPxGeometryType::eCAPSULE },
							.radius = realGeometry.radius,
							.halfHeight = realGeometry.halfHeight
						}
					};
				}
			case physx::PxGeometryType::eBOX:
				{
					const physx::PxBoxGeometry& realGeometry = holder.box();
					return CPxGeometryHolder{
						.box = {
							.base = { .type = CPxGeometryType::eBOX },
							.halfExtents = getVec3Aliased(realGeometry.halfExtents)
						}
					};
				}
			case physx::PxGeometryType::eCONVEXCORE:
				{
					abort();
				}
			case physx::PxGeometryType::eCONVEXMESH:
				{
					const physx::PxConvexMeshGeometry& realGeometry = holder.convexMesh();
					return CPxGeometryHolder{
						.convexMesh = {
							.base = { .type = CPxGeometryType::eCONVEXMESH },
							.scale = getMeshScaleAliased(realGeometry.scale),
							.convexMesh = realGeometry.convexMesh,
							.meshFlags = (CPxConvexMeshGeometryFlag)(physx::PxU8)realGeometry.meshFlags
						}
					};
				}
			case physx::PxGeometryType::ePARTICLESYSTEM:
				{
					const physx::PxParticleSystemGeometry& realGeometry = holder.particleSystem();
					return CPxGeometryHolder{
						.particleSystem = {
							.base = { .type = CPxGeometryType::ePARTICLESYSTEM },
						}
					};
				}
			case physx::PxGeometryType::eTETRAHEDRONMESH:
				{
					const physx::PxTetrahedronMeshGeometry& realGeometry = holder.tetMesh();
					return CPxGeometryHolder{
						.tetrahedronMesh = {
							.base = { .type = CPxGeometryType::eTETRAHEDRONMESH },
							.tetrahedronMesh = realGeometry.tetrahedronMesh
						}
					};
				}
			case physx::PxGeometryType::eTRIANGLEMESH:
				{
					const physx::PxTriangleMeshGeometry& realGeometry = holder.triangleMesh();
					return CPxGeometryHolder{
						.triangleMesh = {
							.base = { .type = CPxGeometryType::eTRIANGLEMESH },
							.scale = getMeshScaleAliased(realGeometry.scale),
							.meshFlags = (CPxMeshGeometryFlag)(physx::PxU8)realGeometry.meshFlags,
							.triangleMesh = realGeometry.triangleMesh
						}
					};
				}
			case physx::PxGeometryType::eHEIGHTFIELD:
				{
					const physx::PxHeightFieldGeometry& realGeometry = holder.heightField();
					return CPxGeometryHolder{
						.heightField = {
							.base = { .type = CPxGeometryType::eSPHERE },
							.heightField = realGeometry.heightField,
							.heightScale = realGeometry.heightScale,
							.rowScale = realGeometry.rowScale,
							.columnScale = realGeometry.columnScale,
							.heightFieldFlags = (CPxMeshGeometryFlag)(physx::PxU8)realGeometry.heightFieldFlags
						}
					};
				}
			case physx::PxGeometryType::eCUSTOM:
				{
					abort();
				}
			default:
				{
					abort();
				}
		}
	}

	CPxRigidActor* CPxShape_getActor(CPxShape* cpp)
	{
		return THIS->getActor();
	}

	void CPxShape_setLocalPose(CPxShape* cpp, const CPxTransform pose)
	{
		THIS->setLocalPose(getTransformNative(pose));
	}

	CPxTransform CPxShape_getLocalPose(CPxShape* cpp)
	{
		return getTransformAliased(THIS->getLocalPose());
	}

	void CPxShape_setSimulationFilterData(CPxShape* cpp, const CPxFilterData data)
	{
		THIS->setSimulationFilterData(getFilterDataNative(data));
	}

	CPxFilterData CPxShape_getSimulationFilterData(CPxShape* cpp)
	{
		return getFilterDataAliased(THIS->getSimulationFilterData());
	}

	void CPxShape_setQueryFilterData(CPxShape* cpp, const CPxFilterData data)
	{
		THIS->setQueryFilterData(getFilterDataNative(data));
	}

	CPxFilterData CPxShape_getQueryFilterData(CPxShape* cpp)
	{
		return getFilterDataAliased(THIS->getQueryFilterData());
	}

	void CPxShape_setMaterials(CPxShape* cpp, CPxMaterial** materials, CPxU16 materialCount)
	{
		THIS->setMaterials((physx::PxMaterial* const*)materials, materialCount);
	}

	void CPxShape_setDeformableSurfaceMaterials(CPxShape* cpp, CPxDeformableSurfaceMaterial** materials, CPxU16 materialCount)
	{
		THIS->setDeformableSurfaceMaterials((physx::PxDeformableSurfaceMaterial* const*)materials, materialCount);
	}

	void CPxShape_setDeformableVolumeMaterials(CPxShape* cpp, CPxDeformableVolumeMaterial** materials, CPxU16 materialCount)
	{
		THIS->setDeformableVolumeMaterials((physx::PxDeformableVolumeMaterial* const*)materials, materialCount);
	}

	CPxU16 CPxShape_getNbMaterials(CPxShape* cpp)
	{
		return THIS->getNbMaterials();
	}

	CPxU32 CPxShape_getMaterials(CPxShape* cpp, CPxMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getMaterials((physx::PxMaterial**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxShape_getDeformableSurfaceMaterials(CPxShape* cpp, CPxDeformableSurfaceMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getDeformableSurfaceMaterials((physx::PxDeformableSurfaceMaterial**)userBuffer, bufferSize, startIndex);
	}

	CPxU32 CPxShape_getDeformableVolumeMaterials(CPxShape* cpp, CPxDeformableVolumeMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex)
	{
		return THIS->getDeformableVolumeMaterials((physx::PxDeformableVolumeMaterial**)userBuffer, bufferSize, startIndex);
	}

	CPxBaseMaterial* CPxShape_getMaterialFromInternalFaceIndex(CPxShape* cpp, CPxU32 faceIndex)
	{
		return THIS->getMaterialFromInternalFaceIndex(faceIndex);
	}

	void CPxShape_setContactOffset(CPxShape* cpp, CPxReal contactOffset)
	{
		THIS->setContactOffset(contactOffset);
	}

	CPxReal CPxShape_getContactOffset(CPxShape* cpp)
	{
		return THIS->getContactOffset();
	}

	void CPxShape_setRestOffset(CPxShape* cpp, CPxReal restOffset)
	{
		THIS->setRestOffset(restOffset);
	}

	CPxReal CPxShape_getRestOffset(CPxShape* cpp)
	{
		return THIS->getRestOffset();
	}

	void CPxShape_setDensityForFLuid(CPxShape* cpp, CPxReal densityForFluid)
	{
		THIS->setDensityForFluid(densityForFluid);
	}

	CPxReal CPxShape_getDensityForFluid(CPxShape* cpp)
	{
		return THIS->getDensityForFluid();
	}

	void CPxShape_setTorsionalPatchRadius(CPxShape* cpp, CPxReal radius)
	{
		THIS->setTorsionalPatchRadius(radius);
	}

	CPxReal CPxShape_getTorsionalPatchRadius(CPxShape* cpp)
	{
		return THIS->getTorsionalPatchRadius();
	}

	void CPxShape_setMinTorsionalPatchRadius(CPxShape* cpp, CPxReal radius)
	{
		THIS->setMinTorsionalPatchRadius(radius);
	}

	CPxReal CPxShape_getMinTorsionalPatchRadius(CPxShape* cpp)
	{
		return THIS->getMinTorsionalPatchRadius();
	}

	CPxShapeGPUIndex CPxShape_getGPUIndex(CPxShape* cpp)
	{
		return THIS->getGPUIndex();
	}

	void CPxShape_setFlag(CPxShape* cpp, CPxShapeFlag flag, bool value)
	{
		THIS->setFlag(CHANGE(physx::PxShapeFlag::Enum, flag), value);
	}

	void CPxShape_setFlags(CPxShape* cpp, CPxShapeFlag inFlags)
	{
		THIS->setFlags(CHANGE(physx::PxShapeFlag::Enum, inFlags));
	}

	CPxShapeFlag CPxShape_getFlags(CPxShape* cpp)
	{
		return (CPxShapeFlag)(CPxU8)THIS->getFlags();
	}

	bool CPxShape_isExclusive(CPxShape* cpp)
	{
		return THIS->isExclusive();
	}

	void CPxShape_setName(CPxShape* cpp, const char* name)
	{
		THIS->setName(name);
	}

	const char* CPxShape_getName(CPxShape* cpp)
	{
		return THIS->getName();
	}

	const char* CPxShape_getConcreteTypeName(CPxShape* cpp)
	{
		return THIS->getConcreteTypeName();
	}
}
