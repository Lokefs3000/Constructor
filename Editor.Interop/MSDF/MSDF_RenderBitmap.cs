using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_RenderBitmap
    {
        public float* Pixels;

        public int Width;
        public int Height;

        public int RowStride;
    }
}
