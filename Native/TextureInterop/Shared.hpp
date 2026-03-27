#pragma once

#include <cstdint>

#pragma pack(push, 1)

enum class ImageFormat : uint8_t
{
	BC1 = 0,
	BC2,
	BC3,
	BC4,
	BC5,

	BC7
};

struct ImageBitmap
{
	uint32_t Width;
	uint32_t Height;

	uint8_t Stride;

	uint8_t* Pixels;
};

struct ImageOutput
{
	uint32_t BlocksX;
	uint32_t BlocksY;

	uint8_t* Pixels;
};

#pragma pack(pop)

struct color_quad_u8
{
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4201)
#endif
	union
	{
		uint8_t m_c[4];
		struct
		{
			uint8_t r;
			uint8_t g;
			uint8_t b;
			uint8_t a;
		};
	};
#ifdef _MSC_VER
#pragma warning(pop)
#endif
};

struct block8
{
	uint64_t m_vals[1];
};

struct block16
{
	uint64_t m_vals[2];
};