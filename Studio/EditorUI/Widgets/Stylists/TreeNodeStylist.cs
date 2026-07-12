using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Assets;
using EditorUI.Common;
using EditorUI.Text;
using Primary.Common;
using Serilog.Parsing;

namespace EditorUI.Widgets.Stylists
{
    [UIWidget]
    public class TreeNodeStylist : Stylist
    {
        protected IAssetProvider<FontFamily>? _fontFamily;
        protected FontStyle _fontStyle;
        protected FontWeight _fontWeight;

        protected bool _allowRichText;

        protected float _fontSize;

        protected UIColor _backgroundColor;
        protected UIColor _textColor;

        public TreeNodeStylist(Widget parent) : base(parent)
        {
            _fontFamily = null;
            _fontStyle = FontStyle.Normal;
            _fontWeight = FontWeight.Normal;

            _allowRichText = false;

            _fontSize = 16.0f;

            _backgroundColor = Color.White;
            _textColor = Color.White;
        }

        #region Serializable
        [Styled(nameof(_fontFamily))] public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        [Styled(nameof(_fontStyle))] public FontStyle FontStyle { get => _fontStyle; set => SetStyledField(value); }
        [Styled(nameof(_fontWeight))] public FontWeight FontWeight { get => _fontWeight; set => SetStyledField(value); }

        [Styled(nameof(_allowRichText))] public bool AllowRichText { get => _allowRichText; set => SetStyledField(value); }

        [Styled(nameof(_fontSize))] public float FontSize { get => _fontSize; set => SetStyledField(value); }

        [Styled(nameof(_backgroundColor))] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        [Styled(nameof(_textColor))] public UIColor TextColor { get => _textColor; set => SetStyledField(value); }
        #endregion
    }
}
