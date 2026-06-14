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
    public class UIListLayout : BaseLayoutModifier
    {
        private UIListLayoutDirection _direction;
        private UIListOverflow _overflow;

        private UIValue _padding;

        public UIListLayout(UIElement element) : base(element)
        {
            _direction = UIListLayoutDirection.Vertical;
            _overflow = UIListOverflow.Overflow;

            _padding = UIValue.Zero;
        }

        public override void ModifyLayout(UILayoutContext context)
        {
            if (_element.Children.Count > 0)
            {
                Vector2 realSize = _element.CurrentSize;

                switch (_direction)
                {
                    case UIListLayoutDirection.Horizontal:
                        {
                            float paddingAmount = _padding.Evaluate(realSize.X);
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
                            float paddingAmount = _padding.Evaluate(realSize.Y);
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

        #region Properties
        [EditableProperty(nameof(_direction))] public UIListLayoutDirection Direction { get => _direction; set { _direction = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }
        [EditableProperty(nameof(_overflow))] public UIListOverflow Overflow { get => _overflow; set { _overflow = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }

        [EditableProperty(nameof(_padding))] public UIValue Padding { get => _padding; set { _padding = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }
        #endregion
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
