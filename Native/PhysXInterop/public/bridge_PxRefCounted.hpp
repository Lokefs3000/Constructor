#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Decrements the reference count of the object and releases it if the new reference count is zero.
	*/
	INTEROP_API void CPxRefCounted_release(CPxRefCounted* cpp);

	/**
	\brief Returns the reference count of the object.

	At creation, the reference count of the object is 1. Every other object referencing this object increments the
	count by 1.	When the reference count reaches 0, and only then, the object gets destroyed automatically.

	\return the current reference count.
	*/
	INTEROP_API CPxU32 CPxRefCounted_getReferenceCount(CPxRefCounted* cpp);

	/**
	\brief Acquires a counted reference to this object.

	This method increases the reference count of the object by 1. Decrement the reference count by calling release()
	*/
	INTEROP_API void CPxRefCounted_acquireReference(CPxRefCounted* cpp);
}