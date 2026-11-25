using Editor.Geometry;
using Editor.UI.Elements.Tree;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo
{
    internal sealed class FaceTreeNode : TreeNode
    {
        private readonly BrushFaceIndex _faceIndex;

        public FaceTreeNode(BrushFaceIndex faceIndex)
        {
            _faceIndex = faceIndex;
        }

        public BrushFaceIndex FaceIndex => _faceIndex;
    }
}
