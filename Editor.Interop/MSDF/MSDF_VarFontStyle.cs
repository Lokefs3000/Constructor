using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Editor.Interop.MSDF
{
    public unsafe struct MSDF_VarFontStyle
    {
        public sbyte* Name;
        public uint NameLength;

        // name is encoded in Big Endian
        public readonly string GetNameAsString()
        {
            char* strBe = (char*)Name;
            Span<char> strLe = stackalloc char[(int)(NameLength / 2)];

            for (int k = 0; k < strLe.Length; k++)
                strLe[k] = (char)(((strBe[k] & 0xff) << 8) | ((strBe[k] & 0xff00) >> 8));

            return strLe.ToString();
        }
    }
}
