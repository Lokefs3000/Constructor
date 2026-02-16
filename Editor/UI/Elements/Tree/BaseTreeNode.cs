using CommunityToolkit.HighPerformance;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements.Tree
{
    public class BaseTreeNode
    {
        private UITreeView? _parentTree;

        private BaseTreeNode? _parent;
        private List<BaseTreeNode> _children;

        private int _shownNodeCount;
        private int _depth;

        private bool _isOpened;

        public BaseTreeNode()
        {
            _parentTree = null;

            _parent = null;
            _children = new List<BaseTreeNode>();

            _shownNodeCount = 1;
            _depth = 0;

            _isOpened = false;
        }

        private void SetParent(BaseTreeNode? newParent)
        {
            if (_parent == newParent)
                return;

            if (_parent != null)
            {
                _parentTree?.RemoveNodeFromTree(this);
                _parent._children.Remove(this);

                if (_parent._isOpened)
                    _parent._shownNodeCount -= _shownNodeCount;
            }

            if (newParent == null)
            {
                _parentTree = null;
                _parent = null;

                _depth = 0;
            }
            else
            {
                _parentTree = newParent._parentTree;
                _parent = newParent;

                _depth = newParent._depth + 1;

                newParent._children.Add(this);

                if (newParent._isOpened)
                {
                    newParent._parentTree?.AddNodeToTree(this);
                    newParent._shownNodeCount += _shownNodeCount;
                }
            }

            SetChildrenDepth(_depth + 1);
        }

        private void SetChildrenDepth(int newDepth)
        {
            int nextDepth = newDepth + 1;
            foreach (BaseTreeNode child in _children)
            {
                child._depth = newDepth;
                child.SetChildrenDepth(nextDepth);
            }
        }

        internal void SetRootData(UITreeView treeView)
        {
            _parentTree = treeView;
            _depth = -1;
        }

        public void Expand()
        {
            if (_isOpened)
                return;

            _shownNodeCount = _children.Sum((x) => x._shownNodeCount /*Correct for *this* node*/) + 1;
            _isOpened = true;

            _parentTree?.ExpandNode(this);
        }

        public void Collapse()
        {
            if (!_isOpened)
                return;

            _shownNodeCount = 1;
            _isOpened = false;

            _parentTree?.CollapseNode(this);
        }

        public void MoveChild(BaseTreeNode child, int newIndex)
        {
            if (newIndex >= 0 && newIndex < _children.Count)
                throw new IndexOutOfRangeException($"newIndex ({newIndex}) >= 0 && newIndex ({newIndex}) < _children.Count ({_children.Count})");

            int oldIndex = _children.IndexOf(child);
            if (oldIndex != newIndex)
            {
                _children[oldIndex] = _children[newIndex];
                _children[newIndex] = child;

                _parentTree?.MoveNodeWithinTree(child, newIndex);
            }
        }

        public virtual void DrawVisual(int zIndex, Vector2 position, UICommandBuffer commandBuffer) { }

        internal UITreeView? ParentTree => _parentTree;

        internal ReadOnlySpan<BaseTreeNode> ChildrenSpan => _children.AsSpan();

        public BaseTreeNode? Parent { get => _parent; set => SetParent(value); }
        public IReadOnlyList<BaseTreeNode> Children => _children;

        public bool IsOpened
        {
            get => _isOpened;
            set
            {
                if (value)
                    Expand();
                else
                    Collapse();
            }
        }

        /// <summary>NOTE: Includes current node state in the count though it will always count</summary>
        public int ShownNodeCount => _shownNodeCount;
        public int Depth => _depth;
    }
}
