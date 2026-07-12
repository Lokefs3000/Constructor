using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Assets;
using EditorUI.Built;
using EditorUI.Common;
using EditorUI.Layout;
using EditorUI.Text;
using EditorUI.Visual;
using Primary.Common;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class Label : Widget
    {
        private TextShapingData? _shapingData;
        private bool _hasBadShapingData;

        private Vector2 _lastIdealSize;

        protected IAssetProvider<FontFamily>? _fontFamily;
        protected FontStyle _fontStyle;
        protected FontWeight _fontWeight;

        protected float _fontSize;

        protected TextWrapMode _wrapMode;
        protected TextAlignment _alignment;

        protected bool _allowRichText;

        protected UIColor _textColor;

        protected string? _text;

        public Label()
        {
            _shapingData = null;
            _hasBadShapingData = false;

            _fontFamily = null;
            _fontStyle = FontStyle.Normal;
            _fontWeight = FontWeight.Normal;

            _fontSize = 1.0f;

            _wrapMode = TextWrapMode.Overflow;
            _alignment = TextAlignment.TopLeft;

            _allowRichText = true;

            _textColor = Color.White;

            _text = null;
        }

        protected internal override void DestroySelf()
        {
            TextManager textManager = UIManager.Instance.TextManager;
            textManager.ForgetShapingDataFor(this);

            base.DestroySelf();
        }

        protected internal override MeasureReturnData MeasureSelf(ref readonly LayoutContext context)
        {
            base.MeasureSelf(in context);

            _hasBadShapingData = _hasBadShapingData || _lastIdealSize != _idealSize;

            // cannot decide wrapping dimensions if this has any kind of auto resize in the X direction
            if (_hasBadShapingData && _autoResize != AutoResizeMode.ResizeX && _autoResize != AutoResizeMode.ResizeXY)
            {
                if (_fontFamily != null)
                {
                    if (Vector2.EqualsAny(_idealSize, Vector2.Zero))
                    {
                        if (!_fontFamily.IsReadyToUse)
                            return MeasureReturnData.MissingPendingData;

                        SetIdealSizeFor(context.LayoutLock);
                    }
                    else
                    {
                        if (!_fontFamily.IsReadyToUse)
                            return MeasureReturnData.MissingPendingData;

                        SetIdealSizeFor(LayoutLockAxis.AxisXY);
                    }
                }

                _hasBadShapingData = false;
            }

            return MeasureReturnData.Success;
        }

        protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            _lastIdealSize = _idealSize;
            return base.LayoutSelf(in context);
        }

        protected internal override void FinalizeSelf(ref readonly LayoutContext context)
        {
            if (_hasBadShapingData)
            {
                SetIdealSizeFor(context.LayoutLock);
                _hasBadShapingData = false;
            }

        }

        protected internal override void PaintSelf(ref PainterContext context)
        {
            base.PaintSelf(ref context);

            if (_fontFamily != null && _fontFamily.IsReadyToUse && !string.IsNullOrEmpty(_text))
            {
                if (_shapingData == null && !_stateFlags.HasFlags(StateFlags.InvalidLayout))
                {
                    // don't set any axis we only want to get the shaping data
                    SetIdealSizeFor(LayoutLockAxis.AxisXY);

                    _hasBadShapingData = false;
                }

                if (_shapingData != null)
                {
                    context.AddText(
                        new Vector2(_computedRect.Minimum.X, _computedRect.Minimum.Y + _fontSize),
                        _shapingData,
                        _computedRect.Size,
                        new Paint(_textColor));
                }
            }
        }

        [MemberNotNull(nameof(_shapingData))]
        private void SetIdealSizeFor(LayoutLockAxis lockAxis)
        {
            Guard.IsNotNull(_fontFamily);

            TextManager textManager = UIManager.Instance.TextManager;
            TextBuilder textBuilder = new TextBuilder(_idealSize.X <= 0.0f ? float.PositiveInfinity : _idealSize.X, _wrapMode, _alignment, _allowRichText);

            FontStyleData styleData = _fontFamily.Value!.GetFontStyle(_fontStyle, _fontWeight);
            textManager.GetOrShapeTextFor(this, ref _shapingData, _text, BuiltTextBuilder.Build(in textBuilder), styleData, _fontSize);

            if (!lockAxis.HasFlags(LayoutLockAxis.AxisX) && !float.IsNegative(_idealSize.X) && _idealSize.X == 0.0f)
                _idealSize.X = _shapingData.TotalSize.X;
            if (!lockAxis.HasFlags(LayoutLockAxis.AxisY) && !float.IsNegative(_idealSize.Y) && _idealSize.Y == 0.0f)
                _idealSize.Y = _shapingData.TotalSize.Y;
        }

        [StyleUpdateCallback(nameof(FontFamily), nameof(FontStyle), nameof(FontWeight), nameof(FontSize), nameof(WrapMode),
            nameof(Alignment), nameof(AllowRichText), nameof(Text), nameof(AutoResize), nameof(Size))]
        private void InvalidateCurrentTextData()
        {
            _hasBadShapingData = true;
            AddStateFlags(StateFlags.SelfInvalidLayout);
        }

        #region Serializable
        [Styled(nameof(_fontFamily))] public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        [Styled(nameof(_fontStyle))] public FontStyle FontStyle { get => _fontStyle; set => SetStyledField(value); }
        [Styled(nameof(_fontWeight))] public FontWeight FontWeight { get => _fontWeight; set => SetStyledField(value); }

        [Styled(nameof(_fontSize))] public float FontSize { get => _fontSize; set => SetStyledField(value); }

        [Styled(nameof(_wrapMode))] public TextWrapMode WrapMode { get => _wrapMode; set => SetStyledField(value); }
        [Styled(nameof(_alignment))] public TextAlignment Alignment { get => _alignment; set => SetStyledField(value); }

        [Styled(nameof(_allowRichText))] public bool AllowRichText { get => _allowRichText; set => SetStyledField(value); }

        [Styled(nameof(_textColor))] public UIColor TextColor { get => _textColor; set => SetStyledField(value); }

        [Styled(nameof(_text), isEditable: true)] public string? Text { get => _text; set => SetEditedField(value); }
        #endregion
    }
}
