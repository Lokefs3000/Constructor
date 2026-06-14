using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.UI.Interaction;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements.Tree
{
    public class BaseTreeNode : IInteractable
    {
        private UITreeView? _parentTree;

        private BaseTreeNode? _parent;
        private List<BaseTreeNode> _children;

        private int _treePosition;

        private int _shownNodeCount;
        private int _depth;
        private int _maxDepth;

        private bool _isOpened;
        private bool _isSelected;

        public BaseTreeNode()
        {
            _parentTree = null;

            _parent = null;
            _children = new List<BaseTreeNode>();

            _treePosition = -1;

            _shownNodeCount = 1;
            _depth = 0;
            _maxDepth = 0;

            _isOpened = false;
            _isSelected = false;
        }

        internal void SetNewOwner(UITreeView? treeView, BaseTreeNode? parent)
        {
            if (_parentTree == treeView && _parent == parent)
                return;

            if (parent == this)
            {
                UIManager.Logger?.Warning("Cannot set tree node parent to self!");
                parent = null;
            }

            if (_parent != parent || _parentTree != treeView)
            {
                if (_parentTree != treeView)
                {
                    if (_isSelected)
                        Deselect();

                    _parentTree?.RemoveNodeFromInternal(this);
                }

                if (_parent != null)
                {
                    _maxDepth -= _depth;

                    _parent._children.Remove(this);
                    (_parent._shownNodeCount, _parent._maxDepth) = GetNodeMetrics(_parent);
                }
                else
                {
                    if (_parentTree != null && _parentTree.TempNode == null)
                        _parentTree.TempNode = null;
                    _parentTree?.RemoveNodeFromList(this);
                }

                if (parent != null)
                {
                    _maxDepth += parent._depth + 1;

                    parent._children.Add(this);
                    (parent._shownNodeCount, parent._maxDepth) = GetNodeMetrics(parent);
                }
                else
                {
                    treeView?.AddNodeToList(this);
                }

                _parent = parent;
                _depth = (parent?._depth ?? -1) + 1;

                Debug.Assert(_parent != this);

                _parentTree = treeView;
                treeView?.AddStateFlags(UIStateFlags.InvalidVisual);

                SetChildrenDepth(_depth);
                SetChildrenTreeView(treeView);
            }
        }

        private void SetChildrenDepth(int newDepth)
        {
            int nextDepth = newDepth + 1;
            foreach (BaseTreeNode child in _children)
            {
                child._depth = nextDepth;
                child.SetChildrenDepth(nextDepth);
            }
        }

        private void SetChildrenTreeView(UITreeView? treeView)
        {
            foreach (BaseTreeNode child in _children)
            {
                if (child._parentTree != treeView)
                {
                    if (_isSelected)
                    {
                        child._parentTree?.DeselectNode(child);
                        treeView?.SelectNode(child);
                    }

                    if (child._parentTree != null && child._parentTree.TempNode == child)
                    {
                        child._parentTree.TempNode = null;
                    }

                    child._parentTree?.RemoveNodeFromInternal(child);

                    child._parentTree = treeView;
                    child.SetChildrenTreeView(treeView);
                }
            }
        }

        public void ClearChildren()
        {
            foreach (BaseTreeNode treeNode in _children)
                treeNode.SetNewOwner(null, null);

            _children.Clear();
        }

        public void Expand()
        {
            if (_isOpened)
                return;

            _isOpened = true;
            (_shownNodeCount, _maxDepth) = GetNodeMetrics(this);

            if (_parent != null)
                (_parent._shownNodeCount, _parent._maxDepth) = GetNodeMetrics(_parent);

            //_parentTree?.ExpandNode(this);
            _parentTree?.AddStateFlags(UIStateFlags.InvalidAll);
        }

        public void Collapse()
        {
            if (!_isOpened)
                return;

            _isOpened = false;
            _shownNodeCount = 1;
            _maxDepth = _depth;

            if (_parent != null)
                (_parent._shownNodeCount, _parent._maxDepth) = GetNodeMetrics(_parent);

            //_parentTree?.CollapseNode(this);
            _parentTree?.AddStateFlags(UIStateFlags.InvalidAll);
        }

        public void Select()
        {
            if (_isSelected)
                return;

            if (_parentTree != null)
            {
                _isSelected = true;

                _parentTree.SelectNode(this);
                _parentTree.AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        public void Deselect()
        {
            if (!_isSelected)
                return;

            if (_parentTree != null)
            {
                _isSelected = false;

                _parentTree.DeselectNode(this);
                _parentTree.AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        /// <summary>NOTE: Removes the <paramref name="treeNode"/> from its previous parent aswell</summary>
        public void AddNode(BaseTreeNode treeNode) => treeNode.SetNewOwner(_parentTree, this);
        public void RemoveNode(BaseTreeNode treeNode)
        {
            if (treeNode._parent == this)
                treeNode.SetNewOwner(null, null);
        }

        public void MoveNode(BaseTreeNode child, int newIndex)
        {
            Guard.IsInRange(newIndex, 0, _children.Count);

            int oldIndex = _children.IndexOf(child);
            if (oldIndex != newIndex)
            {
                (_children[oldIndex], _children[newIndex]) = (_children[newIndex], child);
                _parentTree?.AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        public virtual void DrawVisual(Vector2 position, UIPainterContext context) { }
        public virtual void Activate(MouseButton button)
        {
            OnPress?.Invoke(button);
        }

        public virtual IInteractable GetInteractable(Vector2 point) => this;
        public virtual void HandleEvent(ref readonly UIEvent @event) { }

        public UITreeView? ParentTree => _parentTree;

        internal ReadOnlySpan<BaseTreeNode> ChildrenSpan => _children.AsSpan();

        public BaseTreeNode? Parent => _parent;
        public IReadOnlyList<BaseTreeNode> Children => _children;

        public int TreePosition { get => _treePosition; internal set => _treePosition = value; }

        public bool IsExpanded
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

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value)
                    Select();
                else
                    Deselect();
            }
        }

        /// <summary>NOTE: Includes current node state in the count though it will always count</summary>
        public int ShownNodeCount => _shownNodeCount;
        public int Depth => _depth;
        public int MaxDepth => _maxDepth;

        public IWindowHost? Host => _parentTree?.Host;
        public virtual IInteractionShape? Shape => null;

        public event Action<MouseButton>? OnPress;

        private static (int shownNodes, int maxDepth) GetNodeMetrics(BaseTreeNode treeNode)
        {
            if (treeNode._isOpened)
            {
                if (treeNode._children.Count == 0)
                    return (1, treeNode._depth);
                else
                    return (treeNode._children.Sum(static (x) => x._shownNodeCount) + 1, treeNode._children.Max(static (x) => x._maxDepth));
            }
            else
            {
                return (1, treeNode._depth);
            }
        }
    }
}
