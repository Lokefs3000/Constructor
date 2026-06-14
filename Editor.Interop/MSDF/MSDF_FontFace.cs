using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_FontFace
    {
        public void* SourceFace;
        public void* ActiveFace;
    }
}
