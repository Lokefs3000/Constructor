using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_VarFontAxis
    {
        public sbyte* Name;
        public uint NameLength;

        public long Minimum;
        public long Default;
        public long Maximum;

        public readonly string GetNameAsString() => new string(Name, 0, (int)NameLength);
    }
}
