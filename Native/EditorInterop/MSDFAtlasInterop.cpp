#include <msdf-atlas-gen/msdf-atlas-gen.h>

struct MSDFAtlas_AtlasData
{
	msdf_atlas::TightAtlasPacker Packer;
};

extern "C"
{
	__declspec(dllexport) msdfgen::FreetypeHandle* MSDFAtlas_InitFreetype()
	{
		return msdfgen::initializeFreetype();
	}

	__declspec(dllexport) msdfgen::FontHandle* MSDFAtlas_LoadFont(msdfgen::FreetypeHandle* ft, const char* text)
	{
		return msdfgen::loadFont(ft, text);
	}

	//Create / Destroy
	__declspec(dllexport) msdf_atlas::GlyphGeometry* MSDFAtlas_CreateGlyphGeometry()
	{
		return new msdf_atlas::GlyphGeometry();
	}

	__declspec(dllexport) void MSDFAtlas_DestroyGlyphGeometry(msdf_atlas::GlyphGeometry* geo)
	{
		delete geo;
	}

	//__declspec(dllexport) msdf_atlas::TightAtlasPacker* MSDFAtlas_CreateGlyphGeometry(double minScale, double pixelRange, double miterLimit)
	//{
	//	msdf_atlas::TightAtlasPacker packer;
	//	packer.setDimensionsConstraint(msdf_atlas::DimensionsConstraint::SQUARE);
	//	packer.setMinimumScale(minScale);
	//	packer.setPixelRange(pixelRange);
	//	packer.setMiterLimit(miterLimit);
	//
	//	//return packer;
	//	return nullptr;
	//}
	//
	//__declspec(dllexport) void MSDFAtlas_DestroyGlyphGeometry(msdf_atlas::GlyphGeometry* geo)
	//{
	//	delete geo;
	//}
}