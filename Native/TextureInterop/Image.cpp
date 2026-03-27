#include "Shared.hpp"

#include "libpng/png.h"
#include "turbojpeg/turbojpeg.h"

#define STB_IMAGE_IMPLEMENTATION
#include "stb/stb_image.h"

#pragma pack(push, 1)

struct ImageLoadData
{
	uint8_t* Data;
	uint32_t Length;
};

struct ImageMetrics
{
	uint32_t Width;
	uint32_t Height;

	uint8_t Stride;
};

#pragma pack(pop)

extern "C"
{
	__declspec(dllexport) bool LoadPNG(ImageLoadData* imageData, ImageBitmap* output)
	{
		/*
		if (dataLength < 8 || !png_sig_cmp(data, 0, 8))
		{
			return false;
		}

		png_structp png_ptr = png_create_read_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
		if (png_ptr == nullptr)
		{
			return false;
		}

		png_infop info_ptr = png_create_info_struct(png_ptr);
		if (info_ptr == nullptr)
		{
			png_destroy_read_struct(&png_ptr, nullptr, nullptr);
			return false;
		}

		if (setjmp(png_jmpbuf(png_ptr)))
		{
			png_destroy_read_struct(&png_ptr, &info_ptr, nullptr);
			return false;
		}

		png()
		*/

		int w, h, ch;
		stbi_uc* pixels = stbi_load_from_memory(imageData->Data, imageData->Length, &w, &h, &ch, 0);
		if (pixels == nullptr)
		{
			return false;
		}

		output->Width = w;
		output->Height = h;
		output->Stride = ch;
		output->Pixels = pixels;
		return true;
	}

	__declspec(dllexport) void FreePNG(ImageBitmap* bitmap)
	{
		stbi_image_free(bitmap->Pixels);
	}

	__declspec(dllexport) bool QueryPNG(ImageLoadData* imageData, ImageMetrics* metrics)
	{
		int w, h, ch;
		if (stbi_info_from_memory(imageData->Data, imageData->Length, &w, &h, &ch) == 0)
		{
			return false;
		}

		metrics->Width = w;
		metrics->Height = h;
		metrics->Stride = ch;
		return true;
	}

	__declspec(dllexport) bool LoadJPEG(ImageLoadData* imageData, ImageBitmap* output)
	{
		tjhandle tj = tj3Init(TJINIT_DECOMPRESS);

		if (tj3DecompressHeader(tj, imageData->Data, imageData->Length) < 1)
		{
			tj3Destroy(tj);
			return false;
		}

		int width = tj3Get(tj, TJPARAM_JPEGWIDTH);
		int height = tj3Get(tj, TJPARAM_JPEGHEIGHT);
		TJCS colorSpace = (TJCS)tj3Get(tj, TJPARAM_COLORSPACE);
		int subSamp = tj3Get(tj, TJPARAM_SUBSAMP);

		size_t bufSize = tj3JPEGBufSize(width, height, subSamp);
		uint8_t* outBuf = (uint8_t*)tj3Alloc(bufSize);

		TJPF pixelFormat = TJPF_UNKNOWN;
		switch (colorSpace)
		{
			case TJCS_GRAY: pixelFormat = TJPF_GRAY; break;
			default: pixelFormat = TJPF_RGB; break;
		}

		if (tj3Decompress8(tj, imageData->Data, imageData->Length, outBuf, 0, pixelFormat) < 0)
		{
			tj3Destroy(tj);
			return false;
		}

		tj3Destroy(tj);

		output->Width = width;
		output->Height = height;
		output->Stride = pixelFormat == TJPF_GRAY ? 1 : 3;
		output->Pixels = outBuf;
		return true;
	}

	__declspec(dllexport) void FreeJPEG(ImageBitmap* bitmap)
	{
		tj3Free(bitmap->Pixels);
	}

	__declspec(dllexport) bool QueryJPEG(ImageLoadData* imageData, ImageMetrics* metrics)
	{
		tjhandle tj = tj3Init(TJINIT_DECOMPRESS);

		if (tj3DecompressHeader(tj, imageData->Data, imageData->Length) < 1)
		{
			tj3Destroy(tj);
			return false;
		}

		metrics->Width = tj3Get(tj, TJPARAM_JPEGWIDTH);
		metrics->Height = tj3Get(tj, TJPARAM_JPEGHEIGHT);
		metrics->Stride = tj3Get(tj, TJPARAM_COLORSPACE) == TJCS_GRAY ? 1 : 3;

		tj3Destroy(tj);
		return true;
	}
}