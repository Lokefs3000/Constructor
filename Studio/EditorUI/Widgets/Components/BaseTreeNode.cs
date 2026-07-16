using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Input;
using EditorUI.Visual;
using Primary.Collections.ReadOnly;

namespace EditorUI.Widgets.Components
{
    public abstract class BaseTreeNode : IInteractable
    {
        private TreeView? _parentTree;

        private BaseTreeNode? _parentNode;
        private List<BaseTreeNode>? _childNodes;

        private int _treePosition;

        private int _shownNodeCount;
        private int _nodeDepth;
        private int _maxNodeDepth;

        private bool _isOpened;
        private bool _isSelected;

        public BaseTreeNode()
        {
            _parentTree = null;

            _parentNode = null;
            _childNodes = null;

            _treePosition = -1;

            _shownNodeCount = 1;
            _nodeDepth = 0;
            _maxNodeDepth = 0;

            _isOpened = false;
            _isSelected = false;
        }

        protected internal abstract void PaintSelf(ref readonly PainterContext painter, Vector2 availableSize);
        public abstract bool HandleEventSelf(ref readonly UIInputEvent inputEvent);

        internal void SetNewOwner(TreeView? treeView, BaseTreeNode? parent)
        {
            if (_parentTree == treeView && _parentNode == parent)
                return;

            if (parent == this)
                parent = null;

            if (_parentNode != parent || _parentTree != treeView)
            {
                if (_parentTree != treeView)
                {
                    if (_isSelected)
                        Deselect();

                    _parentTree?.RemoveNodeFromInternal(this);
                    OnParentTreeChanged(treeView);
                }

                if (_parentNode != null)
                {
                    _maxNodeDepth -= _nodeDepth;

                    _parentNode._childNodes?.Remove(this);
                    (_parentNode._shownNodeCount, _parentNode._maxNodeDepth) = GetNodeMetrics(_parentNode);
                }
                else
                {
                    if (_parentTree != null && _parentTree.ActiveNode == null)
                        _parentTree.ActiveNode = null;
                    _parentTree?.RemoveNodeFromList(this);
                }

                if (parent != null)
                {
                    _maxNodeDepth += parent._nodeDepth + 1;

                    (parent._childNodes ??= new List<BaseTreeNode>()).Add(this);
                    (parent._shownNodeCount, parent._maxNodeDepth) = GetNodeMetrics(parent);
                }
                else
                {
                    treeView?.AddNodeToList(this);
                }

                _parentNode = parent;
                _nodeDepth = (parent?._nodeDepth ?? -1) + 1;

                Debug.Assert(_parentNode != this);

                _parentTree = treeView;

                SetChildrenDepth(_nodeDepth);
                SetChildrenTreeView(treeView);
            }
        }

        private void SetChildrenDepth(int newDepth)
        {
            if (_childNodes != null)
            {
                int nextDepth = newDepth + 1;
                foreach (BaseTreeNode child in _childNodes)
                {
                    child._nodeDepth = nextDepth;
                    child.SetChildrenDepth(nextDepth);
                }
            }
        }

        private void SetChildrenTreeView(TreeView? treeView)
        {
            if (_childNodes != null)
            {
                foreach (BaseTreeNode child in _childNodes)
                {
                    if (child._parentTree != treeView)
                    {
                        if (_isSelected)
                        {
                            child._parentTree?.DeselectNode(child);
                            treeView?.SelectNode(child);
                        }

                        if (child._parentTree != null && child._parentTree.ActiveNode == child)
                        {
                            child._parentTree.ActiveNode = null;
                        }

                        child._parentTree?.RemoveNodeFromInternal(child);

                        child.OnParentTreeChanged(treeView);

                        child._parentTree = treeView;
                        child.SetChildrenTreeView(treeView);
                    }
                }
            }
        }

        protected virtual void OnParentTreeChanged(TreeView? newTreeView)
        {
        }

        protected virtual void OnSelected()
        {
        }

        protected virtual void OnDeselected()
        {
        }

        public void ClearChildren()
        {
            if (_childNodes != null)
            {
                foreach (BaseTreeNode treeNode in _childNodes)
                    treeNode.SetNewOwner(null, null);

                _childNodes.Clear();
            }
        }

        public void Expand()
        {
            if (_isOpened)
                return;

            _isOpened = true;
            (_shownNodeCount, _maxNodeDepth) = GetNodeMetrics(this);

            BaseTreeNode? parentNode = _parentNode;
            while (parentNode != null)
            {
                (parentNode._shownNodeCount, parentNode._maxNodeDepth) = GetNodeMetrics(parentNode);
                parentNode = parentNode._parentNode;
            }

            //_parentTree?.ExpandNode(this);
            _parentTree?.AddStateFlags(StateFlags.SelfInvalidLayout);
        }

        public void Collapse()
        {
            if (!_isOpened)
                return;

            _isOpened = false;
            _shownNodeCount = 1;
            _maxNodeDepth = _nodeDepth;

            BaseTreeNode? parentNode = _parentNode;
            while (parentNode != null)
            {
                (parentNode._shownNodeCount, parentNode._maxNodeDepth) = GetNodeMetrics(parentNode);
                parentNode = parentNode._parentNode;
            }

            _parentTree?.AddStateFlags(StateFlags.SelfInvalidLayout);
        }

        public void Select()
        {
            if (_isSelected)
                return;

            if (_parentTree != null)
            {
                _isSelected = true;
                OnSelected();

                _parentTree.SelectNode(this);
            }
        }

        public void Deselect()
        {
            if (!_isSelected)
                return;

            if (_parentTree != null)
            {
                _isSelected = false;
                OnDeselected();

                _parentTree.DeselectNode(this);
            }
        }

        public void AddNode(BaseTreeNode treeNode) => treeNode.SetNewOwner(_parentTree, this);
        public void RemoveNode(BaseTreeNode treeNode)
        {
            if (treeNode._parentNode == this)
                treeNode.SetNewOwner(null, null);
        }

        public void MoveNode(BaseTreeNode child, int newIndex)
        {
            if (_childNodes != null)
            {
                Guard.IsInRange(newIndex, 0, _childNodes.Count);

                int oldIndex = _childNodes.IndexOf(child);
                if (oldIndex != newIndex)
                {
                    (_childNodes[oldIndex], _childNodes[newIndex]) = (_childNodes[newIndex], child);
                    _parentTree?.AddStateFlags(StateFlags.SelfInvalidLayout);
                }
            }
            else
            {
                Guard.IsInRange(newIndex, 0, 0);
            }
        }

        public IInteractable GetInteractable(Vector2 point) => this;

        public IInteractionShape? Shape => null;
        public WidgetInputState InputState => WidgetInputState.Sink;

        public TreeView? ParentTree => _parentTree;

        public BaseTreeNode? ParentNode => _parentNode;
        public ROList<BaseTreeNode> ChildNodes => _childNodes ?? ROList<BaseTreeNode>.Empty;

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

        public int ShownNodeCount => _shownNodeCount;
        public int NodeDepth => _nodeDepth;
        public int MaxNodeDepth => _maxNodeDepth;

        #region Events
        public TreeNodeEventHandler<UIMouseInputEvent>? OnPress;
        #endregion

        private static (int shownNodes, int maxDepth) GetNodeMetrics(BaseTreeNode treeNode)
        {
            if (treeNode._isOpened)
            {
                if (treeNode._childNodes != null && treeNode._childNodes.Count > 0)
                    return (treeNode._childNodes.Sum(static (x) => x._shownNodeCount) + 1, treeNode._childNodes.Max(static (x) => x._maxNodeDepth));
                else
                    return (1, treeNode._nodeDepth);
            }
            else
            {
                return (1, treeNode._nodeDepth);
            }
        }
    }

    public delegate void TreeNodeEventHandler<T0>(BaseTreeNode treeNode, T0 arg0);
}
