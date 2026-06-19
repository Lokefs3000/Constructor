using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Built;
using Primary.Mathematics;

namespace EditorUI.Visual.Draw
{
    public enum PaintCmdType : byte
    {
        Points = 0,
        Lines,
        Rectangle,
        Circle,
        Triangle,
        Text,
        Image,

        PushClipRect,
        PopClipRect
    }

    public struct PointsPaintCmd
    {
        public PaintCmdType CmdType;

        public BuiltPaint Paint;
        public Half Radius;
        public ushort PointCount;
    }
    
    public struct LinesPaintCmd
    {
        public PaintCmdType CmdType;

        public BuiltPaint Paint;
        public Half Thickness;
        public LinePaintMode PaintMode;
        public ushort LineCount;
    }

    public struct RectanglePaintCmd
    {
        public PaintCmdType CmdType;

        public BuiltPaint Paint;
        public Boundaries Rect;

        public Half CornerRadiusTL;
        public Half CornerRadiusTR;
        public Half CornerRadiusBL;
        public Half CornerRadiusBR;
    }

    public struct ImagePaintCmd
    {
        public PaintCmdType CmdType;

        public BuiltPaint Paint;
        public Boundaries Rect;

        public int ImageIndex;
        public Boundaries UVs;
    }

    public struct CirclePaintCmd
    {
        public PaintCmdType CmdType;

        public BuiltPaint Paint;
        public Half Radius;
        public Vector2 Center;
    }

    public struct TrianglePaintCmd
    {
        public PaintCmdType CmdType;

        public BuiltPaint Paint;
        public Half CornerRadius;
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;
    }

    public struct TextPaintCmd
    {
        public PaintCmdType CmdType;

        public BuiltPaint Paint;
        public BuiltTextBuilder TextBuilder;
        public ushort FontFamilyIndex;
        public Vector2 Position;
        public int TextLength;
    }

    public struct PushClipRectCmd
    {
        public PaintCmdType CmdType;
        public Rect Rect;
    }
}
