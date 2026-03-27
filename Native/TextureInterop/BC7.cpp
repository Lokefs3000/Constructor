#include "Shared.hpp"

#include <algorithm>

#include "bc7enc_rdo/bc7enc.h"
#include "bc7enc_rdo/bc7e_ispc.h"

extern "C"
{
	__declspec(dllexport) void InitBC7Encoder()
	{
		bc7enc_compress_block_init();
		ispc::bc7e_compress_block_init();
	}

	__declspec(dllexport) void EncodeBC7(ImageBitmap* bitmap, ImageOutput* output)
	{
		// borrowed from "rdo_bc_encoder.cpp:encode_texture()" in the "bc7enc_rdo" repo

		ispc::bc7e_compress_block_params params{};
		ispc::bc7e_compress_block_params_init_slow(&params, true);

		uint32_t bc7_mode_hist[8];
		memset(bc7_mode_hist, 0, sizeof(bc7_mode_hist));

		block16* packed = (block16*)output->Pixels;

		uint32_t sourceCopyStride = 4 * bitmap->Stride;
		uint32_t* pixelsRGBA = (uint32_t*)bitmap->Pixels;

		for (int32_t by = 0; by < static_cast<int32_t>(output->BlocksY); by++)
		{
			// Process 64 blocks at a time, for efficient SIMD processing.
			// Ideally, N >= 8 (or more) and (N % 8) == 0.
			const int N = 64;

			for (uint32_t bx = 0; bx < output->BlocksX; bx += N)
			{
				const uint32_t num_blocks_to_process = std::min<uint32_t>(output->BlocksX - bx, N);

				color_quad_u8 pixels[16 * N];

				// Extract num_blocks_to_process 4x4 pixel blocks from the source image and put them into the pixels[] array.
				for (uint32_t b = 0; b < num_blocks_to_process; b++)
				{
					color_quad_u8* startPixels = pixels + b * 16;
					for (uint32_t y = 0; y < 4; ++y)
						memcpy(startPixels + y * 4, pixelsRGBA + ((bx + b) * 4 + (by * 4 + y) * bitmap->Width), sourceCopyStride);
				}

				// Compress the blocks to BC7.
				// Note: If you've used Intel's ispc_texcomp, the input pixels are different. BC7E requires a pointer to an array of 16 pixels for each block.
				block16* pBlock = &packed[bx + by * output->BlocksX];
				ispc::bc7e_compress_blocks(num_blocks_to_process, reinterpret_cast<uint64_t*>(pBlock), reinterpret_cast<const uint32_t*>(pixels), &params);
			}
		}
	}
}