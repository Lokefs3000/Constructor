using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Layout;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("MenuItem")]
    public class UIMenuItem : UIFrame
    {
        private UIFontAsset? _font;
        protected string? _fontStyle;

        protected UIFontStyle? _cachedFontStyle;

        protected string _text;

        protected UIColor _menuColor;
        protected UIColor _textColor;

        protected float _menuCornerRadius;
        protected RectCorner _menuCorners;

        protected UIColor _menuStrokeColor;
        protected float _menuStrokeWeight;

        public UIMenuItem()
        {
            _font = null;
            _fontStyle = null;

            _cachedFontStyle = null;

            _text = string.Empty;

            _menuColor = Color.White;
            _textColor = Color.Black;

            _menuCornerRadius = 0.0f;
            _menuCorners = RectCorner.All;

            _menuStrokeColor = Color.Black;
            _menuStrokeWeight = 1.0f;
        }

        public override void MeasureSize(UIMeasureContext context)
        {
            if (Parent is UIMenuBar)
            {
                _currentSize = new Vector2(EdgePadding * 2.0f, context.LocalRegion.Y - EdgePadding * 4.0f);
            }
            else
            {
                _currentSize = new Vector2(EdgePadding * 2.0f, EdgePadding * 2.0f + 16.0f);
            }
        }

        public override void RecalculateLayout(UILayoutContext context)
        {
            float currentOffset = ItemPadding;

            foreach (UIMenuItem menuItem in Children)
            {
                menuItem.RelativeOffset = new Vector2(ItemPadding, currentOffset);
                currentOffset += menuItem.CurrentSize.Y + ItemPadding;
            }
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            if (Flags.HasFlag(StateFlags, (UIStateFlags)ExtraUIStateFlags.InvalidStyle))
            {
                _cachedFontStyle = _font?.FindStyle(_fontStyle);
                RemoveStateFlags((UIStateFlags)ExtraUIStateFlags.InvalidStyle);
            }

            UIPaint paint = UIPaint.FromColor(_backgroundColor);
            if (_strokeWeight > 0.0f)
            {
                paint.SetStroke(true);
                paint.SetStrokeColor(_strokeColor);
                paint.SetStrokeWidth(_strokeWeight);
            }

            painter.DrawRect(_pixelCoordinates, paint, _cornerRadius);

            if (_cachedFontStyle != null)
            {
                float fontSize = ((_pixelCoordinates.Maximum.Y - _pixelCoordinates.Minimum.Y) - EdgePadding * 2.0f) / TextManager.PixelsPerEM;
                painter.DrawText(_pixelCoordinates.Minimum + new Vector2(EdgePadding), UIPaint.FromColor(_textColor), TextBuilder.Default, _cachedFontStyle, fontSize, _text);
            }

            return base.DrawVisual(painter);
        }

        #region Properties
        [StyleableProperty(nameof(_font), UIStateFlags.InvalidVisual | (UIStateFlags)ExtraUIStateFlags.InvalidStyle)] public UIFontAsset? Font { get => _font; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_fontStyle), UIStateFlags.InvalidVisual | (UIStateFlags)ExtraUIStateFlags.InvalidStyle)] public string? FontStyle { get => _fontStyle; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_textColor), UIStateFlags.InvalidVisual)] public UIColor TextColor { get => _textColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_menuColor), UIStateFlags.InvalidVisual)] public UIColor MenuColor { get => _menuColor; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_menuCornerRadius), UIStateFlags.InvalidVisual)] public float MenuCornerRadius { get => _menuCornerRadius; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_menuCorners), UIStateFlags.InvalidVisual)] public RectCorner MenuCorners { get => _menuCorners; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_menuStrokeColor), UIStateFlags.InvalidVisual)] public UIColor MenuStrokeColor { get => _menuStrokeColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_menuStrokeWeight), UIStateFlags.InvalidVisual)] public float MenuStrokeWeight { get => _menuStrokeWeight; set => SetStyleProperty(value); }

        [EditableProperty(nameof(_text), UIStateFlags.InvalidVisual)] public string Text { get => _text; set => SetEditableProperty(value); }
        #endregion

        public const float ItemPadding = 2.0f;
        public const float EdgePadding = 2.0f;

        private enum ExtraUIStateFlags : byte
        {
            InvalidStyle = 1 << 4
        }
    }
}
