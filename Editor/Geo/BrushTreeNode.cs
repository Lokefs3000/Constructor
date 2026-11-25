using Editor.Geometry;
using Editor.UI.Elements.Tree;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo
{
    internal sealed class BrushTreeNode : TreeNode
    {
        private readonly Brush _brush;

        public BrushTreeNode(Brush brush)
        {
            _brush = brush;
        }

        public Brush Brush => _brush;
    }
}
