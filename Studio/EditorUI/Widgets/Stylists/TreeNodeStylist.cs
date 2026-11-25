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
        [StyleInclude] protected IAssetProvider<FontFamily>? _fontFamily;
        [StyleInclude] protected FontStyle _fontStyle;
        [StyleInclude] protected FontWeight _fontWeight;

        [StyleInclude] protected bool _allowRichText;

        [StyleInclude] protected float _fontSize;

        [StyleInclude] protected UIColor _backgroundColor;
        [StyleInclude] protected UIColor _textColor;

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
        public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        public FontStyle FontStyle { get => _fontStyle; set => SetStyledField(value); }
        public FontWeight FontWeight { get => _fontWeight; set => SetStyledField(value); }

        public bool AllowRichText { get => _allowRichText; set => SetStyledField(value); }

        public float FontSize { get => _fontSize; set => SetStyledField(value); }

        public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        public UIColor TextColor { get => _textColor; set => SetStyledField(value); }
        #endregion
    }
}
