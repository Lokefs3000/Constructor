#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Adjust the geometry of the shape.

	\note The type of the passed in geometry must match the geometry type of the shape.
	\note It is not allowed to change the geometry type of a shape.
	\note This function does not guarantee correct/continuous behavior when objects are resting on top of old or new geometry.

	\param[in] geometry New geometry of the shape.

	\see PxGeometry PxGeometryType getGeometryType()
	*/
	INTEROP_API void CPxShape_setGeometry(CPxShape* cpp, CPxGeometry* geometry);

	/**
	\brief Retrieve a reference to the shape's geometry.

	\warning The returned reference has the same lifetime as the PxShape it comes from.

	\return	Reference to internal PxGeometry object.

	\see PxGeometry PxGeometryType getGeometryType() setGeometry()
	*/
	INTEROP_API CPxGeometryHolder CPxShape_getGeometry(CPxShape* cpp);

	/**
	\brief Retrieves the actor which this shape is associated with.

	\return The actor this shape is associated with, if it is an exclusive shape, else NULL

	\see PxRigidStatic, PxRigidDynamic, PxArticulationLink
	*/
	INTEROP_API CPxRigidActor* CPxShape_getActor(CPxShape* cpp);

	/**
	\brief Sets the pose of the shape in actor space, i.e. relative to the actors to which they are attached.

	This transformation is identity by default.

	The local pose is an attribute of the shape, and so will apply to all actors to which the shape is attached.

	<b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.

	<i>Note:</i> Does not automatically update the inertia properties of the owning actor (if applicable); use the
	PhysX extensions method #PxRigidBodyExt::updateMassAndInertia() to do this.

	<b>Default:</b> the identity transform

	\param[in] pose	The new transform from the actor frame to the shape frame. <b>Range:</b> rigid body transform

	\see getLocalPose()
	*/
	INTEROP_API void CPxShape_setLocalPose(CPxShape* cpp, const CPxTransform pose);

	/**
	\brief Retrieves the pose of the shape in actor space, i.e. relative to the actor they are owned by.

	This transformation is identity by default.

	\return Pose of shape relative to the actor's frame.

	\see setLocalPose()
	*/
	INTEROP_API CPxTransform CPxShape_getLocalPose(CPxShape* cpp);

	/**
	\brief Sets the user definable collision filter data.

	<b>Sleeping:</b> Does wake up the actor if the filter data change causes a formerly suppressed
	collision pair to be enabled.

	<b>Default:</b> (0,0,0,0)

	\see getSimulationFilterData()
	*/
	INTEROP_API void CPxShape_setSimulationFilterData(CPxShape* cpp, const CPxFilterData data);

	/**
	\brief Retrieves the shape's collision filter data.

	\see setSimulationFilterData()
	*/
	INTEROP_API CPxFilterData CPxShape_getSimulationFilterData(CPxShape* cpp);

	/**
	\brief Sets the user definable query filter data.

	<b>Default:</b> (0,0,0,0)

	\see getQueryFilterData()
	*/
	INTEROP_API void CPxShape_setQueryFilterData(CPxShape* cpp, const CPxFilterData data);

	/**
	\brief Retrieves the shape's Query filter data.

	\see setQueryFilterData()
	*/
	INTEROP_API CPxFilterData CPxShape_getQueryFilterData(CPxShape* cpp);

	/**
	\brief Assigns material(s) to the shape. Will remove existing materials from the shape.

	<b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.

	\param[in] materials List of material pointers to assign to the shape. See #PxMaterial
	\param[in] materialCount The number of materials provided.

	\see PxPhysics.createMaterial() getMaterials()
	*/
	INTEROP_API void CPxShape_setMaterials(CPxShape* cpp, CPxMaterial** materials, CPxU16 materialCount);

	/**
	\brief Assigns surface deformable material(s) to the shape. Will remove existing materials from the shape.

	<b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.

	\param[in] materials List of material pointers to assign to the shape. See #PxDeformableSurfaceMaterial
	\param[in] materialCount The number of materials provided.

	\see PxPhysics.createDeformableSurfaceMaterial() getDeformableSurfaceMaterials()
	*/
	INTEROP_API void CPxShape_setDeformableSurfaceMaterials(CPxShape* cpp, CPxDeformableSurfaceMaterial** materials, CPxU16 materialCount);

	/**
	\brief Assigns deformable volume material(s) to the shape. Will remove existing materials from the shape.

	<b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.

	\param[in] materials List of material pointers to assign to the shape. See #PxDeformableVolumeMaterial
	\param[in] materialCount The number of materials provided.

	\see PxPhysics.createDeformableVolumeMaterial() getDeformableVolumeMaterials()
	*/
	INTEROP_API void CPxShape_setDeformableVolumeMaterials(CPxShape* cpp, CPxDeformableVolumeMaterial** materials, CPxU16 materialCount);

	/**
	\brief Returns the number of materials assigned to the shape.

	You can use #getMaterials() to retrieve the material pointers.

	\return Number of materials associated with this shape.

	\see PxMaterial getMaterials()
	*/
	INTEROP_API CPxU16 CPxShape_getNbMaterials(CPxShape* cpp);

	/**
	\brief Retrieve all the material pointers associated with the shape.

	You can retrieve the number of material pointers by calling #getNbMaterials()

	Note: The returned data may contain invalid pointers if you release materials using #PxMaterial::release().

	\param[out] userBuffer The buffer to store the material pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first material pointer to be retrieved
	\return Number of material pointers written to the buffer.

	\see PxMaterial getNbMaterials() PxMaterial::release()
	*/
	INTEROP_API CPxU32 CPxShape_getMaterials(CPxShape* cpp, CPxMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Retrieve all the surface deformable material pointers associated with the shape.

	You can retrieve the number of material pointers by calling #getNbMaterials()

	Note: The returned data may contain invalid pointers if you release materials using #PxMaterial::release().

	\param[out] userBuffer The buffer to store the material pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first material pointer to be retrieved
	\return Number of material pointers written to the buffer.

	\see PxDeformableSurfaceMaterial getNbMaterials() PxMaterial::release()
	*/
	INTEROP_API CPxU32 CPxShape_getDeformableSurfaceMaterials(CPxShape* cpp, CPxDeformableSurfaceMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Retrieve all the deformable volume material pointers associated with the shape.

	You can retrieve the number of material pointers by calling #getNbMaterials()

	Note: The returned data may contain invalid pointers if you release materials using #PxMaterial::release().

	\param[out] userBuffer The buffer to store the material pointers.
	\param[in] bufferSize Size of provided user buffer.
	\param[in] startIndex Index of first material pointer to be retrieved
	\return Number of material pointers written to the buffer.

	\see PxDeformableVolumeMaterial getNbMaterials() PxMaterial::release()
	*/
	INTEROP_API CPxU32 CPxShape_getDeformableVolumeMaterials(CPxShape* cpp, CPxDeformableVolumeMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Retrieve material from given triangle index.

	The input index is the internal triangle index as used inside the SDK. This is the index
	returned to users by various SDK functions such as raycasts.

	This function is only useful for triangle meshes or heightfields, which have per-triangle
	materials. For other shapes or SDF triangle meshes, the function returns the single material
	associated with the	shape, regardless of the index.

	\param[in] faceIndex The internal triangle index whose material you want to retrieve.
	\return Material from input triangle

	\note If faceIndex value of 0xFFFFffff is passed as an input for mesh and heightfield shapes, this function will issue a warning and return NULL.
	\note Scene queries set the value of PxQueryHit::faceIndex to 0xFFFFffff whenever it is undefined or does not apply.

	\see PxMaterial getNbMaterials() PxMaterial::release()
	*/
	INTEROP_API CPxBaseMaterial* CPxShape_getMaterialFromInternalFaceIndex(CPxShape* cpp, CPxU32 faceIndex);

	/**
	\brief Sets the contact offset.

	Shapes whose distance is less than the sum of their contactOffset values will generate contacts. The contact offset must be positive and
	greater than the rest offset. Having a contactOffset greater than than the restOffset allows the collision detection system to
	predictively enforce the contact constraint even when the objects are slightly separated. This prevents jitter that would occur
	if the constraint were enforced only when shapes were within the rest distance.

	<b>Default:</b> 0.02f * PxTolerancesScale::length

	<b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.

	\param[in] contactOffset <b>Range:</b> [maximum(0,restOffset), PX_MAX_F32)

	\see getContactOffset PxTolerancesScale setRestOffset
	*/
	INTEROP_API void CPxShape_setContactOffset(CPxShape* cpp, CPxReal contactOffset);

	/**
	\brief Retrieves the contact offset.

	\return The contact offset of the shape.

	\see setContactOffset()
	*/
	INTEROP_API CPxReal CPxShape_getContactOffset(CPxShape* cpp);

	/**
	\brief Sets the rest offset.

	Two shapes will come to rest at a distance equal to the sum of their restOffset values. If the restOffset is 0, they should converge to touching
	exactly.  Having a restOffset greater than zero is useful to have objects slide smoothly, so that they do not get hung up on irregularities of
	each others' surfaces.

	<b>Default:</b> 0.0f

	<b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.

	\param[in] restOffset	<b>Range:</b> (-PX_MAX_F32, contactOffset)

	\see getRestOffset setContactOffset
	*/
	INTEROP_API void CPxShape_setRestOffset(CPxShape* cpp, CPxReal restOffset);

	/**
	\brief Retrieves the rest offset.

	\return The rest offset of the shape.

	\see setRestOffset()
	*/
	INTEROP_API CPxReal CPxShape_getRestOffset(CPxShape* cpp);

	/**
	\brief Sets the density used to interact with fluids.

	To be physically accurate, the density of a rigid body should be computed as its mass divided by its volume. To
	simplify tuning the interaction of fluid and rigid bodies, the density for fluid can differ from the real density. This
	allows to create floating bodies, even if they are supposed to sink with their mass and volume.

	<b>Default:</b> 800.0f

	\param[in] densityForFluid	<b>Range:</b> (0, PX_MAX_F32)

	\see getDensityForFluid
	*/
	INTEROP_API void CPxShape_setDensityForFLuid(CPxShape* cpp, CPxReal densityForFluid);

	/**
	\brief Retrieves the density used to interact with fluids.

	\return The density of the body when interacting with fluid.

	\see setDensityForFluid()
	*/
	INTEROP_API CPxReal CPxShape_getDensityForFluid(CPxShape* cpp);

	/**
	\brief Sets torsional patch radius.

	This defines the radius of the contact patch used to apply torsional friction. If the radius is 0 (and minTorsionalPatchRadius
	is 0 too, see #setMinTorsionalPatchRadius), no torsional friction will be applied. If the radius is > 0, some torsional friction
	will be applied. This is proportional to the penetration depth so, if the shapes are separated or penetration is zero, no
	torsional friction will be applied. It is used to approximate rotational friction introduced by the compression of contacting surfaces.

	\note Will only be active, if the friction patch has a single anchor point only. This is for example the case, if a contact patch
		  has a single contact point.

	\note Only supported in combination with solver type PxSolverType::eTGS.

	<b>Default:</b> 0.0

	\param[in] radius	<b>Range:</b> [0, PX_MAX_F32)
	*/
	INTEROP_API void CPxShape_setTorsionalPatchRadius(CPxShape* cpp, CPxReal radius);

	/**
	\brief Gets torsional patch radius.

	See #setTorsionalPatchRadius for more info.

	\return The torsional patch radius of the shape.
	*/
	INTEROP_API CPxReal CPxShape_getTorsionalPatchRadius(CPxShape* cpp);

	/**
	\brief Sets minimum torsional patch radius.

	This defines the minimum radius of the contact patch used to apply torsional friction. If the radius is 0, the amount of torsional friction
	that will be applied will be entirely dependent on the value of torsionalPatchRadius.

	If the radius is > 0, some torsional friction will be applied regardless of the value of torsionalPatchRadius or the amount of penetration.

	\note Will only be active in certain cases, see #setTorsionalPatchRadius for details.

	<b>Default:</b> 0.0

	\param[in] radius	<b>Range:</b> [0, PX_MAX_F32)
	*/
	INTEROP_API void CPxShape_setMinTorsionalPatchRadius(CPxShape* cpp, CPxReal radius);

	/**
	\brief Gets minimum torsional patch radius.

	See #setMinTorsionalPatchRadius for more info.

	\return The minimum torsional patch radius of the shape.
	*/
	INTEROP_API CPxReal CPxShape_getMinTorsionalPatchRadius(CPxShape* cpp);

	/**
	\brief Returns the GPU shape index.

	\note This function only returns valid results if GPU dynamics is enabled.

	\return The GPU index, or 0xFFFFFFFF if the shape is not attached to a PxActor that is inserted into a PxScene.

	\see PxDirectGPUAPI::evaluateSDFDistances().
	*/
	INTEROP_API CPxShapeGPUIndex CPxShape_getGPUIndex(CPxShape* cpp);

	/**
	\brief Sets shape flags

	<b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.

	\param[in] flag The shape flag to enable/disable. See #PxShapeFlag.
	\param[in] value True to set the flag. False to clear the flag specified in flag.

	<b>Default:</b> PxShapeFlag::eVISUALIZATION | PxShapeFlag::eSIMULATION_SHAPE | PxShapeFlag::eSCENE_QUERY_SHAPE

	\see PxShapeFlag getFlags()
	*/
	INTEROP_API void CPxShape_setFlag(CPxShape* cpp, CPxShapeFlag flag, bool value);

	/**
	\brief Sets shape flags

	\see PxShapeFlag getFlags()
	*/
	INTEROP_API void CPxShape_setFlags(CPxShape* cpp, CPxShapeFlag inFlags);

	/**
	\brief Retrieves shape flags.

	\return The values of the shape flags.

	\see PxShapeFlag setFlag()
	*/
	INTEROP_API CPxShapeFlag CPxShape_getFlags(CPxShape* cpp);

	/**
	\brief Returns true if the shape is exclusive to an actor.

	\see PxPhysics::createShape()
	*/
	INTEROP_API bool CPxShape_isExclusive(CPxShape* cpp);

	/**
	\brief Sets a name string for the object that can be retrieved with #getName().

	This is for debugging and is not used by the SDK.
	The string is not copied by the SDK, only the pointer is stored.

	<b>Default:</b> NULL

	\param[in] name The name string to set the objects name to.

	\see getName()
	*/
	INTEROP_API void CPxShape_setName(CPxShape* cpp, const char* name);

	/**
	\brief retrieves the name string set with setName().
	\return The name associated with the shape.

	\see setName()
	*/
	INTEROP_API const char* CPxShape_getName(CPxShape* cpp);

	INTEROP_API const char* CPxShape_getConcreteTypeName(CPxShape* cpp);
}