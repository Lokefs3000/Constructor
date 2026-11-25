using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Modifiers
{
    public sealed class UIMenuBar : BaseLayoutModifier
    {
        private UIColor _fillColor;
        private UIColor _strokeColor;

        private float _strokeWeight;

        public UIMenuBar(UIElement element) : base(element)
        {
            _fillColor = Color.White;
            _strokeColor = new Color(0.5f);

            _strokeWeight = 1.0f;
        }

        public override void ModifyLayout(UILayoutContext context)
        {
            float height = MenuBarHeight + _strokeWeight;
            foreach (UIElement element in _element.Children)
            {
                element.RelativeOffset =
                    new Vector2(element.RelativeOffset.X, element.RelativeOffset.Y + height);
            }

            context.Measurements.MarkAsOutdated();
        }

        public UIColor FillColor { get => _fillColor; set { _fillColor = value; _element.AddStateFlags(UIStateFlags.InvalidVisual); } }
        public UIColor StrokeColor { get => _strokeColor; set { _strokeColor = value; _element.AddStateFlags(UIStateFlags.InvalidVisual); } }

        public float StrokeWeight { get => _strokeWeight; set { _strokeWeight = MathF.Max(value, 0.0f); _element.AddStateFlags(UIStateFlags.InvalidAll); } }

        private const float MenuBarHeight = 24.0f;
    }
}
