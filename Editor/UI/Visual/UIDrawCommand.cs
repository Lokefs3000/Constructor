using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Visual
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UIDrawCommand : IEquatable<UIDrawCommand>
    {
        [FieldOffset(0)]
        public UIDrawType Type;

        [FieldOffset(1)]
        public ushort CommandId;

        [FieldOffset(3)]
        public ushort ZIndex;

        [FieldOffset(5)]
        public UIDrawRectangle Rectangle;

        [FieldOffset(5)]
        public UIDrawTriangle Triangle;

        [FieldOffset(5)]
        public UIDrawCircle Circle;

        [FieldOffset(5)]
        public UIDrawSimpleText SimpleText;

        public UIDrawCommand(int commandId, int zIndex, UIDrawRectangle rectangle)
        {
            Type = UIDrawType.Rectangle;
            CommandId = (ushort)commandId;
            ZIndex = (ushort)zIndex;
            Rectangle = rectangle;
        }

        public UIDrawCommand(int commandId, int zIndex, UIDrawTriangle rectangle)
        {
            Type = UIDrawType.Triangle;
            CommandId = (ushort)commandId;
            ZIndex = (ushort)zIndex;
            Triangle = rectangle;
        }

        public UIDrawCommand(int commandId, int zIndex, UIDrawCircle rectangle)
        {
            Type = UIDrawType.Circle;
            CommandId = (ushort)commandId;
            ZIndex = (ushort)zIndex;
            Circle = rectangle;
        }

        public UIDrawCommand(int commandId, int zIndex, UIDrawSimpleText simpleText)
        {
            Type = UIDrawType.SimpleText;
            CommandId = (ushort)commandId;
            ZIndex = (ushort)zIndex;
            SimpleText = simpleText;
        }

        public bool Equals(UIDrawCommand other)
        {
            return (Type == other.Type) && (Type switch
            {
                UIDrawType.Rectangle => Rectangle.Equals(other.Rectangle),
                UIDrawType.Triangle => Triangle.Equals(other.Triangle),
                UIDrawType.Circle => Circle.Equals(other.Circle),

                UIDrawType.ShapedText => throw new NotImplementedException(),
                UIDrawType.SimpleText => SimpleText.Equals(other.SimpleText),

                _ => true,
            });
        }
    }

    public enum UIDrawType : byte
    {
        Rectangle = 0,
        Triangle,
        Circle,

        ShapedText,
        SimpleText
    }

    public struct UIDrawRectangle : IEquatable<UIDrawRectangle>
    {
        public Boundaries DrawBounds;
        public UIDrawColor Color;

        public float InfillWidth;

        public UIRoundedCorner CornersToRound;
        public float Rounding;

        public bool Equals(UIDrawRectangle other) => true;
    }

    public struct UIDrawTriangle : IEquatable<UIDrawTriangle>
    {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;
        public UIDrawColor Color;

        public float InfillWidth;

        public float Rounding;

        public bool Equals(UIDrawTriangle other) => true;
    }

    public struct UIDrawCircle : IEquatable<UIDrawCircle>
    {
        public Vector2 Center;
        public float Radius;
        public UIDrawColor Color;

        public float InfillRadius;

        public bool Equals(UIDrawCircle other) => true;
    }

    public struct UIDrawShapedText
    {

    }

    public struct UIDrawSimpleText : IEquatable<UIDrawSimpleText>
    {
        public Vector2 Position;
        public UIDrawColor Color;

        public int FontStyleIndex;

        public StringHandle Text;
        public float TextScale;

        public bool Equals(UIDrawSimpleText other) => FontStyleIndex == other.FontStyleIndex;
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

        None = 0,
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
