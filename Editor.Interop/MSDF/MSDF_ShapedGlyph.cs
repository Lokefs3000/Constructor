using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_ShapedGlyph
    {
        public double BearingX;
        public double BearingY;

        public double Width;
        public double Height;

        public double Advance;
        public fixed byte Shape[40];
    }
}
