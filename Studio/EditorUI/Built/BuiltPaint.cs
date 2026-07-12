using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Common;
using EditorUI.Visual;

namespace EditorUI.Built
{
    public readonly record struct BuiltPaint(BuiltColor Fill, BuiltColor Stroke, ushort StrokeWidth, StrokePosition StrokePosition)
    {
        public bool IsTransparent
        {
            get
            {
                if (Fill.ColorType == BuiltColorType.Solid)
                {
                    if (Fill.Solid.A < 1.0f)
                        return true;
                }
                else
                {
                    return true;
                }

                if (StrokeWidth > 0)
                {
                    if (Stroke.ColorType == BuiltColorType.Solid)
                    {
                        if (Stroke.Solid.A < 1.0f)
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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
