using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Mathematics.PackedVector;

namespace Editor.UI.Visual
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly record struct UIVertex(Vector2 Position, Vector2 UV, uint MetadataOffset);

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct StrokeMetadata
    {
        [FieldOffset(0)] public readonly Half4 Color;
        [FieldOffset(8)] public readonly Half Width;

        public StrokeMetadata(Half4 color, Half width)
        {
            Color = color;
            Width = width;
        }

        public const int LargestElement = 2;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct PointsMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;

        public PointsMetadata(bool hasStroke, Half4 color, ushort zIndex)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
        }

        public const int LargestElement = 2;
    }


    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct LinesMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;
        [FieldOffset(12)] public readonly Half LineWidth;

        public LinesMetadata(bool hasStroke, Half4 color, ushort zIndex, Half lineWidth)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
            LineWidth = lineWidth;
        }

        public const int LargestElement = 2;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct RectMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;
        [FieldOffset(12)] public readonly UShort2 BoxSize;

        public RectMetadata(bool hasStroke, Half4 color, ushort zIndex, UShort2 boxSize)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
            BoxSize = boxSize;
        }

        public const int LargestElement = 2;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct RoundedRectMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;
        [FieldOffset(12)] public readonly Half Radius;
        [FieldOffset(14)] public readonly UShort2 BoxSize;

        public RoundedRectMetadata(bool hasStroke, Half4 color, ushort zIndex, Half radius, UShort2 boxSize)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
            Radius = radius;
            BoxSize = boxSize;
        }

        public const int LargestElement = 2;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct CircleMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;
        [FieldOffset(12)] public readonly Half Radius;

        public CircleMetadata(bool hasStroke, Half4 color, ushort zIndex, Half radius)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
            Radius = radius;
        }

        public const int LargestElement = 2;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct TriangleMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;
        [FieldOffset(12)] public readonly Half2 A;
        [FieldOffset(16)] public readonly Half2 B;
        [FieldOffset(20)] public readonly Half2 C;

        public TriangleMetadata(bool hasStroke, Half4 color, ushort zIndex, Half2 a, Half2 b, Half2 c)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
            A = a;
            B = b;
            C = c;
        }

        public const int LargestElement = 2;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct ImageMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;
        [FieldOffset(12)] public readonly UShort2 BoxSize;

        public ImageMetadata(bool hasStroke, Half4 color, ushort zIndex, UShort2 boxSize)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
            BoxSize = boxSize;
        }

        public const int LargestElement = 2;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal readonly record struct TextMetadata
    {
        [FieldOffset(0)] public readonly ushort HasStroke;
        [FieldOffset(2)] public readonly Half4 Color;
        [FieldOffset(10)] public readonly ushort ZIndex;

        public TextMetadata(bool hasStroke, Half4 color, ushort zIndex)
        {
            HasStroke = Unsafe.As<bool, byte>(ref hasStroke);
            Color = color;
            ZIndex = zIndex;
        }

        public const int LargestElement = 2;
    }
}
