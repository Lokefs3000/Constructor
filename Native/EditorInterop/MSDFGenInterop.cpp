#include <ft2build.h>
#include <freetype/freetype.h>
#include <freetype/ftmm.h>
#include <freetype/ftsnames.h>

#include "msdfgen/msdfgen.h"
#include "msdfgen/msdfgen-ext.h"

#pragma pack(show)
struct SzFontGlyph
{
	uint8_t ContourCount;
	int Advance;
	int DataSize;
};

struct SzFontContour
{
	uint8_t EdgeCount;
};

enum class SzFontEdgeType : uint8_t
{
	Linear = 0,
	Quadratic,
	Cubic
};

struct SzFontPoint
{
	int X;
	int Y;
};

#pragma pack(push, 1)
struct MSDF_FontFace
{
	FT_Face Face;
	msdfgen::FontHandle* MSDFFont;

	void* HeldFaceMemory;
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

struct MSDF_EdgeData
{
	uint32_t Type;
	msdfgen::Point2* Points;
};

struct MSDF_KernData
{
	int X;
	int Y;
};

#pragma pack(pop)

//#define ConvertToPoint2(p) msdfgen::Point2(p.FixedX / (double)SzFontPoint::DecimalPlaces, p.FixedY / (double)SzFontPoint::DecimalPlaces)
#define ConvertToPoint2(p, scale) msdfgen::Point2(p.X * scale, p.Y * scale)

#define ArrayLength(arr) (sizeof(arr) / sizeof(arr[0]))

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
			msdfgen::adoptFreetypeFont(face),
			nullptr
		};
	}

	__declspec(dllexport) MSDF_FontFace* MSDF_LoadFont_Memory(FT_Library ft, uint8_t* memory, uint64_t fileSize)
	{
		uint8_t* memCopy = (uint8_t*)malloc(fileSize);
		memcpy(memCopy, memory, fileSize);

		FT_Face face = nullptr;

		FT_Error err = FT_New_Memory_Face(ft, memCopy, fileSize, 0, &face);
		if (err != 0)
			return nullptr;

		FT_Select_Charmap(face, FT_ENCODING_UNICODE);

		return new MSDF_FontFace{
			face,
			msdfgen::adoptFreetypeFont(face),
			memCopy
		};
	}

	__declspec(dllexport) void MSDF_DestroyFont(MSDF_FontFace* face)
	{
		if (face != nullptr)
		{
			if (face->HeldFaceMemory != nullptr)
				free(face->HeldFaceMemory);
			msdfgen::destroyFont(face->MSDFFont);
			FT_Done_Face(face->Face);

			delete face;
		}
	}

	__declspec(dllexport) MSDF_ShapedGlyph* MSDF_CreateShapedGlyph()
	{
		return new MSDF_ShapedGlyph{};
	}

	__declspec(dllexport) MSDF_ShapedGlyph* MSDF_DeserializeShapedGlyph(char* sourceData, double scale)
	{
		char* head = sourceData;

		SzFontGlyph glyph = *(SzFontGlyph*)head;
		head += sizeof(SzFontGlyph);

		msdfgen::Shape shape{};
		shape.contours.reserve(glyph.ContourCount);

		for (size_t i = 0; i < glyph.ContourCount; i++)
		{
			SzFontContour contourData = *(SzFontContour*)head;
			head += sizeof(SzFontContour);

			msdfgen::Contour contour{};
			contour.edges.reserve(contourData.EdgeCount);

			for (size_t j = 0; j < contourData.EdgeCount; j++)
			{
				SzFontEdgeType edgeType = *(SzFontEdgeType*)head;
				head += sizeof(SzFontEdgeType);

				SzFontPoint* points = (SzFontPoint*)head;
				head += ((size_t)edgeType + 2) * sizeof(SzFontPoint);

				switch (edgeType)
				{
				case SzFontEdgeType::Linear:
				{
					msdfgen::Point2 p0 = ConvertToPoint2(points[0], scale);
					msdfgen::Point2 p1 = ConvertToPoint2(points[1], scale);

					contour.addEdge(msdfgen::EdgeHolder(p0, p1));
					break;
				}
				case SzFontEdgeType::Quadratic:
				{
					msdfgen::Point2 p0 = ConvertToPoint2(points[0], scale);
					msdfgen::Point2 p1 = ConvertToPoint2(points[1], scale);
					msdfgen::Point2 p2 = ConvertToPoint2(points[2], scale);

					contour.addEdge(msdfgen::EdgeHolder(p0, p1, p2));
					break;
				}
				case SzFontEdgeType::Cubic:
				{
					msdfgen::Point2 p0 = ConvertToPoint2(points[0], scale);
					msdfgen::Point2 p1 = ConvertToPoint2(points[1], scale);
					msdfgen::Point2 p2 = ConvertToPoint2(points[2], scale);
					msdfgen::Point2 p3 = ConvertToPoint2(points[3], scale);

					contour.addEdge(msdfgen::EdgeHolder(p0, p1, p2, p3));
					break;
				}
				}
			}

			shape.addContour(contour);
		}

		MSDF_ShapedGlyph* shapedGlyph = MSDF_CreateShapedGlyph();
		shapedGlyph->Advance = glyph.Advance * scale;
		shapedGlyph->Shape = shape;

		return shapedGlyph;
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

	__declspec(dllexport) uint16_t MSDF_GetUnitsPerEM(MSDF_FontFace* face)
	{
		return face->Face->units_per_EM ? face->Face->units_per_EM : 1;
	}

	__declspec(dllexport) bool MSDF_GetWhitespaceWidth(MSDF_FontFace* face, int* spaceAdvance, int* tabAdvance)
	{
		if (FT_Load_Char(face->Face, ' ', FT_LOAD_NO_SCALE))
			return false;
		*spaceAdvance = face->Face->glyph->advance.x;

		if (FT_Load_Char(face->Face, '\t', FT_LOAD_NO_SCALE))
			return false;
		*tabAdvance = face->Face->glyph->advance.x;

		return true;
	}

	__declspec(dllexport) void MSDF_GetMetrics(MSDF_FontFace* face, short* ascender, short* descender, short* lineHeight, short* underlineY, short* fontHeight)
	{
		*ascender = -face->Face->ascender;
		*descender = -face->Face->descender;
		*lineHeight = (face->Face->ascender - face->Face->descender);
		*underlineY = -face->Face->underline_position;
		*fontHeight = face->Face->height;
	}

	__declspec(dllexport) bool MSDF_GetKerning(MSDF_FontFace* face, uint32_t left, uint32_t right, MSDF_KernData* kernData)
	{
		uint32_t leftGlyph = FT_Get_Char_Index(face->Face, left);
		uint32_t rightGlyph = FT_Get_Char_Index(face->Face, right);

		if (leftGlyph == 0 || rightGlyph == 0)
		{
			return false;
		}

		FT_Vector vector;
		if (FT_Get_Kerning(face->Face, leftGlyph, rightGlyph, FT_KERNING_UNSCALED, &vector))
		{
			return false;
		}

		*kernData = MSDF_KernData{ vector.x, vector.y };
		return true;
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
		if (msdfgen::loadGlyph(outData->Shape, face->MSDFFont, glyph, msdfgen::FONT_SCALING_NONE, &outData->Advance) && outData->Shape.validate())
		{
			// no glyph data
			if (outData->Shape.contours.empty())
				return false;

			outData->Shape.normalize();

			double scale = 1.0 / (face->Face->units_per_EM ? face->Face->units_per_EM : 1);
			msdfgen::Shape::Bounds bounds = outData->Shape.getBounds();

			outData->BearingX = face->Face->glyph->metrics.horiBearingX * -scale;
			outData->BearingY = face->Face->glyph->metrics.horiBearingY * -scale;

			return true;
		}

		return false;
	}

	__declspec(dllexport) void MSDF_ScaleGlyph(MSDF_ShapedGlyph* shapedGlyph, double emScale)
	{
		for (size_t i = 0; i < shapedGlyph->Shape.contours.size(); i++)
		{
			msdfgen::Contour& contour = shapedGlyph->Shape.contours[i];
			for (size_t j = 0; j < contour.edges.size(); j++)
			{
				msdfgen::EdgeSegment* edge = contour.edges[j];
				switch (edge->type())
				{
					case msdfgen::LinearSegment::EDGE_TYPE:
					{
						msdfgen::LinearSegment* linear = (msdfgen::LinearSegment*)edge;
						for (size_t k = 0; k < ArrayLength(linear->p); k++)
						{
							msdfgen::Vector2& p = linear->p[k];

							p.x *= emScale;
							p.y *= emScale;
						}

						break;
					}
					case msdfgen::QuadraticSegment::EDGE_TYPE:
					{
						msdfgen::QuadraticSegment* quadratic = (msdfgen::QuadraticSegment*)edge;
						for (size_t k = 0; k < ArrayLength(quadratic->p); k++)
						{
							msdfgen::Vector2& p = quadratic->p[k];

							p.x *= emScale;
							p.y *= emScale;
						}

						break;
					}
					case msdfgen::CubicSegment::EDGE_TYPE:
					{
						msdfgen::CubicSegment* cubic = (msdfgen::CubicSegment*)edge;
						for (size_t k = 0; k < ArrayLength(cubic->p); k++)
						{
							msdfgen::Vector2& p = cubic->p[k];

							p.x *= emScale;
							p.y *= emScale;
						}

						break;
					}
				}
			}
		}
	}

	__declspec(dllexport) uint32_t MSDF_QueryShapeContours(MSDF_ShapedGlyph* shapedGlyph)
	{
		return shapedGlyph->Shape.contours.size();
	}

	__declspec(dllexport) uint32_t MSDF_QueryContourEdges(MSDF_ShapedGlyph* shapedGlyph, uint32_t contourIndex)
	{
		return shapedGlyph->Shape.contours[contourIndex].edges.size();
	}

	__declspec(dllexport) void MSDF_GetContourEdges(MSDF_ShapedGlyph* shapedGlyph, uint32_t contourIndex, uint32_t edgeIndex, MSDF_EdgeData* edgeData)
	{
		msdfgen::EdgeSegment* edge = shapedGlyph->Shape.contours[contourIndex].edges[edgeIndex];

		edgeData->Type = edge->type();
		switch (edgeData->Type)
		{
		case msdfgen::LinearSegment::EDGE_TYPE: edgeData->Points = ((msdfgen::LinearSegment*)edge)->p; break;
		case msdfgen::QuadraticSegment::EDGE_TYPE: edgeData->Points = ((msdfgen::QuadraticSegment*)edge)->p; break;
		case msdfgen::CubicSegment::EDGE_TYPE: edgeData->Points = ((msdfgen::CubicSegment*)edge)->p; break;
		default: abort(); break;
		}
	}

	__declspec(dllexport) void MSDF_GenerateGlyph(MSDF_ShapedGlyph* shapedGlyph, MSDF_RenderBox* renderBox, MSDF_RenderBitmap* bitmap)
	{
		msdfgen::BitmapSection<float, 4> section = msdfgen::BitmapSection<float, 4>(bitmap->Pixels, bitmap->Width, bitmap->Height, msdfgen::Y_DOWNWARD);
		msdfgen::SDFTransformation transformation(renderBox->Projection, renderBox->Range);

		float sdfZeroValue = renderBox->Range.lower != renderBox->Range.upper ? (float)(renderBox->Range.lower / (renderBox->Range.lower - renderBox->Range.upper)) : 0.5f;

		msdfgen::MSDFGeneratorConfig config = msdfgen::MSDFGeneratorConfig(true);
		config.errorCorrection.mode = msdfgen::ErrorCorrectionConfig::Mode::EDGE_ONLY;
		config.errorCorrection.distanceCheckMode = msdfgen::ErrorCorrectionConfig::DistanceCheckMode::DO_NOT_CHECK_DISTANCE;

		msdfgen::edgeColoringByDistance(shapedGlyph->Shape, 3.0);
		msdfgen::generateMTSDF(section, shapedGlyph->Shape, transformation);
		msdfgen::distanceSignCorrection(section, shapedGlyph->Shape, transformation, sdfZeroValue, msdfgen::FILL_NONZERO);
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