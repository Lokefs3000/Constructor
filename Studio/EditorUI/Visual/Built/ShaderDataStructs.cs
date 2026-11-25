using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Mathematics.PackedVector;

namespace EditorUI.Visual.Built
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 12)]
    internal readonly struct SharedShaderData(ushort strokeWidth, Half4 strokeColor)
    {
        [FieldOffset(0)] public readonly ushort StrokeWidth = strokeWidth;
        [FieldOffset(2)] public readonly Half4 StrokeColor = strokeColor;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 12)]
    internal readonly struct LinesShaderData(SharedShaderData shared, Half thickness)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(10)] public readonly Half Thickness = thickness;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 24)]
    internal readonly struct RectangleShaderData(SharedShaderData shared, UShort2 boxSize, Half4 cornerRadii)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(10)] public readonly UShort2 BoxSize = boxSize;
        [FieldOffset(16)] public readonly Half4 CornerRadii = cornerRadii;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 20)]
    internal readonly struct TextShaderData(SharedShaderData shared, Vector2 maxExtents)
    {
        [FieldOffset(0)] public readonly SharedShaderData Shared = shared;
        [FieldOffset(12)] public readonly Vector2 MaxExtents = maxExtents;
    }
}
