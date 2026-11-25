#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Releases the PxBase instance, please check documentation of release in derived class.
	*/
	INTEROP_API void CPxBase_release(CPxBase* cpp);

	/**
	\brief Returns string name of dynamic type.
	\return	Class name of most derived type of this object.
	*/
	INTEROP_API const char* CPxBase_getConcreteTypeName(CPxBase* cpp);

	/**
	\brief	Returns concrete type of object.
	\return	PxConcreteType::Enum of serialized object

	\see PxConcreteType
	*/
	INTEROP_API CPxType CPxBase_getConcreteType(CPxBase* cpp);

	/**
	\brief Set PxBaseFlag

	\param[in] flag The flag to be set
	\param[in] value The flags new value
	*/
	INTEROP_API void CPxBase_setBaseFlag(CPxBase* cpp, CPxBaseFlag flag, bool value);

	/**
	\brief Set PxBaseFlags

	\param[in] inFlags The flags to be set

	\see PxBaseFlags
	*/
	INTEROP_API void CPxBase_setBaseFlags(CPxBase* cpp, CPxBaseFlag inFlags);

	/**
	\brief Returns PxBaseFlags

	\return	PxBaseFlags

	\see PxBaseFlags
	*/
	INTEROP_API CPxBaseFlag CPxBase_getBaseFlags(CPxBase* cpp);

	/**
	\brief Whether the object is subordinate.

	A class is subordinate, if it can only be instantiated in the context of another class.

	\return	Whether the class is subordinate

	\see PxSerialization::isSerializable
	*/
	INTEROP_API bool CPxBase_isReleasable(CPxBase* cpp);
}