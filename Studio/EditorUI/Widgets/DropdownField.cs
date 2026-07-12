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
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Extensions;
using Serilog.Parsing;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class DropdownField : Button
    {
        private TextShapingData? _shapingData;
        private bool _hasBadShapingData;

        private Vector2 _textExtents;
        private Vector2 _lastIdealSize;

        protected IAssetProvider<FontFamily>? _fontFamily;
        protected FontStyle _fontStyle;
        protected FontWeight _fontWeight;

        protected float _fontSize;

        protected bool _allowRichText;

        protected UIColor _textColor;

        protected Vector4 _innerPadding;

        protected List<string> _options;
        protected int _index;

        public DropdownField()
        {
            _shapingData = null;
            _hasBadShapingData = false;

            _fontFamily = null;
            _fontStyle = FontStyle.Normal;
            _fontWeight = FontWeight.Normal;

            _fontSize = 1.0f;

            _allowRichText = true;

            _textColor = Color.White;

            _innerPadding = new Vector4(2.0f);

            _options = new List<string>();
            _index = 0;
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

            if (_fontFamily != null && !_fontFamily.IsReadyToUse)
            {
                return MeasureReturnData.MissingPendingData;
            }

            return MeasureReturnData.Success;
        }

        protected internal override void FinalizeSelf(ref readonly LayoutContext context)
        {
            _textExtents = Vector2.Max(Vector2.Zero, _idealSize - _innerPadding.GetLower() + _innerPadding.GetUpper());

            if (_fontFamily != null && _fontFamily.IsReadyToUse && (_lastIdealSize != _textExtents || _hasBadShapingData))
            {
                SetIdealSizeFor();
                _hasBadShapingData = false;
                _lastIdealSize = _textExtents;
            }
        }

        protected internal override void PaintSelf(ref PainterContext context)
        {
            base.PaintSelf(ref context);

            if (_options.Count > 0)
            {
                if (_fontFamily != null && _fontFamily.IsReadyToUse && !string.IsNullOrEmpty(_options[_index]))
                {
                    if (_shapingData == null && !_stateFlags.HasFlags(StateFlags.InvalidLayout))
                    {
                        SetIdealSizeFor();
                        _hasBadShapingData = false;
                    }

                    if (_shapingData != null)
                    {
                        context.AddText(
                            new Vector2(_computedRect.Minimum.X, _computedRect.Minimum.Y + _fontSize) + _innerPadding.GetLower(),
                            _shapingData,
                            _textExtents,
                            new Paint(_textColor));
                    }
                }
            }
        }

        private void SetIdealSizeFor()
        {
            Guard.IsNotNull(_fontFamily);
            
            if (_index < _options.Count)
            {
                TextManager textManager = UIManager.Instance.TextManager;
                TextBuilder textBuilder = new TextBuilder(_textExtents.X, TextWrapMode.Ellipsis, TextAlignment.CenterMiddle, _allowRichText);

                FontStyleData styleData = _fontFamily.Value!.GetFontStyle(_fontStyle, _fontWeight);
                textManager.GetOrShapeTextFor(this, ref _shapingData, _options[_index], BuiltTextBuilder.Build(in textBuilder), styleData, _fontSize);
            }
        }

        [StyleUpdateCallback(nameof(FontFamily), nameof(FontStyle), nameof(FontWeight), nameof(FontSize), nameof(AllowRichText),
            nameof(AutoResize), nameof(Size))]
        private void InvalidateCurrentTextData()
        {
            _hasBadShapingData = true;
            AddStateFlags(StateFlags.SelfInvalidLayout);
        }

        public void AddOption(string optionText)
        {
            _options.Add(optionText);
        }

        public void RemoveOption(string optionText)
        {
            int indexOf = _options.IndexOf(optionText);
            if (indexOf != -1)
            {
                RemoveOption(indexOf);
            }
        }
        
        public void RemoveOption(int index)
        {
            string oldOption = _options[index];

            _options.RemoveAt(index);

            if (_options.Count == 0)
            {
                if (_shapingData != null)
                {
                    TextManager textManager = UIManager.Instance.TextManager;
                    textManager.ForgetShapingDataFor(this);

                    _shapingData = null;
                    _hasBadShapingData = false;
                }
            }
            else if (_index >= _options.Count)
            {
                Index = _options.Count - 1;
            }
            else if (_index == index)
            {
                string newOption = _options[_index];

                if (!oldOption.SequenceEqual(newOption))
                {
                    InvalidateCurrentTextData();
                }
            }
        }

        public void ClearOptions()
        {
            _options.Clear();
            _index = 0;

            if (_shapingData != null)
            {
                TextManager textManager = UIManager.Instance.TextManager;
                textManager.ForgetShapingDataFor(this);

                _shapingData = null;
                _hasBadShapingData = false;
            }
        }

        public ROList<string> Options => _options;
        public int Index
        {
            get => _index;
            set
            {
                if (_options.Count == 0)
                    return;

                if (_index != value && (uint)_index < _options.Count)
                {
                    string oldOption = _options[_index];
                    string newOption = _options[value];

                    if (!oldOption.SequenceEqual(newOption))
                    {
                        InvalidateCurrentTextData();
                    }

                    _index = value;
                }
            }
        }

        #region Serializable
        [Styled(nameof(_fontFamily))] public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        [Styled(nameof(_fontStyle))] public FontStyle FontStyle { get => _fontStyle; set => SetStyledField(value); }
        [Styled(nameof(_fontWeight))] public FontWeight FontWeight { get => _fontWeight; set => SetStyledField(value); }

        [Styled(nameof(_fontSize))] public float FontSize { get => _fontSize; set => SetStyledField(value); }

        [Styled(nameof(_allowRichText))] public bool AllowRichText { get => _allowRichText; set => SetStyledField(value); }

        [Styled(nameof(_textColor))] public UIColor TextColor { get => _textColor; set => SetStyledField(value); }
        #endregion
    }
}
