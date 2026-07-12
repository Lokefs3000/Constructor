using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Interop.MSDF
{
    public static unsafe partial class MSDFInterop
    {
        private const string LibraryName = "edinterop.dll";

        [LibraryImport(LibraryName, EntryPoint = "MSDF_InitFt", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_FTContext* InitFt();

        [LibraryImport(LibraryName, EntryPoint = "MSDF_ShutdownFt", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ShutdownFt(MSDF_FTContext* ft);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_LoadFont", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_FontFace* LoadFont(MSDF_FTContext* ft, string fileName);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_LoadFont_Memory", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_FontFace* LoadFont_Memory(MSDF_FTContext* ft, byte* memory, ulong fileSize);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_DestroyFont", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void DestroyFont(MSDF_FontFace* face);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_CreateShapedGlyph", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_ShapedGlyph* CreateShapedGlyph();

        [LibraryImport(LibraryName, EntryPoint = "MSDF_DestroyShapedGlyph", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void DestroyShapedGlyph(MSDF_ShapedGlyph* shapedGlyph);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GetUnitsPerEM", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ushort GetUnitsPerEM(MSDF_FontFace* face);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GetWhitespaceWidth", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool GetWhitespaceWidth(MSDF_FontFace* face, out int spaceAdvance, out int tabAdvance);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GetMetrics", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void GetMetrics(MSDF_FontFace* face, out short ascender, out short descner, out short lineHeight, out short underlineY, out short height);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GetKerning", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool GetKerning(MSDF_FontFace* face, uint left, uint right, out MSDF_KernData kernData);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GetVarFontData", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_VarFontData* GetVarFontData(MSDF_FontFace* face, MSDF_VarFontMetrics* metrics);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_DestroyVarData", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void DestroyVarData(MSDF_FTContext* ft, MSDF_VarFontData* vars);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GetVarFontAxis", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool GetVarFontAxis(MSDF_FontFace* face, MSDF_VarFontData* vars, uint index, MSDF_VarFontAxis* axis);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GetVarFontStyle", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_VarFontStyleData* GetVarFontStyle(MSDF_FontFace* face, MSDF_VarFontData* vars, uint index, MSDF_VarFontStyle* style);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_SetFontStyle", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetFontStyle(MSDF_FontFace* face, uint index, MSDF_VarFontStyleData* styleData);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_ShapeGlyph", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool ShapeGlyph(MSDF_FontFace* face, uint glyph, MSDF_ShapedGlyph* shapedGlyph);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_ScaleGlyph", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ScaleGlyph(MSDF_ShapedGlyph* shapedGlyph, double emScale);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_GenerateGlyph", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void GenerateGlyph(MSDF_ShapedGlyph* shapedGlyph, MSDF_RenderBox* renderBox, MSDF_RenderBitmap* bitmap);

        [LibraryImport(LibraryName, EntryPoint = "MSDF_CalculateBox", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void CalculateBox(MSDF_ShapedGlyph* shapedGlyph, double minScale, double pxRange, double miterLimit, int pxPaddingX, int pxPaddingY, MSDF_RenderBox* renderBox);
    }
}
