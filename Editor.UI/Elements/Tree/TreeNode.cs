using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Text;
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
        private UIFontAsset? _font;
        private FontStyle _style;
        private FontWeight _weight;

        private string _text;

        private UIColor _textColor;

        public TreeNode()
        {
            _font = null;
            _style = FontStyle.Normal;
            _weight = FontWeight.Normal;

            _text = "TreeNode";

            _textColor = Color.Black;
        }

        public override void DrawVisual(Vector2 position, UIPainterContext painter)
        {
            if (_font != null)
            {
                painter.DrawText(new Vector2(position.X, position.Y + (18.0f - (18.0f - 14.0f))), UIPaint.FromColor(_textColor), new TextBuilder().SetOrigin(TextOrigin.Bottom), _font.FindStyle(_style, _weight), 14.0f / TextManager.PixelsPerEM, _text);
            }
        }

        public UIFontAsset? Font { get => _font; set => _font = value; }
        public FontStyle FontStyle { get => _style; set => _style = value; }
        public FontWeight FontWeight { get => _weight; set => _weight = value; }

        public string Text { get => _text; set => _text = value; }

        public UIColor TextColor { get => _textColor; set => _textColor = value;}
    }
}
