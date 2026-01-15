using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Layout
{
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

        public void ModifyElement(IUILayoutModiferTime time)
        {
            if (_element.Children.Count > 0)
            {
                Boundaries boundaries = _element.Transform.RenderCoordinates;

                switch (_direction)
                {
                    case UIListLayoutDirection.Horizontal:
                        {
                            Vector2 parentSize = boundaries.Size;
                            Vector2 paddingAmount = _padding.Evaluate(parentSize);

                            Vector2 currentPosition = Vector2.Zero;
                            float maxWidth = 0.0f;

                            foreach (UIElement child in _element.Children)
                            {
                                Vector2 size = child.Transform.Size.Evaluate(parentSize);

                                child.Transform.Position = new UIValue2((int)currentPosition.X, (int)currentPosition.Y);

                                maxWidth = MathF.Max(maxWidth, size.X);

                                float nextPos = currentPosition.Y + size.Y + paddingAmount.Y;
                                if (nextPos > parentSize.Y)
                                {
                                    currentPosition.Y = 0.0f;
                                    currentPosition.X += maxWidth + paddingAmount.X;

                                    maxWidth = 0.0f;
                                }
                                else
                                    currentPosition.Y = nextPos;
                            }

                            break;
                        }
                    case UIListLayoutDirection.Vertical:
                        {
                            Vector2 parentSize = boundaries.Size;
                            Vector2 paddingAmount = _padding.Evaluate(parentSize);

                            Vector2 currentPosition = Vector2.Zero;
                            float maxHeight = 0.0f;

                            foreach (UIElement child in _element.Children)
                            {
                                Vector2 size = child.Transform.Size.Evaluate(parentSize);

                                child.Transform.Position = new UIValue2((int)currentPosition.X, (int)currentPosition.Y);

                                maxHeight = MathF.Max(maxHeight, size.Y);

                                float nextPos = currentPosition.X + size.X + paddingAmount.X;
                                if (nextPos > parentSize.Y)
                                {
                                    currentPosition.Y += maxHeight + paddingAmount.Y;
                                    currentPosition.X = 0.0f;

                                    maxHeight = 0.0f;
                                }
                                else
                                    currentPosition.X = nextPos;
                            }

                            break;
                        }
                }
            }
        }

        public UIListLayoutDirection Direction { get => _direction; set { _direction = value; _element.InvalidateSelf(UIInvalidationFlags.Layout); } }
        public UIListOverflow Overflow { get => _overflow; set { _overflow = value; _element.InvalidateSelf(UIInvalidationFlags.Layout); } }
        
        public UIValue2 Padding { get => _padding; set { _padding = value; _element.InvalidateSelf(UIInvalidationFlags.Layout); } }

        public IUILayoutModiferTime Timing => IUILayoutModiferTime.Descending;
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
