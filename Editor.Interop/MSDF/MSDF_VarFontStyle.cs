using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_VarFontStyle
    {
        public sbyte* Name;
        public uint NameLength;

        public readonly string GetNameAsString() => new string(Name, 0, (int)NameLength);
    }
}
