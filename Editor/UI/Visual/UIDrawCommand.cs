using Editor.UI.Datatypes;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Visual
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UIDrawCommand
    {
        [FieldOffset(0)]
        public UIDrawType Type;

        [FieldOffset(1)]
        public ushort CommandId;

        [FieldOffset(3)]
        public ushort ZIndex;

        [FieldOffset(5)]
        public UIDrawRectangle Rectangle;

        public UIDrawCommand(int commandId, int zIndex, UIDrawRectangle rectangle)
        {
            Type = UIDrawType.Rectangle;
            CommandId = (ushort)commandId;
            ZIndex = (ushort)zIndex;
            Rectangle = rectangle;
        }
    }

    public enum UIDrawType : byte
    {
        Rectangle = 0,
    }

    public struct UIDrawRectangle
    {
        public Boundaries DrawBounds;
        public UIDrawColor Color;

        public UIRoundedCorner CornersToRound;
        public float RoundingPerc;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UIDrawColor
    {
        [FieldOffset(0)]
        public UIColorType Type;

        [FieldOffset(1)]
        public Color RGBA;
        [FieldOffset(1)]
        public int GradientKey;

        public UIDrawColor(Color rgba)
        {
            Type = UIColorType.Solid;
            RGBA = rgba;
        }

        public UIDrawColor(int gradientKey)
        {
            Type = UIColorType.Gradient;
            GradientKey = gradientKey;
        }
    }

    public enum UIRoundedCorner : byte
    {
        TopLeft = 1 << 0,
        TopRight = 1 << 1,
        BottomLeft = 1 << 2,
        BottomRight = 1 << 3,

        All = 0xf,

        Left = TopLeft | BottomLeft,
        Right = TopRight | BottomRight,
        Top = TopLeft | TopRight,
        Bottom = BottomLeft | BottomRight
    }

    public enum UIStrokePosition : byte
    {
        Inside = 0,
        Center,
        Outside
    }
}
