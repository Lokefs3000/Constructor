using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Datatypes
{
    public struct UIColor
    {
        public UIColorType Type;

        public Color Solid;
        public UIGradientColor Gradient;

        public UIColor(Color color)
        {
            Type = UIColorType.Solid;
            Solid = color;
        }

        public UIColor(UIGradientColor gradient)
        {
            Type = UIColorType.Gradient;
            Gradient = gradient;
        }

        public static implicit operator UIColor(Color color) => new UIColor(color);
        public static implicit operator UIColor(UIGradientColor gradient) => new UIColor(gradient);

        public static explicit operator Color(UIColor color) => color.Type == UIColorType.Solid ? color.Solid : throw new InvalidCastException();
        public static explicit operator UIGradientColor(UIColor color) => color.Type == UIColorType.Gradient ? color.Gradient : throw new InvalidCastException();
    }

    public enum UIColorType : byte
    {
        Solid = 0,
        Gradient
    }
}
