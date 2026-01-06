#define RGBCX_IMPLEMENTATION

#include "bc7enc_rdo/rgbcx.h"
#include "bc7enc_rdo/bc7enc.h"

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
#pragma pack(pop)

extern "C"
{
	
}