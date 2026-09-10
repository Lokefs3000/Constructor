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
        [StyleInclude] private IAssetProvider<FontFamily>? _fontFamily;
        [StyleInclude] private float _fontSize;
        [StyleInclude] private UIColor _textColor;
        [StyleInclude] private UIColor _selectedColor;

        public EntryGridItemStyle(GridView gridView) : base(gridView)
        {
            _fontFamily = null;
        }

        #region Serializable
        public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        public float FontSize { get => _fontSize; set => SetStyledField(value); }
        public UIColor TextColor { get => _textColor; set => SetStyledField(value); }
        public UIColor SelectedColor { get => _selectedColor; set => SetStyledField(value); }
        #endregion
    }
}
