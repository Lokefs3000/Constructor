using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Modifiers
{
    [ModifierPrettyName("ListLayout")]
    public class UIListLayout : IUILayoutModifier
    {
        private readonly UIElement _element;

        private UIListLayoutDirection _direction;
        private UIListOverflow _overflow;

        private UIValue2 _padding;

        public UIListLayout(UIElement element)
        {
            _direction = UIListLayoutDirection.Vertical;
            _overflow = UIListOverflow.Overflow;

            _padding = UIValue2.Zero;

            _element = element;
        }

        public void MeasureSize(UIMeasureContext context) { }

        public void ModifyElement(UILayoutContext context)
        {
            if (_element.Children.Count > 0)
            {
                Vector2 realSize = _element.CurrentSize;

                switch (_direction)
                {
                    case UIListLayoutDirection.Horizontal:
                        {
                            float paddingAmount = _padding.X.Evaluate(realSize.X);
                            float currentPosition = 0.0f;

                            foreach (UIElement child in _element.Children)
                            {
                                child.RelativeOffset = new Vector2(currentPosition, 0.0f);
                                currentPosition += child.CurrentSize.X + paddingAmount;
                            }

                            break;
                        }
                    case UIListLayoutDirection.Vertical:
                        {
                            float paddingAmount = _padding.Y.Evaluate(realSize.Y);
                            float currentPosition = 0.0f;

                            foreach (UIElement child in _element.Children)
                            {
                                child.RelativeOffset = new Vector2(0.0f, currentPosition);
                                currentPosition += child.CurrentSize.Y + paddingAmount;
                            }

                            break;
                        }
                }

                context.Measurements.MarkAsOutdated();
            }
        }

        public void DrawVisual(UIPainterContext painter) { }

        public UIListLayoutDirection Direction { get => _direction; set { _direction = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }
        public UIListOverflow Overflow { get => _overflow; set { _overflow = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }

        public UIValue2 Padding { get => _padding; set { _padding = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }
    }

    public enum UIListLayoutDirection : byte
    {
        Vertical = 0,
        Horizontal
    }

    public enum UIListOverflow : byte
    {
        Overflow = 0,
        Wrap
    }
}
