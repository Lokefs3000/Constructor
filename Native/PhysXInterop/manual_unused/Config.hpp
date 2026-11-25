#pragma once

#ifdef PHYSXINTEROP_EXPORTS
#define INTEROP_API __declspec(dllexport)
#else
#define INTEROP_API __declspec(dllimport)
#endif

#define MTABLE_FUNCTION_VOID(className, name, ...) void (className::*name)(__VA_ARGS__)
#define MTABLE_FUNCTION_WITH(ret, className, name, ...) ret (className::*name)(__VA_ARGS__)