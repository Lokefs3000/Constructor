using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Common;

namespace EditorUI.Built
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct BuiltColor
    {
        [FieldOffset(0)] public readonly BuiltColorType ColorType;
        [FieldOffset(1)] public readonly Color Solid;
        [FieldOffset(1)] public readonly int GradientIndex;

        public BuiltColor(Color color)
        {
            ColorType = BuiltColorType.Solid;
            Solid = color;
        }

        public BuiltColor(int gradientIndex)
        {
            ColorType = BuiltColorType.Gradient;
            GradientIndex = gradientIndex;
        }
    }

    public enum BuiltColorType : byte
    {
        Solid = 0,
        Gradient
    }
}
