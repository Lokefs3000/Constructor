using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Data
{
    public readonly record struct ValueDataRef(int Code)
    {
        public ValueDataRef(ValueGeneric generic, int rows, int columns) : this(((int)generic) | (rows << 4) | (columns << 8)) { }
        public ValueDataRef(ValueGeneric generic, int index) : this(((int)ValueGeneric.Custom) | (index << 4)) { }

        public ValueGeneric Generic => (ValueGeneric)(Code & 0x7);

        public int Rows => (Code >> 4) & 0x7;
        public int Columns => (Code >> 8) & 0x7;

        public int Index => (Code & ~0x7) >> 4;

        public bool IsSpecified => Code < int.MaxValue;

        public override string ToString()
        {
            if (Generic == ValueGeneric.Custom)
                return "Custom";

            string genericWord = Generic.ToString().ToLower();
            if (Rows > 1)
            {
                if (Columns > 1)
                    return $"{genericWord}{Rows}x{Columns}";
                else
                    return $"{genericWord}{Rows}";
            }

            return genericWord;
        }

        public static readonly ValueDataRef Unspecified = new ValueDataRef(int.MaxValue);
    }
}
