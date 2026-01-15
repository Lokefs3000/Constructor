using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Interop.Ed
{
    public static unsafe partial class EdInterop
    {
        private const string LibraryName = "edinterop.dll";

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial nint SPIRV_CreateOptimize();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SPIRV_DestroyOptimize(nint optimizer);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SPIRV_OptRegisterPerfPasses(nint optimizer);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool SPIRV_RunOptimize(nint optimizer, SPIRV_OptimizeOut* @out);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial nint MSDF_InitFt();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MSDF_ShutdownFt(nint ft);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_FontFace* MSDF_LoadFont(nint ft, sbyte* fileName);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MSDF_DestroyFont(MSDF_FontFace* font);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial MSDF_ShapedGlyph* MSDF_CreateShapedGlyph();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MSDF_DestroyShapedGlyph(MSDF_ShapedGlyph* glyph);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool MSDF_SetFontPixelSize(MSDF_FontFace* font, uint width, uint height);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool MSDF_GetWhitespaceWidth(MSDF_FontFace* font, double* spaceAdvance, double* tabAdvance);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MSDF_GetLineHeight(MSDF_FontFace* face, double* lineHeight);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial nint MSDF_GetVarFontData(MSDF_FontFace* face, MSDF_VarFontMetrics* metrics);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MSDF_DestroyVarData(nint ft, nint vars);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool MSDF_GetVarFontAxis(MSDF_FontFace* face, nint vars, uint index, MSDF_VarFontAxis* axis);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial nint MSDF_GetVarFontStyle(MSDF_FontFace* face, nint vars, uint index, MSDF_VarFontStyle* style);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial nint MSDF_SetFontStyle(MSDF_FontFace* face, uint index, nint style);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool MSDF_ShapeGlyph(MSDF_FontFace* face, uint glyph, MSDF_ShapedGlyph* outData);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MSDF_GenerateGlyph(MSDF_ShapedGlyph* shapedGlyph, MSDF_RenderBox* renderBox, MSDF_RenderBitmap* bitmap);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MSDF_CalculateBox(MSDF_ShapedGlyph* shapedGlyph, double minScale, double pxRange, double miterLimit, int pxPaddingX, int pxPaddingY, MSDF_RenderBox* renderBox);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SPIRV_OptimizeOut
    {
        public delegate* unmanaged[Cdecl]<ulong, void*> Alloc;

        public uint* InBinary;
        public ulong InSize;

        public uint* OutBinary;
        public ulong OutSize;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MSDF_FontFace
    {
        public nint Face;
        public nint MSDFFont;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MSDF_VarFontMetrics
    {
        public uint AxisCount;
        public uint DesignCount;
        public uint NamedStyleCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MSDF_VarFontAxis
    {
        public sbyte* Name;
        public uint NameLength;

        public long Minimum;
        public long Default;
        public long Maximum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MSDF_VarFontStyle
    {
        public sbyte* Name;
        public uint NameLength;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MSDF_ShapedGlyph
    {
        public double BearingX;
        public double BearingY;

        public double Width;
        public double Height;

        public double Advance;
        public fixed byte Shape[40];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MSDF_RenderBitmap
    {
        public float* Pixels;

        public int Width;
        public int Height;

        public int RowStride;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MSDF_RenderBox
    {
        public int RectW;
        public int RectH;
        public fixed byte Range[16];
        public fixed byte Projection[32];
    }
}
