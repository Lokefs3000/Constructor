using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Text;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    public class UILabel : UIElement
    {
        private UIFontStyle? _fontStyle;

        private string _text;
        private float _size;

        private float? _lineHeight;
        private float _letterSpacing;

        private UITextAlignment _alignment;
        private UITextOverflow _overflow;

        private UITextAutoSize _autoSize;

        private UITextShapingData _shapingData;

        public UILabel()
        {
            _fontStyle = null;

            _text = string.Empty;
            _size = 16.0f;

            _lineHeight = null;
            _letterSpacing = 1.0f;

            _alignment = UITextAlignment.Left | UITextAlignment.Top;
            _overflow = UITextOverflow.Overflow;

            _autoSize = UITextAutoSize.None;

            _shapingData = new UITextShapingData();
        }

        public override UIRecalcLayoutStatus RecalculateLayout(UILayoutManager manager, UIRecalcType type)
        {
            if (_autoSize != UITextAutoSize.FitSizeToText)
                base.RecalculateLayout(manager, type);

            if (FlagUtility.HasFlag(InvalidFlags, UIInvalidationFlags.Layout) && _fontStyle != null)
            {
                manager.TextShaper.ShapeText(_shapingData, new UITextShapingInfo
                {
                    Style = _fontStyle,

                    Text = _text,
                    Size = _size,

                    LetterSpacing = _letterSpacing,
                    LineHeight = _lineHeight.GetValueOrDefault(_fontStyle.LineHeight * _size),
                    Overflow = _overflow,

                    MaxExtents = (_autoSize == UITextAutoSize.FitSizeToText) ? Vector2.PositiveInfinity : Transform.RenderCoordinates.Size
                });

                if (_autoSize == UITextAutoSize.FitSizeToText)
                {
                    Transform.Size = new UIValue2((int)_shapingData.TotalSize.X, (int)_shapingData.TotalSize.Y);
                }
            }

            if (Transform.HasChangedSinceLast)
                base.RecalculateLayout(manager, type);
            return UIRecalcLayoutStatus.Finished;
        }

        public UIFontStyle? FontStyle { get => _fontStyle; set { _fontStyle = value; InvalidateSelf(UIInvalidationFlags.Layout); } }

        public string Text { get => _text; set { _text = value; InvalidateSelf(UIInvalidationFlags.Layout); } }
        public float Size { get => _size; set { _size = value; InvalidateSelf(UIInvalidationFlags.Layout); } }

        public float? LineHeight { get => _lineHeight; set { _lineHeight = value; InvalidateSelf(UIInvalidationFlags.Layout); } }
        public float LetterSpacing { get => _letterSpacing; set { _letterSpacing = value; InvalidateSelf(UIInvalidationFlags.Layout); } }

        public UITextAlignment Alignment { get => _alignment; set { _alignment = value; InvalidateSelf(UIInvalidationFlags.Layout); } }
        public UITextOverflow Overflow { get => _overflow; set { _overflow = value; InvalidateSelf(UIInvalidationFlags.Layout); } }

        public UITextAutoSize AutoSize { get => _autoSize; set { _autoSize = value; InvalidateSelf(UIInvalidationFlags.Layout); } }

        public UITextShapingData ShapingData => _shapingData;
    }

    public enum UITextAlignment : byte
    {
        Left        = 0b000_001,
        Center      = 0b000_010,
        Right       = 0b000_001,

        Top         = 0b001_000,
        Middle      = 0b010_000,
        Bottom      = 0b011_000
    }

    public enum UITextOverflow : byte
    {
        Overflow = 0,
        Cull,
        WrapWords,
        WrapLetters
    }

    public enum UITextAutoSize : byte
    {
        None = 0,
        FitSizeToText = 1 << 0,
        FitTextToSize = 1 << 1,
    }
}
