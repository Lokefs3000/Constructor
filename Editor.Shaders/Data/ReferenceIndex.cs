using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Shaders.Data
{
    public readonly record struct ReferenceIndex(uint Data)
    {
        public ReferenceIndex(ReferenceType type, int index) : this((uint)((int)type | index)) { }

        public ReferenceType Type => (ReferenceType)(Data & (0x3 << 30));
        public int Index => (int)(Data & ~(0x3 << 30));

        public override int GetHashCode() => (int)Data;
        public override string ToString() => $"{Type}[{Index}]";
    }

    public enum ReferenceType : int
    {
        Function = 0,
        Resource = 1 << 30,
        Property = 1 << 31
    }
}
