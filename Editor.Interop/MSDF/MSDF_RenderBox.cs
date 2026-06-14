using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_RenderBox
    {
        public int RectW;
        public int RectH;
        public MSDF_Range Range;
        public MSDF_Projection Projection;
    }
}
