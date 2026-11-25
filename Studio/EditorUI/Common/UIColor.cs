using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Common;

namespace EditorUI.Common
{
    public record struct UIColor
    {
        private readonly ColorType _type;

        private Color _solid;
        private GradientColor _gradient;

        public UIColor(Color color)
        {
            _type = ColorType.Solid;

            _solid = color;
            _gradient = default;
        }

        public UIColor(GradientColor gradient)
        {
            _type = ColorType.Gradient;

            _solid = default;
            _gradient = gradient;
        }

        public readonly ColorType Type => _type;

        [UnscopedRef] public ref Color Solid => ref _solid;
        [UnscopedRef] public ref GradientColor Gradient => ref _gradient;

        public readonly bool IsVisible => _type == ColorType.Solid ? _solid.A > 0.0f : (_gradient.Keys.Length == 1 ? _gradient.Keys[0].Color.A > 0.0f : true);

        public static implicit operator UIColor(Color color) => new UIColor(color);
        public static implicit operator UIColor(GradientColor gradient) => new UIColor(gradient);
    }

    public enum ColorType : byte
    {
        Solid = 0,
        Gradient
    }
}
