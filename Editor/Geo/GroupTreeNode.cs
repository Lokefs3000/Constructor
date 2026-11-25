using Editor.Geometry;
using Editor.UI.Elements.Tree;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo
{
    internal class GroupTreeNode : TreeNode
    {
        private BrushGroup _group;

        public GroupTreeNode(BrushGroup group)
        {
            _group = group;
        }

        public BrushGroup Group => _group;
    }
}
