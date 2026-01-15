using Editor.UI.Datatypes;
using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Layout
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

        public void ModifyElement(IUILayoutModiferTime time)
        {
            if (time == IUILayoutModiferTime.Descending)
            {
                Vector2 calc = _margin.Evaluate(_element.GetParentRenderBoundaries().Size);
                if (calc.X != 0.0f || calc.Y != 0.0f)
                {
                    UIValue2 marginOffset = new UIValue2((int)calc.X, (int)calc.Y);
                    foreach (UIElement element in _element.Children)
                    {
                        element.Transform.Position += marginOffset;
                    }
                }
            }
            else if (time == IUILayoutModiferTime.Acending)
            {
                Vector2 maxExtents = Vector2.Zero;
                Vector2 calc = _margin.Evaluate(_element.GetParentRenderBoundaries().Size);

                foreach (UIElement child in _element.Children)
                {
                    maxExtents = Vector2.Max(maxExtents, child.Transform.RenderCoordinates.Maximum);
                }

                maxExtents = Vector2.Max(maxExtents - _element.Transform.RenderCoordinates.Minimum, Vector2.Zero) + calc;

                switch (_axis)
                {
                    case UIFitterAxis.Vertical: _element.Transform.Size = new UIValue2(_element.Transform.Size.X, new UIValue((int)maxExtents.Y)); break;
                    case UIFitterAxis.Horizontal: _element.Transform.Size = new UIValue2(new UIValue((int)maxExtents.X), _element.Transform.Size.Y); break;
                    case UIFitterAxis.Both: _element.Transform.Size = new UIValue2(new UIValue((int)maxExtents.X), new UIValue((int)maxExtents.Y)); break;
                }
            }
        }

        public IUILayoutModiferTime Timing => IUILayoutModiferTime.Acending | IUILayoutModiferTime.Descending;

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
