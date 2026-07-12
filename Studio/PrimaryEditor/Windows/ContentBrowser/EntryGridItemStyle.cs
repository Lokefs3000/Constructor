using System;
using System.Collections.Generic;
using System.Text;
using EditorUI;
using EditorUI.Assets;
using EditorUI.Common;
using EditorUI.Widgets;

namespace PrimaryEditor.Windows.ContentBrowser
{
    internal sealed class EntryGridItemStyle : GridViewItemStyle
    {
        private IAssetProvider<FontFamily>? _fontFamily;
        private float _fontSize;
        private UIColor _textColor;
        private UIColor _selectedColor;

        public EntryGridItemStyle(GridView gridView) : base(gridView)
        {
            _fontFamily = null;
        }

        #region Serializable
        [Styled(nameof(_fontFamily))] public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        [Styled(nameof(_fontSize))] public float FontSize { get => _fontSize; set => SetStyledField(value); }
        [Styled(nameof(_textColor))] public UIColor TextColor { get => _textColor; set => SetStyledField(value); }
        [Styled(nameof(_selectedColor))] public UIColor SelectedColor { get => _selectedColor; set => SetStyledField(value); }
        #endregion
    }
}
