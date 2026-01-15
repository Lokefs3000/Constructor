using Editor.UI.Elements;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Datatypes
{
    public sealed class UITransform
    {
        private readonly UIElement? _owner;

        private UITransformAlign _align;

        private UIValue2 _offset;
        private UIValue2 _size;

        private Vector2 _anchor;

        private Boundaries _renderCoordinates;

        private bool _hasChangedSinceLast;

        public UITransform(UIElement? owner)
        {
            _owner = owner;

            _offset = UIValue2.Zero;
            _size = UIValue2.Zero;

            _anchor = Vector2.Zero;

            _renderCoordinates = Boundaries.Zero;

            _hasChangedSinceLast = false;
        }

        public bool Recalculate(Boundaries parentCoords)
        {
            if (_hasChangedSinceLast)
            {
                _hasChangedSinceLast = false;

                Vector2 parentSize = parentCoords.Size;

                Vector2 basePosition = _offset.Evaluate(parentSize);
                Vector2 baseSize = _size.Evaluate(parentSize);

                UITransformAlign horizontalAlign = (UITransformAlign)((int)_align & AlignHorizontalMask);
                UITransformAlign verticalAlign = (UITransformAlign)((int)_align & AlignVerticalMask);

                switch (horizontalAlign)
                {
                    case UITransformAlign.Left: break;
                    case UITransformAlign.Middle: basePosition.X += (parentSize.X - baseSize.X) * 0.5f; break;
                    case UITransformAlign.Right: basePosition.X += parentSize.X; break;
                    case UITransformAlign.FillHorizontal: baseSize.X -= basePosition.X; break;
                }

                switch (verticalAlign)
                {
                    case UITransformAlign.Top: break;
                    case UITransformAlign.Center: basePosition.Y += (parentSize.Y - baseSize.Y) * 0.5f; break;
                    case UITransformAlign.Bottom: basePosition.Y += parentSize.Y; break;
                    case UITransformAlign.FillVertical: baseSize.Y -= basePosition.Y; break;
                }

                if (_anchor.X != 0.0f && horizontalAlign != UITransformAlign.FillHorizontal)
                    basePosition.X += _anchor.X * baseSize.X;
                if (_anchor.X != 0.0f && verticalAlign != UITransformAlign.FillVertical)
                    basePosition.Y += _anchor.Y * baseSize.Y;

                basePosition += parentCoords.Minimum;
                baseSize += basePosition;

                _renderCoordinates = new Boundaries(basePosition, baseSize);

                return true;
            }

            return false;
        }

        public UITransformAlign Align { get => _align; set { _align = value; _owner?.InvalidateSelf(UIInvalidationFlags.Layout); _hasChangedSinceLast = true; } }

        public UIValue2 Position { get => _offset; set { _offset = value; _owner?.InvalidateSelf(UIInvalidationFlags.Layout); _hasChangedSinceLast = true; } }
        public UIValue2 Size { get => _size; set { _size = value; _owner?.InvalidateSelf(UIInvalidationFlags.Layout); _hasChangedSinceLast = true; } }

        public Vector2 Anchor { get => _anchor; set { _anchor = value; _owner?.InvalidateSelf(UIInvalidationFlags.Layout); _hasChangedSinceLast = true; } }

        public Boundaries RenderCoordinates => _renderCoordinates;

        public bool HasChangedSinceLast => _hasChangedSinceLast;

        public const int AlignHorizontalMask = 0b000111;
        public const int AlignVerticalMask = 0b111000;
    }

    public enum UITransformAlign : byte
    {
        Left = 0b000001,
        Middle = 0b000010,
        Right = 0b000011,

        Top = 0b001000,
        Center = 0b010000,
        Bottom = 0b011000,

        FillHorizontal = 0b000100,
        FillVertical = 0b100000,

        Fill = FillHorizontal | FillVertical,
    }
}
