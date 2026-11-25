using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Text
{
    public enum TextWrapMode : byte
    {
        Overflow = 0,
        Ellipsis,
        Wrap
    }

    [Flags]
    public enum TextAlignment : byte
    {
        Left = 0b01,
        Middle = 0b10,
        Right = 0b11,

        Top = 0b0100,
        Center = 0b1000,
        Bottom = 0b1100,

        TopLeft = Top | Left,
        TopMiddle = Top | Middle,
        TopRight = Top | Right,

        CenterLeft = Center | Left,
        CenterMiddle = Center | Middle,
        CenterRight = Center | Right,

        BottomLeft = Bottom | Left,
        BottomMiddle = Bottom | Middle,
        BottomRight = Bottom | Right,

        HorizontalAlignment = 0b11,
        VerticalAlignment = 0b1100
    }

    public enum FontStyle : byte
    {
        Normal = 0,
        Italic
    }

    public enum FontWeight : byte
    {
        w100 = 0,
        w200,
        w300,
        w400,
        w500,
        w600,
        w700,
        w800,
        w900,

        Light = w300,
        Normal = w400,
        Bold = w600,
        Bolder = w700
    }
}
