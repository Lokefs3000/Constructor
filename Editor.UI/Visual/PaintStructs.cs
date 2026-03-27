using Editor.UI.Datatypes;
using Editor.UI.Helpers;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Visual
{
    internal readonly record struct PaintDrawSegment(int BaseCommandIndex, int MatrixId = -1, int ClipId = -1, UIBlendMode BlendMode = UIBlendMode.Undefined);
    internal readonly record struct RawPaintData(PaintColor Color, bool StrokeEnabled, float StrokeWidth, PaintColor StrokeColor, int CustomShaderIdx);

    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct PaintColor
    {
        [FieldOffset(0)] public readonly bool IsGradient;

        [FieldOffset(1)] public readonly Color Solid;
        [FieldOffset(1)] public readonly int GradientKey;

        public PaintColor(Color solid)
        {
            IsGradient = false;
            Solid = solid;
        }

        public PaintColor(int gradientKey)
        {
            IsGradient = true;
            GradientKey = gradientKey;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly record struct PaintCmd : IEquatable<PaintCmd>
    {
        [FieldOffset(0)] public readonly PaintType Type;

        [FieldOffset(1)] public readonly uint CommandSortIndex;

        [FieldOffset(3)] public readonly ushort ObjectIndex;
        [FieldOffset(1)] public readonly ushort ZIndex;

        [FieldOffset(5)] public readonly CmdPointsData PointsData;
        [FieldOffset(5)] public readonly CmdLinesData LinesData;
        [FieldOffset(5)] public readonly CmdRectData RectData;
        [FieldOffset(5)] public readonly CmdRoundedRectData RoundedRectData;
        [FieldOffset(5)] public readonly CmdCircleData CircleData;
        [FieldOffset(5)] public readonly CmdTriangleData TriangleData;
        [FieldOffset(5)] public readonly CmdImageData ImageData;
        [FieldOffset(5)] public readonly CmdTextData TextData;

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdPointsData data)
        {
            Type = PaintType.Points;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            PointsData = data;
        }

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdLinesData data)
        {
            Type = PaintType.Lines;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            LinesData = data;
        }

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdRectData data)
        {
            Type = PaintType.Rect;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            RectData = data;
        }

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdRoundedRectData data)
        {
            Type = PaintType.RoundedRect;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            RoundedRectData = data;
        }

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdCircleData data)
        {
            Type = PaintType.Circle;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            CircleData = data;
        }

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdTriangleData data)
        {
            Type = PaintType.Triangle;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            TriangleData = data;
        }

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdImageData data)
        {
            Type = PaintType.Image;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            ImageData = data;
        }

        public PaintCmd(ushort zIndex, ushort objectIndex, CmdTextData data)
        {
            Type = PaintType.Text;
            ZIndex = zIndex;
            ObjectIndex = objectIndex;
            TextData = data;
        }

        public bool Equals(PaintCmd other)
        {
            return Type switch
            {
                PaintType.Image => ImageData.ImageIdx == other.ImageData.ImageIdx,
                _ => true
            };
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    internal enum PaintType : byte
    {
        Points = 0,
        Lines,
        Rect,
        RoundedRect,
        Circle,
        Triangle,
        Image,
        Text
    }

    internal readonly record struct CmdPointsData(Ptr<Vector2> Points, int Count, RawPaintData Paint);
    internal readonly record struct CmdLinesData(Ptr<Vector2> Lines, UILineMode Mode, int Count, RawPaintData Paint, float LineWidth);
    internal readonly record struct CmdRectData(Boundaries Rect, RawPaintData Paint);
    internal readonly record struct CmdRoundedRectData(Boundaries Rect, RawPaintData Paint, float Radius);
    internal readonly record struct CmdCircleData(Vector2 Center, float Radius, RawPaintData Paint);
    internal readonly record struct CmdTriangleData(Vector2 A, Vector2 B, Vector2 C, RawPaintData Paint);
    internal readonly record struct CmdImageData(Boundaries Rect, RawPaintData Paint, Vector2 UVMin, Vector2 UVMax, int ImageIdx);
    internal readonly record struct CmdTextData(Vector2 Position, RawPaintData Paint, RawTextBuilderData Builder, int ShapedDataIdx);
}
