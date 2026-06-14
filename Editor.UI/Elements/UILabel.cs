using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Helpers;
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
    [UIElementPrettyName("Label")]
    public class UILabel : UIElement
    {
        private UIFontAsset? _font;
        private FontStyle _style;
        private FontWeight _weight;

        private string _text;
        private float _fontSize;

        private UITextAlignment _alignment;
        private UITextOverflow _overflow;

        private UITextAutoSize _autoSize;

        private UIColor _textColor;

        public UILabel()
        {
            _font = null;
            _style = FontStyle.Normal;
            _weight = FontWeight.Normal;

            _text = string.Empty;
            _fontSize = 1.0f;

            _alignment = UITextAlignment.Left | UITextAlignment.Top;
            _overflow = UITextOverflow.Overflow;

            _autoSize = UITextAutoSize.None;

            _textColor = Color.Black;
        }

        public UILabel(UIElement parent) : base()
        {
            SetParent(parent);
        }

        public override void MeasureSize(UIMeasureContext context)
        {
            if (Flags.HasFlag(_autoSize, UITextAutoSize.FitBoundsToText))
            {
                if (_text.Length == 0 || _font == null)
                    _currentSize = Vector2.Zero;
                else
                {
                    TextVisualInfo visualInfo = new TextVisualInfo(new PaintColor(_textColor.Solid), _fontSize, _font.FindStyle(_style, _weight));
                    TextWrapInfo wrapInfo = new TextWrapInfo(TextOrigin.Top, context.LocalRegion, true, visualInfo);

                    using RentedArray<char> tempText = RentedArray<char>.Rent(_text.Length + 1);

                    _text.CopyTo(tempText.Span);
                    tempText.Span[_text.Length] = '\0';

                    UIManager manager = UIManager.Instance;
                    ShapedTextData textData = manager.TextManager.ShapeText(wrapInfo, _overflow, tempText.Span, _text.Length < 200 ? _text.GetDjb2HashCode() : StringHandle.InvalidHashCode);

                    _currentSize = textData.TotalSize;
                }

                _viewSize = Vector2.Zero;
                return;
            }

            base.MeasureSize(context);
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            if (_font != null)
            {
                UIFontTypeData? typeData = _font.FindStyle(_style, _weight);
                if (typeData != null)
                {
                    TextBuilder text = new TextBuilder();
                    text.SetAlignment(_alignment);
                    text.SetOverflow(_overflow);
                    text.SetMaxExtents(_viewSize);
                    text.SetOrigin(TextOrigin.Bottom);

                    Vector2 position = ViewCoordinates.Minimum;
                    position.Y -= typeData.Metrics.Ascender * _fontSize * TextManager.PixelsPerEM;

                    painter.DrawText(position, UIPaint.FromColor(_textColor), text, typeData, _fontSize, _text);
                }
            }

            return base.DrawVisual(painter);
        }

        #region Properties
        [StyleableProperty(nameof(_font), UIStateFlags.InvalidVisual)] public UIFontAsset? Font { get => _font; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_style), UIStateFlags.InvalidVisual)] public FontStyle FontStyle { get => _style; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_weight), UIStateFlags.InvalidVisual)] public FontWeight FontWeight { get => _weight; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_fontSize), UIStateFlags.InvalidVisual)] public float FontSize { get => _fontSize; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_textColor), UIStateFlags.InvalidVisual)] public UIColor TextColor { get => _textColor; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_alignment), UIStateFlags.InvalidVisual)] public UITextAlignment Alignment { get => _alignment; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_overflow), UIStateFlags.InvalidVisual)] public UITextOverflow Overflow { get => _overflow; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_autoSize), UIStateFlags.InvalidAll)] public UITextAutoSize AutoSize { get => _autoSize; set => SetStyleProperty(value); }

        [EditableProperty(nameof(_text), UIStateFlags.InvalidVisual)] public string Text { get => _text; set => SetEditableProperty(value); }
        #endregion
    }

    public enum UITextAlignment : byte
    {
        //Flags
        Left = 0b000_001,
        Center = 0b000_010,
        Right = 0b000_011,

        Top = 0b001_000,
        Middle = 0b010_000,
        Bottom = 0b011_000,

        //Defined
        TopLeft = Top | Left,
        TopCenter = Top | Center,
        TopRight = Top | Right,

        MiddleLeft = Middle | Left,
        MiddleCenter = Middle | Center,
        MiddleRight = Middle | Right,

        BottomLeft = Bottom | Left,
        BottomCenter = Bottom | Center,
        BottomRight = Bottom | Right,

        //Mask
        LCRMask = 0b000_011,
        TMBMask = 0b011_000
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
        FitBoundsToText = 1 << 0,
    }
}
