#include "Shared.hpp"

#define RGBCX_IMPLEMENTATION
#include "bc7enc_rdo/rgbcx.h"
#include "bc7enc_rdo/ert.h"

enum class EncoderType : uint8_t
{
	BC1 = 0,
	BC3,
	BC4,
	BC5
};

static void EncodeGeneric(ImageBitmap* bitmap, ImageOutput* output, EncoderType type)
{
	// borrowed from "rdo_bc_encoder.cpp:encode_texture()" in the "bc7enc_rdo" repo

	block8* currBlock8 = (block8*)output->Pixels;
	block16* currBlock16 = (block16*)output->Pixels;

	uint32_t sourceCopyStride = 4 * bitmap->Stride;
	uint32_t* pixelsRGBA = (uint32_t*)bitmap->Pixels;

	for (int by = 0; by < (int)output->BlocksY; by++)
	{
		for (uint32_t bx = 0; bx < output->BlocksX; bx++)
		{
			color_quad_u8 pixels[16];

			for (uint32_t y = 0; y < 4; ++y)
				memcpy(pixels + y * 4, pixelsRGBA + (bx * 4 + (by * 4 + y) * bitmap->Width), sourceCopyStride);

			switch (type)
			{
				case EncoderType::BC1:
				{
					rgbcx::encode_bc1(bitmap->Effort, currBlock8++, &pixels[0].m_c[0], true, true);

					break;
				}
				case EncoderType::BC3:
				{
					rgbcx::encode_bc3(bitmap->Effort, currBlock16++, &pixels[0].m_c[0]);

					break;
				}
				case EncoderType::BC4:
				{
					rgbcx::encode_bc4(currBlock8++, &pixels[0].m_c[0], bitmap->Stride);

					break;
				}
				case EncoderType::BC5:
				{
					rgbcx::encode_bc5(currBlock16++, &pixels[0].m_c[0], 0, 1, bitmap->Stride);

					break;
				}
			}
		}
	}
}

extern "C"
{
	__declspec(dllexport) void InitBC15Encoder()
	{
		rgbcx::init();
	}

	__declspec(dllexport) void EncodeBC1(ImageBitmap* bitmap, ImageOutput* output)
	{
		EncodeGeneric(bitmap, output, EncoderType::BC1);
	}

	__declspec(dllexport) void EncodeBC3(ImageBitmap* bitmap, ImageOutput* output)
	{
		EncodeGeneric(bitmap, output, EncoderType::BC3);
	}

	__declspec(dllexport) void EncodeBC4(ImageBitmap* bitmap, ImageOutput* output)
	{
		EncodeGeneric(bitmap, output, EncoderType::BC4);
	}

	__declspec(dllexport) void EncodeBC5(ImageBitmap* bitmap, ImageOutput* output)
	{
		EncodeGeneric(bitmap, output, EncoderType::BC5);
	}
}