#include <ft2build.h>
#include <freetype/freetype.h>
#include <freetype/ftmm.h>
#include <freetype/ftsnames.h>

#include "msdfgen/msdfgen.h"
#include "msdfgen/msdfgen-ext.h"

#pragma pack(1)
struct MSDF_FontFace
{
	FT_Face Face;
	msdfgen::FontHandle* MSDFFont;
};

struct MSDF_VarFontMetrics
{
	uint32_t AxisCount;
	uint32_t DesignCount;
	uint32_t NamedStyleCount;
};

struct MSDF_VarFontAxis
{
	const char* Name;
	uint32_t NameLength;

	int64_t Minimum;
	int64_t Default;
	int64_t Maximum;
};

struct MSDF_VarFontStyle
{
	const char* Name;
	uint32_t NameLength;
};

struct MSDF_ShapedGlyph
{
	double BearingX;
	double BearingY;

	double Width;
	double Height;

	double Advance;
	msdfgen::Shape Shape;
};

struct MSDF_RenderBitmap
{
	float* Pixels;

	int Width;
	int Height;

	int RowStride;
};

struct MSDF_RenderBox
{
	int RectW;
	int RectH;
	msdfgen::Range Range;
	msdfgen::Projection Projection;
};
#pragma pack(pop)

extern "C"
{
	__declspec(dllexport) FT_Library MSDF_InitFt()
	{
		FT_Library lib = nullptr;
		if (FT_Init_FreeType(&lib) != 0)
			return nullptr;

		return lib;
	}

	__declspec(dllexport) void MSDF_ShutdownFt(FT_Library ft)
	{
		if (ft != nullptr)
		{
			FT_Done_FreeType(ft);
		}
	}

	__declspec(dllexport) MSDF_FontFace* MSDF_LoadFont(FT_Library ft, char* fileName)
	{
		FT_Face face = nullptr;

		FT_Error err = FT_New_Face(ft, fileName, 0, &face);
		if (err != 0)
			return nullptr;

		FT_Select_Charmap(face, FT_ENCODING_UNICODE);
		
		return new MSDF_FontFace{
			face,
			msdfgen::adoptFreetypeFont(face)
		};
	}

	__declspec(dllexport) void MSDF_DestroyFont(MSDF_FontFace* face)
	{
		if (face != nullptr)
		{
			msdfgen::destroyFont(face->MSDFFont);
			FT_Done_Face(face->Face);

			delete face;
		}
	}

	__declspec(dllexport) MSDF_ShapedGlyph* MSDF_CreateShapedGlyph()
	{
		return new MSDF_ShapedGlyph{};
	}

	__declspec(dllexport) void MSDF_DestroyShapedGlyph(MSDF_ShapedGlyph* glyph)
	{
		if (glyph != nullptr)
		{
			delete glyph;
		}
	}

	__declspec(dllexport) void MSDF_SetFontPixelSize(MSDF_FontFace* face, uint32_t width, uint32_t height)
	{
		FT_Set_Pixel_Sizes(face->Face, width, height);
	}

	__declspec(dllexport) bool MSDF_GetWhitespaceWidth(MSDF_FontFace* face, double* spaceAdvance, double* tabAdvance)
	{
		double scale = 1.0 / (face->Face->units_per_EM ? face->Face->units_per_EM : 1);

		if (FT_Load_Char(face->Face, ' ', FT_LOAD_NO_SCALE))
			return false;
		*spaceAdvance = face->Face->glyph->advance.x * scale;

		if (FT_Load_Char(face->Face, '\t', FT_LOAD_NO_SCALE))
			return false;
		*tabAdvance = face->Face->glyph->advance.x * scale;

		return true;
	}

	__declspec(dllexport) void MSDF_GetLineHeight(MSDF_FontFace* face, double* lineHeight)
	{
		double scale = 1.0 / (face->Face->units_per_EM ? face->Face->units_per_EM : 1);
		*lineHeight = (face->Face->ascender - face->Face->descender) * scale;
	}

	__declspec(dllexport) FT_MM_Var* MSDF_GetVarFontData(MSDF_FontFace* face, MSDF_VarFontMetrics* metrics)
	{
		FT_MM_Var* vars = nullptr;

		FT_Error err = FT_Get_MM_Var(face->Face, &vars);
		if (err != 0)
			return nullptr;

		*metrics = {
			vars->num_axis,
			vars->num_designs,
			vars->num_namedstyles
		};

		return vars;
	}

	__declspec(dllexport) void MSDF_DestroyVarData(FT_Library ft, FT_MM_Var* vars)
	{
		if (vars != nullptr)
		{
			FT_Done_MM_Var(ft, vars);
		}
	}

	__declspec(dllexport) bool MSDF_GetVarFontAxis(MSDF_FontFace* face, FT_MM_Var* vars, uint32_t index, MSDF_VarFontAxis* axis)
	{
		if (index >= vars->num_axis)
			return false;

		const FT_Var_Axis& varAxis = vars->axis[index];

		FT_SfntName name{};
		FT_Error err = FT_Get_Sfnt_Name(face->Face, varAxis.strid, &name);
		
		*axis = {
			(const char*)name.string,
			name.string_len,

			varAxis.minimum,
			varAxis.def,
			varAxis.maximum,
		};

		return true;
	}

	__declspec(dllexport) FT_Var_Named_Style* MSDF_GetVarFontStyle(MSDF_FontFace* face, FT_MM_Var* vars, uint32_t index, MSDF_VarFontStyle* style)
	{
		if (index >= vars->num_namedstyles)
			return nullptr;

		const FT_Var_Named_Style& varStyle = vars->namedstyle[index];

		FT_SfntName name{};
		for (size_t i = 0; i < FT_Get_Sfnt_Name_Count(face->Face); i++)
		{
			name = {};
			FT_Error err = FT_Get_Sfnt_Name(face->Face, i, &name);
			
			if (name.name_id == varStyle.strid)
				break;
		}
		
		*style = {
			(const char*)name.string,
			name.string_len
		};

		return &vars->namedstyle[index];
	}

	__declspec(dllexport) void MSDF_SetFontStyle(MSDF_FontFace* face, uint32_t index, FT_Var_Named_Style* style)
	{
		if (index == std::numeric_limits<uint32_t>::max())
		{
			if (FT_Get_Default_Named_Instance(face->Face, &index) != 0)
				index = 0;
		}
		else
		{
			++index;
		}

		FT_Set_Named_Instance(face->Face, index);
	}

	__declspec(dllexport) bool MSDF_ShapeGlyph(MSDF_FontFace* face, uint32_t glyph, MSDF_ShapedGlyph* outData)
	{
		if (msdfgen::loadGlyph(outData->Shape, face->MSDFFont, glyph, msdfgen::FONT_SCALING_EM_NORMALIZED, &outData->Advance) && outData->Shape.validate())
		{
			outData->Shape.normalize();

			double scale = 1.0 / (face->Face->units_per_EM ? face->Face->units_per_EM : 1);
			msdfgen::Shape::Bounds bounds = outData->Shape.getBounds();

			outData->BearingX = bounds.l;
			outData->BearingY = bounds.b;
			
			outData->Width = bounds.r - bounds.l;
			outData->Height = bounds.t - bounds.b;

			return true;
		}

		return false;
	}

	__declspec(dllexport) void MSDF_GenerateGlyph(MSDF_ShapedGlyph* shapedGlyph, MSDF_RenderBox* renderBox, MSDF_RenderBitmap* bitmap)
	{
		msdfgen::BitmapSection<float, 4> section = msdfgen::BitmapSection<float, 4>(bitmap->Pixels, bitmap->Width, bitmap->Height, msdfgen::Y_DOWNWARD);
		msdfgen::SDFTransformation transformation(renderBox->Projection, renderBox->Range);

		msdfgen::edgeColoringByDistance(shapedGlyph->Shape, 3.0);
		msdfgen::generateMTSDF(section, shapedGlyph->Shape, transformation);
		msdfgen::distanceSignCorrection(section, shapedGlyph->Shape, transformation, msdfgen::FILL_NONZERO);
		msdfgen::msdfErrorCorrection(section, shapedGlyph->Shape, transformation, msdfgen::Range(0.125));
	}

	__declspec(dllexport) void MSDF_CalculateBox(MSDF_ShapedGlyph* shapedGlyph, double minScale, double pxRange, double miterLimit, int pxPaddingX, int pxPaddingY, MSDF_RenderBox* renderBox)
	{
		renderBox->Range = pxRange / minScale;

		double paddingX = pxPaddingX / minScale;
		double paddingY = pxPaddingY / minScale;

		double rectTranslateX = 0;
		double rectTranslateY = 0;
		int rectW = 0;
		int rectH = 0;

		//wrapBox
		{
			msdfgen::Shape::Bounds bounds = shapedGlyph->Shape.getBounds();
			if (bounds.l < bounds.r && bounds.b < bounds.t)
			{
				double l = bounds.l, b = bounds.b, r = bounds.r, t = bounds.t;
				l += renderBox->Range.lower, b += renderBox->Range.lower;
				r -= renderBox->Range.lower, t -= renderBox->Range.lower;

				if (miterLimit > 0.0)
					shapedGlyph->Shape.boundMiters(l, b, r, t, -renderBox->Range.lower, miterLimit, 1);

				l -= paddingX, b -= paddingY;
				r += paddingX, t += paddingY;

				double w = minScale * (r - l);
				rectW = (int)ceil(w) + 1;
				rectTranslateX = -l + .5 * (rectW - w) / minScale;

				double h = minScale * (t - b);
				rectH = (int)ceil(h) + 1;
				rectTranslateY = -b + .5 * (rectH - h) / minScale;
			}
		}

		renderBox->RectW = rectW;
		renderBox->RectH = rectH;

		renderBox->Projection = msdfgen::Projection(msdfgen::Vector2(minScale), msdfgen::Vector2(rectTranslateX, rectTranslateY));
	}
}