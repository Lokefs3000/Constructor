using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Text;

namespace Editor.UI.Modifiers
{
    public class UIFitterLayout : IUILayoutModifier
    {
        private readonly UIElement _element;

        private UIFitterAxis _axis;
        private UIValue2 _margin;

        public UIFitterLayout(UIElement element)
        {
            _element = element;

            _axis = UIFitterAxis.None;
            _margin = UIValue2.Zero;
        }

        public void MeasureSize(Vector2 treeSize)
        {
            Vector2 calc2x = _margin.Evaluate(_element.Transform.RealSize) * 2.0f;
            switch (_axis)
            {
                case UIFitterAxis.Vertical: _element.Transform.RealSize = new Vector2(_element.Transform.RealSize.X, treeSize.Y + calc2x.Y); break;
                case UIFitterAxis.Horizontal: _element.Transform.RealSize = new Vector2(treeSize.X + calc2x.X, _element.Transform.RealSize.Y); break;
                case UIFitterAxis.Both: _element.Transform.RealSize = treeSize + calc2x; break;
            }
        }

        public void ModifyElement(ref UIMeasurements measurements)
        {
            Vector2 calc = _margin.Evaluate(_element.Transform.RealSize);
            if (calc.X != 0.0f || calc.Y != 0.0f)
            {
                foreach (UIElement element in _element.Children)
                {
                    element.Transform.RelativePosition += calc;
                }
            }

            measurements.MarkAsOutdated();
        }

        public void DrawVisual(UICommandBuffer commandBuffer) { }

        public UIFitterAxis Axis { get => _axis; set {  _axis = value; _element.InvalidateSelf(UIInvalidationFlags.Layout); } }
        public UIValue2 Margin { get => _margin; set { _margin = value; _element.InvalidateSelf(UIInvalidationFlags.Layout); } }
    }

    public enum UIFitterAxis : byte
    {
        None = 0,

        Vertical = 1 << 0,
        Horizontal = 1 << 1,

        Both = Vertical | Horizontal
    }
}
