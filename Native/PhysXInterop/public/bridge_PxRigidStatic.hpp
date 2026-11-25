#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	INTEROP_API const char* CPxRigidStatic_getConcreteTypeName(CPxRigidStatic* cpp);
}