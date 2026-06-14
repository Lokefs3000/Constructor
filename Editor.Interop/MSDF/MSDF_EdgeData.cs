using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_EdgeData
    {
        public uint Type;
        public MSDF_Vector2* Points;
    }
}
