using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements.Tree
{
    public class TreeNode : BaseTreeNode
    {
        private UIFontStyle? _fontStyle;
        private string _text;

        private UIColor _textColor;

        public TreeNode()
        {
            _fontStyle = null;
            _text = "TreeNode";

            _textColor = Color.Black;
        }

        public override void DrawVisual(Vector2 position, UIPainterContext painter)
        {
            if (_fontStyle != null)
                painter.DrawText(position, UIPaint.FromColor(_textColor), TextBuilder.Default, _fontStyle, 1.0f, _text);
        }

        public UIFontStyle? FontStyle { get => _fontStyle; set => _fontStyle = value; }
        public string Text { get => _text; set => _text = value; }

        public UIColor TextColor { get => _textColor; set => _textColor = value;}
    }
}
