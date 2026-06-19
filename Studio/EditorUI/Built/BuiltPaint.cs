using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Common;
using EditorUI.Visual;
using EditorUI.Visual.Draw;

namespace EditorUI.Built
{
    public readonly record struct BuiltPaint(BuiltColor Fill, BuiltColor Stroke, ushort StrokeWidth, StrokePosition StrokePosition)
    {
        public static BuiltPaint Build(ref readonly Paint paint, GradientManager gradientManager)
        {
            return new BuiltPaint(
                paint.Fill.Type == ColorType.Solid ? new BuiltColor(paint.Fill.Solid) : new BuiltColor(gradientManager.GetGradientIndex(paint.Fill.Gradient)),
                paint.StrokeWidth > 0 ? (paint.Stroke.Type == ColorType.Solid ? new BuiltColor(paint.Stroke.Solid) : new BuiltColor(gradientManager.GetGradientIndex(paint.Stroke.Gradient))) : default,
                paint.StrokeWidth,
                paint.StrokePosition);
        }
    }
}
