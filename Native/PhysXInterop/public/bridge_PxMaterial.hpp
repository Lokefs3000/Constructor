#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Sets the coefficient of dynamic friction.
	
	The coefficient of dynamic friction should be in [0, PX_MAX_F32). If set to greater than staticFriction, the effective value of staticFriction will be increased to match.

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in] coef Coefficient of dynamic friction. <b>Range:</b> [0, PX_MAX_F32)

	\see getDynamicFriction()
	*/
	INTEROP_API void CPxMaterial_setDynamicFriction(CPxMaterial* cpp, CPxReal coef);

	/**
	\brief Retrieves the DynamicFriction value.

	\return The coefficient of dynamic friction.

	\see setDynamicFriction
	*/
	INTEROP_API CPxReal CPxMaterial_getDynamicFriction(CPxMaterial* cpp);

	/**
	\brief Sets the coefficient of static friction

	The coefficient of static friction should be in the range [0, PX_MAX_F32)

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in] coef Coefficient of static friction. <b>Range:</b> [0, PX_MAX_F32)

	\see getStaticFriction()
	*/
	INTEROP_API void CPxMaterial_setStaticFriction(CPxMaterial* cpp, CPxReal coef);

	/**
	\brief Retrieves the coefficient of static friction.
	\return The coefficient of static friction.

	\see setStaticFriction
	*/
	INTEROP_API CPxReal CPxMaterial_getStaticFriction(CPxMaterial* cpp);

	/**
	\brief Sets the coefficient of restitution or the spring stiffness for compliant contact

	A coefficient of 0 makes the object bounce as little as possible, higher values up to 1.0 result in more bounce.
	If a negative value is provided it is interpreted as stiffness term for an implicit spring
	simulated at the contact site, with the spring positional error defined by
	the contact separation value. Higher stiffness terms produce stiffer springs that behave more like a rigid contact.

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in] rest Coefficient of restitution / negative spring stiffness <b>Range:</b> (-INF,1]

	\see getRestitution()  setDamping()
	*/
	INTEROP_API void CPxMaterial_setRestitution(CPxMaterial* cpp, CPxReal rest);

	/**
	\brief Retrieves the coefficient of restitution.

	See #setRestitution.

	\return The coefficient of restitution.

	\see setRestitution()
	*/
	INTEROP_API CPxReal CPxMaterial_getRestitution(CPxMaterial* cpp);

	/**
	\brief Sets the coefficient of damping

	This property only affects the simulation if compliant contact mode is enabled, i.e., a negative restitution value is set.
	Damping works together with spring stiffness. Spring stiffness corrects positional error while
	damping resists relative velocity. Setting a high damping coefficient can produce spongy contacts.

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in] damping Coefficient of damping. <b>Range:</b> [0,INF)

	\see getDamping()
	*/
	INTEROP_API void CPxMaterial_setDamping(CPxMaterial* cpp, CPxReal damping);

	/**
	\brief Retrieves the coefficient of damping.

	See #setDamping.

	\return The coefficient of damping.

	\see setDamping()
	*/
	INTEROP_API CPxReal CPxMaterial_getDamping(CPxMaterial* cpp);

	/**
	\brief Raises or clears a particular material flag.

	See the list of flags #PxMaterialFlag

	<b>Default:</b> No flag raised.

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in]	flag	The PxMaterial flag to raise(set) or clear.
	\param[in]	b		New state of the flag

	\see getFlags() setFlags() PxMaterialFlag
	*/
	INTEROP_API void CPxMaterial_setFlag(CPxMaterial* cpp, CPxMaterialFlag flag, bool b);

	/**
	\brief sets all the material flags.

	See the list of flags #PxMaterialFlag

	<b>Default:</b> No flag raised.

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in]	flags	All PxMaterial flags

	\see getFlags() setFlag() PxMaterialFlag
	*/
	INTEROP_API void CPxMaterial_setFlags(CPxMaterial* cpp, CPxMaterialFlag flags);

	/**
	\brief Retrieves the flags. See #PxMaterialFlag.

	\return The material flags.

	\see PxMaterialFlag setFlags()
	*/
	INTEROP_API CPxMaterialFlag CPxMaterial_getFlags(CPxMaterial* cpp);

	/**
	\brief Sets the friction combine mode.

	See the enum ::PxCombineMode .

	<b>Default:</b> PxCombineMode::eAVERAGE

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in] combMode Friction combine mode to set for this material. See #PxCombineMode.

	\see PxCombineMode getFrictionCombineMode setStaticFriction() setDynamicFriction()
	*/
	INTEROP_API void CPxMaterial_setFrictionCombineMode(CPxMaterial* cpp, CPxCombineMode combMode);

	/**
	\brief Retrieves the friction combine mode.

	See #setFrictionCombineMode.

	\return The friction combine mode for this material.

	\see PxCombineMode setFrictionCombineMode()
	*/
	INTEROP_API CPxCombineMode CPxMaterial_getFrictionCombineMode(CPxMaterial* cpp);

	/**
	\brief Sets the restitution combine mode.

	See the enum ::PxCombineMode .

	<b>Default:</b> PxCombineMode::eAVERAGE

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in] combMode Restitution combine mode for this material. See #PxCombineMode.

	\see PxCombineMode getRestitutionCombineMode() setRestitution()
	*/
	INTEROP_API void CPxMaterial_setRestitutionCombineMode(CPxMaterial* cpp, CPxCombineMode combMode);

	/**
	\brief Retrieves the restitution combine mode.

	See #setRestitutionCombineMode.

	\return The coefficient of restitution combine mode for this material.

	\see PxCombineMode setRestitutionCombineMode getRestitution()
	*/
	INTEROP_API CPxCombineMode CPxMaterial_getRestitutionCombineMode(CPxMaterial* cpp);

	/**
	\brief Sets the damping combine mode.

	See the enum ::PxCombineMode .

	<b>Default:</b> PxCombineMode::eAVERAGE

	<b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.

	\param[in] combMode Damping combine mode for this material. See #PxCombineMode.

	\see PxCombineMode getDampingCombineMode() setDamping()
	*/
	INTEROP_API void CPxMaterial_setDampingCombineMode(CPxMaterial* cpp, CPxCombineMode combMode);

	/**
	\brief Retrieves the damping combine mode.

	\return The damping combine mode for this material.

	\see PxCombineMode setDampingCombineMode() getDamping()
	*/
	INTEROP_API CPxCombineMode CPxMaterial_getDampingCombineMode(CPxMaterial* cpp);

	INTEROP_API const char* CPxMaterial_getConcreteTypeName(CPxMaterial* cpp);
}