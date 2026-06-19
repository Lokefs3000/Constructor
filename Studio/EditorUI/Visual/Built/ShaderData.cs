using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using EditorUI.Visual.Draw;
using Primary.Common;
using Primary.Mathematics;
using Vortice.Mathematics.PackedVector;

namespace EditorUI.Visual.Built
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 10)]
    internal readonly struct SharedShaderData(ushort strokeWidth, Half4 strokeColor)
    {
        [FieldOffset(0)] public readonly ushort StrokeWidth = strokeWidth;
        [FieldOffset(2)] public readonly Half4 StrokeColor = strokeColor;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 12)]
    internal readonly struct PointsShaderData(SharedShaderData shared, Half radius)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(10)] public readonly Half Radius = radius;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 12)]
    internal readonly struct LinesShaderData(SharedShaderData shared, Half thickness)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(10)] public readonly Half Thickness = thickness;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 24)]
    internal readonly struct RectangleShaderData(SharedShaderData shared, UShort2 boxSize, Half tL, Half tR, Half bL, Half bR)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(10)] public readonly UShort2 BoxSize = boxSize;
        [FieldOffset(14)] public readonly Half4 CornerRadii = new Half4(tL, tR, bL, bR);
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 24)]
    internal readonly struct CircleShaderData(SharedShaderData shared, Vector2 origin, Half radius)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(12)] public readonly Vector2 Origin = origin;
        [FieldOffset(20)] public readonly Half Radius = radius;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 40)]
    internal readonly struct TriangleShaderData(SharedShaderData shared, Vector2 a, Vector2 b, Vector2 c, Half cornerRadius)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(12)] public readonly Vector2 A = a;
        [FieldOffset(20)] public readonly Vector2 B = b;
        [FieldOffset(28)] public readonly Vector2 C = c;
        [FieldOffset(36)] public readonly Half CornerRadius = cornerRadius;
    }
}
