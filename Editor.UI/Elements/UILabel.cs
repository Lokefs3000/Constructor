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
        protected string? _fontStyle;

        protected UIFontStyle? _cachedFontStyle;

        private string _text;
        private float _fontSize;

        private UITextAlignment _alignment;
        private UITextOverflow _overflow;

        private UITextAutoSize _autoSize;

        private UIColor _textColor;

        public UILabel()
        {
            _font = null;
            _fontStyle = null;

            _cachedFontStyle = null;

            _text = string.Empty;
            _fontSize = 1.0f;

            _alignment = UITextAlignment.Left | UITextAlignment.Top;
            _overflow = UITextOverflow.Overflow;

            _autoSize = UITextAutoSize.None;

            _textColor = Color.Black;
        }

        public override void MeasureSize(UIMeasureContext context)
        {
            if (Flags.HasFlag(_autoSize, UITextAutoSize.FitBoundsToText))
            {
                if (_text.Length == 0)
                    _currentSize = Vector2.Zero;
                else
                {
                    if (_cachedFontStyle == null)
                    {
                        if (Flags.HasFlag(StateFlags, (UIStateFlags)ExtraUIStateFlags.InvalidStyle))
                        {
                            _cachedFontStyle = _font?.FindStyle(_fontStyle);
                            RemoveStateFlags((UIStateFlags)ExtraUIStateFlags.InvalidStyle);
                        }

                        if (_cachedFontStyle == null)
                        {
                            _currentSize = Vector2.Zero;
                            return;
                        }
                    }

                    TextVisualInfo visualInfo = new TextVisualInfo(new PaintColor(_textColor.Solid), _fontSize, _cachedFontStyle);
                    TextWrapInfo wrapInfo = new TextWrapInfo(context.LocalRegion, visualInfo);

                    using RentedArray<char> tempText = RentedArray<char>.Rent(_text.Length + 1);

                    _text.CopyTo(tempText.Span);
                    tempText.Span[_text.Length] = '\0';

                    UIManager manager = UIManager.Instance;
                    ShapedTextData textData = manager.TextManager.ShapeText(wrapInfo, _overflow, tempText.Span, _text.Length < 200 ? _text.GetDjb2HashCode() : StringHandle.InvalidHashCode);

                    _currentSize = textData.TotalSize;
                }

                return;
            }
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            if (_font != null)
            {
                if (Flags.HasFlag(StateFlags, (UIStateFlags)ExtraUIStateFlags.InvalidStyle))
                {
                    _cachedFontStyle = _font?.FindStyle(_fontStyle);
                    RemoveStateFlags((UIStateFlags)ExtraUIStateFlags.InvalidStyle);
                }

                if (_cachedFontStyle != null)
                {
                    TextBuilder text = new TextBuilder();
                    text.SetAlignment(_alignment);
                    text.SetOverflow(_overflow);
                    text.SetMaxExtents(PixelCoordinates.Size);

                    painter.DrawText(PixelCoordinates.Minimum, UIPaint.FromColor(_textColor), text, _cachedFontStyle, _fontSize, _text);
                }
            }
            else
                _fontStyle = null;

            return base.DrawVisual(painter);
        }

        #region Properties
        [StyleableProperty(nameof(_font), UIStateFlags.InvalidVisual | (UIStateFlags)ExtraUIStateFlags.InvalidStyle)] public UIFontAsset? Font { get => _font; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_fontStyle), UIStateFlags.InvalidVisual | (UIStateFlags)ExtraUIStateFlags.InvalidStyle)] public string? FontStyle { get => _fontStyle; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_fontSize), UIStateFlags.InvalidVisual)] public float FontSize { get => _fontSize; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_textColor), UIStateFlags.InvalidVisual)] public UIColor TextColor { get => _textColor; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_alignment), UIStateFlags.InvalidVisual)] public UITextAlignment Alignment { get => _alignment; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_overflow), UIStateFlags.InvalidVisual)] public UITextOverflow Overflow { get => _overflow; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_autoSize), UIStateFlags.InvalidAll)] public UITextAutoSize AutoSize { get => _autoSize; set => SetStyleProperty(value); }

        [EditableProperty(nameof(_text), UIStateFlags.InvalidVisual)] public string Text { get => _text; set => SetEditableProperty(value); }
        #endregion

        private enum ExtraUIStateFlags : byte
        {
            InvalidStyle = 1 << 4
        }
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
