using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI
{
    [Flags]
    public enum StateFlags : byte
    {
        None = 0,

        InvalidStyle = 1 << 0,
        InvalidLayout = 1 << 1,

        ThisStyle = 1 << 4,
        ThisLayout = 1 << 5,

        // shorthands

        This = ThisStyle | ThisLayout,

        SelfInvalidStyle = InvalidStyle | ThisStyle,
        SelfInvalidLayout = InvalidLayout | ThisLayout
    }
}
