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

        private UIColor _fillColor;

        public TreeNode()
        {
            _text = "TreeNode";

            _fillColor = Color.Black;
        }

        public override void DrawVisual(int zIndex, Vector2 position, UICommandBuffer commandBuffer)
        {
            if (_fontStyle != null)
                commandBuffer.AddSimpleText(zIndex, position, _fillColor, _fontStyle, _text, 18.0f);
        }

        public UIFontStyle? FontStyle { get => _fontStyle; set => _fontStyle = value; }
        public string Text { get => _text; set => _text = value; }

        public UIColor FillColor { get => _fillColor; set => _fillColor = value;}
    }
}
