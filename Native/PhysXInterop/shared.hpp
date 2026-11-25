#pragma once

#include <cassert>

#ifdef PHYSXINTEROP_EXPORTS
#define INTEROP_API __declspec(dllexport)
#else
#define INTEROP_API __declspec(dllimport)
#endif

#define BRIDGE_METHOD_FUNC(name, ret, ...) \
	INTEROP_API ret ACTIVE_CLASS##_##name(__VA_ARGS__) \
	{

#define BRIDGE_METHOD_END }

#define GET(name, val) static_cast<name##*>(val)
#define CHANGE(name, val) reinterpret_cast<name##&>(val)

#define ASSERT_ENUM(value, required) static_assert(static_cast<uint32_t>(value) == static_cast<uint32_t>(required), "Enum value '"#value"' does not match the required value of '"#required"'")

// #define MTABLE_FUNCTION_VOID(className, name, ...) void (className::*name)(__VA_ARGS__)
// #define MTABLE_FUNCTION_WITH(ret, className, name, ...) ret (className::*name)(__VA_ARGS__)