using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Interop.Ed
{
    public unsafe static partial class TexInterop
    {
        [LibraryImport(ModuleName)]
        public static partial void InitBC15Encoder();
        [LibraryImport(ModuleName)]
        public static partial void InitBC7Encoder();

        [LibraryImport(ModuleName)]
        public static partial void EncodeBC1(ref ImageBitmap bitmap, ref ImageOutput output);
        [LibraryImport(ModuleName)]
        public static partial void EncodeBC3(ref ImageBitmap bitmap, ref ImageOutput output);
        [LibraryImport(ModuleName)]
        public static partial void EncodeBC4(ref ImageBitmap bitmap, ref ImageOutput output);
        [LibraryImport(ModuleName)]
        public static partial void EncodeBC5(ref ImageBitmap bitmap, ref ImageOutput output);
        [LibraryImport(ModuleName)]
        public static partial void EncodeBC7(ref ImageBitmap bitmap, ref ImageOutput output);

        [LibraryImport(ModuleName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool LoadPNG(ref ImageLoadData imageData, ref ImageBitmap bitmap, sbyte** errorOutput);
        [LibraryImport(ModuleName)]
        public static partial void FreePNG(ref ImageBitmap bitmap);
        [LibraryImport(ModuleName)]
        public static partial void FreePNGError(sbyte* errorOutput);
        [LibraryImport(ModuleName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool QueryPNG(ref ImageLoadData imageData, ref ImageMetrics metrics, sbyte** errorOutput);
        [LibraryImport(ModuleName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool LoadJPEG(ref ImageLoadData imageData, ref ImageBitmap bitmap, sbyte** errorOutput);
        [LibraryImport(ModuleName)]
        public static partial void FreeJPEG(ref ImageBitmap bitmap);
        [LibraryImport(ModuleName)]
        public static partial void FreeJPEGError(sbyte* errorOutput);
        [LibraryImport(ModuleName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool QueryJPEG(ref ImageLoadData imageData, ref ImageMetrics metrics, sbyte** errorOutput);

        public const string ModuleName = "texinterop";

        public enum ImageFormat : byte
        {
            BC1 = 0,
            BC2,
            BC3,
            BC4,
            BC5,

            BC7
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ImageBitmap
        {
            public uint Width;
            public uint Height;

            public byte Stride;

            public byte* Pixels;
            public byte Effort;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ImageOutput
        {
            public uint BlocksX;
            public uint BlocksY;

            public byte* Pixels;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ImageLoadData
        {
            public byte* Data;
            public uint Length;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ImageMetrics
        {
            public uint Width;
            public uint Height;

            public byte Stride;
        }
    }
}
