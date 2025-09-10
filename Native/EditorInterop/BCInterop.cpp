#define RGBCX_IMPLEMENTATION

#include "BC/rgbcx.h"
#include "BC/bc7enc.h"

typedef void* (*SPIRV_OptimizeAlloc)(size_t size);

#pragma pack(1)
enum class BC_Format : uint8_t
{
	BC1,
	BC1a,
	BC2,
	BC3,
	BC3n,
	BC4u,
	BC5u,
	BC6s,
	BC6u,
	BC7
};

struct SPIRV_OptimizeOut
{
	SPIRV_OptimizeAlloc Alloc;

	uint32_t* InBinary;
	size_t InSize;

	uint32_t* OutBinary;
	size_t OutSize;
};
#pragma pack(pop)

extern "C"
{
	
}