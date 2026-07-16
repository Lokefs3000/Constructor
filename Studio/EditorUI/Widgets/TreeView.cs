using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Layout;
using EditorUI.Visual;
using EditorUI.Widgets.Components;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Utility;
using TerraFX.Interop.Windows;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class TreeView : ScrollView
    {
        protected List<BaseTreeNode> _childNodes;
        protected int _maxNodeDepth;

        protected Stack<(ROList<BaseTreeNode> Nodes, int Index)> _nodeStack;

        protected HashSet<BaseTreeNode> _selectedNodes;

        protected BaseTreeNode? _currentNode;
        protected BaseTreeNode? _activeNode;

        private float _internalFullWidth;

        protected float _indentSize;
        protected float _nodeHeight;

        protected ushort _activeStrokeWidth;
        protected UIColor _activeStrokeColor;
        protected Vector4 _activeCornerRadius;

        protected UIColor _arrowColor;
        protected float _arrowThickness;

        public TreeView()
        {
            _childNodes = new List<BaseTreeNode>();
            _maxNodeDepth = 0;

            _nodeStack = new Stack<(ROList<BaseTreeNode> Nodes, int Index)>();

            _selectedNodes = new HashSet<BaseTreeNode>();

            _currentNode = null;
            _activeNode = null;

            _indentSize = 18.0f;
            _nodeHeight = 20.0f;

            _internalFullWidth = 0.0f;

            _activeStrokeWidth = 2;
            _activeStrokeColor = Color.White;
            _activeCornerRadius = new Vector4(4.0f);

            _arrowColor = Color.White;
            _arrowThickness = 1.75f;
        }

        protected internal override void DestroySelf()
        {
            ClearSelectedNodes();

            while (_childNodes.Count > 0)
            {
                BaseTreeNode treeNode = _childNodes[0];
                treeNode.SetNewOwner(null, null);
            }

            base.DestroySelf();
        }

        protected internal override MeasureStatus MeasureSelf(ref readonly LayoutContext context)
        {
            base.MeasureSelf(in context);

            Vector2 lastViewSize = _viewSize;
            _maxNodeDepth = _childNodes.Count == 0 ? 0 : _childNodes.Max(static (x) => x.MaxNodeDepth);
            _viewSize = new Vector2(_maxNodeDepth * _indentSize + _nodeHeight, _childNodes.Sum(static (x) => x.ShownNodeCount) * _nodeHeight);

            return lastViewSize != _viewSize ? MeasureStatus.DontCheckChanges : MeasureStatus.Success;
        }

        protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            base.LayoutSelf(in context);

            _internalFullWidth = Math.Max(_maxNodeDepth * _indentSize + _nodeHeight, _layoutState.ContentSize.X - _nodeHeight - 2.0f);
            return LayoutReturnData.Success;
        }

        protected internal override void PaintSelf(ref PainterContext painter)
        {
            base.PaintSelf(ref painter);

            IndexRange shownRange = GetShownNodeRange();

            Vector2 drawMinimum = _computedRect.Minimum + new Vector2(2.0f);
            Vector2 positionMetrics = new Vector2(_indentSize, _nodeHeight);
            Vector2 availableSize = new Vector2(_internalFullWidth, _nodeHeight);

            const float ArrowPadding = 4.0f;
            const float ArrowAspect = 1.5f;

            LineArrowBuffer expandedArrowBuffer = default;
            {
                expandedArrowBuffer.Point0 = new Vector2(-_nodeHeight + ArrowPadding, ArrowPadding * ArrowAspect);
                expandedArrowBuffer.Point1 = new Vector2(-_nodeHeight * 0.5f, _nodeHeight - ArrowPadding - ArrowAspect);
                expandedArrowBuffer.Point2 = new Vector2(-ArrowPadding, ArrowPadding * ArrowAspect);
            }

            LineArrowBuffer collapsedArrowBuffer = default;
            {
                collapsedArrowBuffer.Point0 = new Vector2(-_nodeHeight + ArrowPadding * ArrowAspect, ArrowPadding);
                collapsedArrowBuffer.Point1 = new Vector2(-ArrowPadding * ArrowAspect, _nodeHeight * 0.5f);
                collapsedArrowBuffer.Point2 = new Vector2(-_nodeHeight + ArrowPadding * ArrowAspect, _nodeHeight - ArrowPadding);
            }

            int currentPosition = 0;

            _nodeStack.Push((_childNodes, 0));
            while (_nodeStack.Count > 0)
            {
                (ROList<BaseTreeNode> list, int start) = _nodeStack.Pop();
                for (int i = start; i < list.Count; ++i)
                {
                    BaseTreeNode currentNode = list[i];

                    if (currentPosition > shownRange.End)
                        goto FinishIteration;

                    int nextTreePosition = currentPosition + currentNode.ShownNodeCount;
                    if (nextTreePosition >= shownRange.Start)
                    {
                        if (currentPosition >= shownRange.Start)
                        {
                            Vector2 basePosition = drawMinimum + new Vector2(currentNode.NodeDepth, currentPosition) * positionMetrics;
                            basePosition.X += _nodeHeight;

                            Vector2 currentAvailSize = availableSize;
                            currentAvailSize.X -= currentNode.NodeDepth * positionMetrics.X;

                            painter.PushTranslate(basePosition);
                            currentNode.PaintSelf(in painter, currentAvailSize);

                            if (_activeNode == currentNode)
                            {
                                Boundaries boundaries = new Boundaries(new Vector2(-_nodeHeight, 0.0f), currentAvailSize);
                                if (_activeStrokeWidth > 0 && _activeStrokeColor.IsVisible)
                                    painter.AddRectangle(boundaries, new Paint(Color.TransparentBlack, _activeStrokeColor, _activeStrokeWidth), _activeCornerRadius);
                            }

                            if (currentNode.ChildNodes.Count > 0)
                            {
                                if (currentNode.IsExpanded)
                                    painter.AddLines(MemoryMarshal.CreateReadOnlySpan(ref expandedArrowBuffer.Point0, 3), new Paint(_arrowColor), LinePaintMode.Strip, _arrowThickness);
                                else
                                    painter.AddLines(MemoryMarshal.CreateReadOnlySpan(ref collapsedArrowBuffer.Point0, 3), new Paint(_arrowColor), LinePaintMode.Strip, _arrowThickness);
                            }

                            painter.PopTranslate();
                        }

                        ++currentPosition;
                        if (currentNode.IsExpanded && currentNode.ChildNodes.Count > 0)
                        {
                            _nodeStack.Push((list, i + 1));
                            _nodeStack.Push((currentNode.ChildNodes, 0));

                            break;
                        }
                    }
                    else
                    {
                        currentPosition = nextTreePosition;
                    }
                }
            }

        FinishIteration:
            _nodeStack.Clear();
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            if (base.HandleEventSelf(in inputEvent))
                return true;

            if (inputEvent.EventType == UIInputEventType.MouseDown)
            {
                Vector2 point = inputEvent.Mouse.Position + new Vector2(-_scrollPosition.X, _scrollPosition.Y) - _computedRect.Minimum;
                if (inputEvent.Mouse.Button == MouseButton.Left)
                {
                    (BaseTreeNode? treeNode, int position) = GetTreeNodeAt(point.Y);
                    if (treeNode != null)
                    {
                        float pointX = point.X - _computedRect.Minimum.X;

                        float arrowStartX = treeNode.NodeDepth * _nodeHeight + 2.0f;
                        float nodeStartX = arrowStartX + _nodeHeight;

                        if (pointX >= arrowStartX && pointX < nodeStartX)
                        {
                            if (treeNode.IsExpanded)
                                treeNode.Collapse();
                            else
                                treeNode.Expand();
                        }
                    }

                    return true;
                }
            }
            else if (inputEvent.EventType == UIInputEventType.KeyDown)
            {
                BaseTreeNode? treeNode = _activeNode ?? _currentNode;
                if (treeNode == null)
                    treeNode = _activeNode = _childNodes.FirstOrDefault();

                if (treeNode != null)
                {
                    if (inputEvent.Key.Key == KeyCode.Up || inputEvent.Key.Key == KeyCode.Down)
                    {
                        BaseTreeNode? newNode = inputEvent.Key.Key == KeyCode.Up ?
                            GetPreviousNode(treeNode) :
                            GetNextNode(treeNode);

                        if (newNode != null)
                        {
                            bool isControlPressed = InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl);
                            bool isShiftPressed = InputSystem.Keyboard.IsKeyDown(KeyCode.LeftShift);

                            if (isShiftPressed && _currentNode != null)
                            {
                                if (!isControlPressed)
                                    ClearSelectedNodes(false);

                                UILog.Logger?.Information("{x} - {y}", _currentNode, newNode);

                                SelectAllBetween(_currentNode, newNode);
                                _activeNode = newNode;
                            }
                            else
                            {
                                if (!isControlPressed)
                                    _currentNode = null;
                                _activeNode = newNode;
                            }

                            NavigateViewTo(newNode);
                        }
                    }
                    else if (inputEvent.Key.Key == KeyCode.Right)
                    {
                        treeNode.Expand();
                    }
                    else if (inputEvent.Key.Key == KeyCode.Left)
                    {
                        treeNode.Collapse();
                    }
                    else if (inputEvent.Key.Key == KeyCode.Space)
                    {
                        if (!InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl))
                            ClearSelectedNodes();

                        if (_activeNode != null)
                        {
                            if (_activeNode.IsSelected)
                            {
                                _activeNode.Deselect();
                            }
                            else
                            {
                                _activeNode.Select();
                                _currentNode = _activeNode;
                            }
                        }
                    }
                    else if (inputEvent.Key.Key == KeyCode.Escape)
                    {
                        _activeNode = null;
                    }
                }

                return true;
            }

            return false;
        }

        internal void AddNodeToList(BaseTreeNode treeNode)
        {
            if (_childNodes.AddUnique(treeNode))
            {
                AddStateFlags(StateFlags.SelfInvalidLayout);
                _maxNodeDepth = Math.Max(_maxNodeDepth, treeNode.MaxNodeDepth);
            }
        }

        internal void RemoveNodeFromList(BaseTreeNode treeNode)
        {
            if (_childNodes.Remove(treeNode))
            {
                AddStateFlags(StateFlags.SelfInvalidLayout);
                _maxNodeDepth = -1;
            }
        }

        internal void SelectNode(BaseTreeNode treeNode)
        {
            if (_selectedNodes.Add(treeNode))
            {
                _currentNode ??= treeNode;
            }
        }

        internal void DeselectNode(BaseTreeNode treeNode)
        {
            if (_selectedNodes.Remove(treeNode))
            {
                if (_currentNode == treeNode)
                    _currentNode = _selectedNodes.FirstOrDefault();
            }
        }

        internal void RemoveNodeFromInternal(BaseTreeNode treeNode)
        {

        }

        public void AddNode(BaseTreeNode treeNode)
        {
            treeNode.SetNewOwner(this, null);
        }

        public void RemoveNode(BaseTreeNode treeNode)
        {
            if (treeNode.ParentTree == this && treeNode.ParentNode == null)
                treeNode.SetNewOwner(null, null);
        }

        public void MoveNode(BaseTreeNode treeNode, int newIndex)
        {
            if (treeNode.ParentTree != this || treeNode.ParentNode != null)
                return;

            Guard.IsInRange(newIndex, 0, _childNodes.Count);

            int oldIndex = _childNodes.IndexOf(treeNode);
            if (oldIndex != -1 && oldIndex != newIndex)
            {
                (_childNodes[oldIndex], _childNodes[newIndex]) = (_childNodes[newIndex], treeNode);
                AddStateFlags(StateFlags.SelfInvalidLayout);
            }
        }

        public void ClearNodes()
        {
            while (_childNodes.Count > 0)
            {
                RemoveNode(_childNodes[0]);
            }
        }

        public void ClearSelectedNodes(bool clearActiveNode = true)
        {
            BaseTreeNode? prevActiveNode = _currentNode;

            while (_selectedNodes.Count > 0)
            {
                BaseTreeNode treeNode = _selectedNodes.First();
                treeNode.Deselect();
            }

            if (clearActiveNode)
                _currentNode = null;
            else
                prevActiveNode?.Select();
        }

        public void SelectAllBetween(BaseTreeNode a, BaseTreeNode b)
        {
            if (a.ParentTree != this)
                return;
            if (b.ParentTree != this)
                return;

            BaseTreeNode endNode = b;

            int position = GetNodePosition(a);
            int activePosition = GetNodePosition(b);

            if (position < activePosition)
                PrepareStackForTraversal(a);
            else
            {
                PrepareStackForTraversal(b);
                endNode = a;
            }

            while (_nodeStack.Count > 0)
            {
                (ROList<BaseTreeNode> list, int start) = _nodeStack.Pop();
                for (int i = start; i < list.Count; ++i)
                {
                    BaseTreeNode currentNode = list[i];

                    currentNode.Select();

                    if (currentNode == endNode)
                        goto FinishIteration;

                    if (currentNode.IsExpanded && currentNode.ChildNodes.Count > 0)
                    {
                        // move iteration to after the current nodes children have been processed
                        _nodeStack.Push((list, i + 1));
                        _nodeStack.Push((currentNode.ChildNodes, 0));

                        break;
                    }
                }
            }

        FinishIteration:
            _nodeStack.Clear();
        }

        public BaseTreeNode? GetPreviousNode(BaseTreeNode treeNode)
        {
            if (treeNode.ParentTree != this)
                return null;

            ROList<BaseTreeNode> list = treeNode.ParentNode?.ChildNodes ?? _childNodes;
            int index = list.IndexOf(treeNode);

            if (index == 0)
            {
                return treeNode.ParentNode;
            }
            else
            {
                BaseTreeNode currentNode = list[index - 1];

                while (currentNode.ShownNodeCount > 1)
                {
                    currentNode = currentNode.ChildNodes[currentNode.ChildNodes.Count - 1];
                }

                return currentNode;
            }
        }

        public BaseTreeNode? GetNextNode(BaseTreeNode treeNode)
        {
            if (treeNode.ParentTree != this)
                return null;

            ROList<BaseTreeNode> list = treeNode.ParentNode?.ChildNodes ?? _childNodes;
            int index = list.IndexOf(treeNode);

            if (treeNode.ShownNodeCount == 1 && index == list.Count - 1)
            {
                BaseTreeNode? currentNode = treeNode;

                while (true)
                {
                    list = currentNode?.ParentNode?.ChildNodes ?? _childNodes;
                    index = list.IndexOf(currentNode!);

                    if (index < list.Count - 1)
                        return list[index + 1];

                    currentNode = currentNode?.ParentNode;
                    if (currentNode == null)
                        return null;
                }
            }
            else
            {
                if (treeNode.ShownNodeCount > 1)
                {
                    BaseTreeNode currentNode = treeNode;

                    while (currentNode.ShownNodeCount > 1)
                    {
                        currentNode = currentNode.ChildNodes[0];
                    }

                    return currentNode;
                }
                else
                    return list[index + 1];
            }
        }

        public void NavigateViewTo(BaseTreeNode treeNode)
        {
            if (treeNode.ParentTree != this)
                return;

            int position = GetNodePosition(treeNode);
            float yPosition = position * _nodeHeight + 2.0f;

            if (_scrollPosition.Y > yPosition - _nodeHeight)
            {
                _scrollPosition.Y = Math.Max(_scrollPosition.Y - (_scrollPosition.Y - (yPosition - 2.0f - _nodeHeight)), 0.0f);
            }
            else if (_scrollPosition.Y + _viewSize.Y < yPosition)
            {
                _scrollPosition.Y = Math.Min(yPosition - _viewSize.Y - 6.0f, _layoutState.ContentSize.Y - _viewSize.Y);
            }
        }

        private IndexRange GetShownNodeRange()
        {
            int start = (int)MathF.Floor(_scrollPosition.Y / _nodeHeight);
            return new IndexRange(start, start + (int)MathF.Ceiling(_layoutState.ContentSize.Y / _nodeHeight));
        }

        private (BaseTreeNode? treeNode, int position) GetTreeNodeAt(float y)
        {
            IndexRange shownRange = GetShownNodeRange();

            float nodeYMinimum = 0.0f;
            int currentPosition = 0;

            _nodeStack.Push((_childNodes, 0));
            while (_nodeStack.Count > 0)
            {
                (ROList<BaseTreeNode> list, int start) = _nodeStack.Pop();
                for (int i = start; i < list.Count; ++i)
                {
                    BaseTreeNode currentNode = list[i];

                    if (currentPosition > shownRange.End)
                        goto FinishIteration;

                    int nextTreePosition = currentPosition + currentNode.ShownNodeCount;
                    if (nextTreePosition >= shownRange.Start)
                    {
                        if (currentPosition >= shownRange.Start)
                        {
                            float baseYPosition = nodeYMinimum + currentPosition * _nodeHeight;

                            if (baseYPosition <= y && baseYPosition + _nodeHeight >= y)
                            {
                                _nodeStack.Clear();
                                return (currentNode, currentPosition + 1);
                            }
                        }

                        ++currentPosition;
                        if (currentNode.IsExpanded && currentNode.ChildNodes.Count > 0)
                        {
                            // move iteration to after the current nodes children have been processed
                            _nodeStack.Push((list, i + 1));
                            _nodeStack.Push((currentNode.ChildNodes, 0));

                            break;
                        }
                    }
                    else
                        currentPosition = nextTreePosition;
                }
            }

        FinishIteration:
            _nodeStack.Clear();

            return (null, -1);
        }

        private int GetNodePosition(BaseTreeNode treeNode)
        {
            BaseTreeNode currentNode = treeNode;
            IReadOnlyList<BaseTreeNode>? list = treeNode.ParentNode?.ChildNodes ?? _childNodes;

            int position = 0;

            do
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i] == currentNode)
                    {
                        ++position;
                        break;
                    }

                    position += list[i].ShownNodeCount;
                }

                currentNode = currentNode.ParentNode!;

                if (currentNode == null)
                    break;
                else
                    list = currentNode.ParentNode?.ChildNodes ?? _childNodes;

            } while (list != null);

            return position;
        }

        private void PrepareStackForTraversal(BaseTreeNode origin)
        {
            Stack<(ROList<BaseTreeNode>, int)> queue = new Stack<(ROList<BaseTreeNode>, int)>();
            _nodeStack.Clear();

            BaseTreeNode? currentNode = origin;
            do
            {
                ROList<BaseTreeNode> list = currentNode.ParentNode?.ChildNodes ?? _childNodes;
                if (currentNode == origin)
                    queue.Push((list, list.IndexOf(currentNode)));
                else
                    queue.Push((list, list.IndexOf(currentNode) + 1));
            } while ((currentNode = currentNode.ParentNode) != null);

            _nodeStack.Clear();
            while (queue.TryPop(out var tuple))
                _nodeStack.Push(tuple);
        }

        public override IInteractable GetInteractable(Vector2 point)
        {
            IInteractable baseInteractable = base.GetInteractable(point);
            if (baseInteractable != this)
                return baseInteractable;

            point += new Vector2(-_scrollPosition.X, _scrollPosition.Y) - _computedRect.Minimum;

            (BaseTreeNode? treeNode, int position) = GetTreeNodeAt(point.Y);
            if (treeNode != null)
            {
                point -= new Vector2(2.0f, (position - 1) * _nodeHeight - _scrollPosition.Y);

                float arrowStartX = treeNode.NodeDepth * _nodeHeight;
                float nodeStartX = arrowStartX + _nodeHeight;

                return (point.X >= nodeStartX && (treeNode.Shape?.Intersects(point) ?? true)) ? treeNode : baseInteractable;
            }

            return baseInteractable;
        }

        internal BaseTreeNode? ActiveNode
        {
            get => _activeNode; set
            {
                if (value == null)
                    _activeNode = null;
            }
        }

        public ROList<BaseTreeNode> ChildNodes => _childNodes;

        #region Serializable
        [Styled(nameof(_indentSize), StateFlags.SelfInvalidLayout)] public float IndentSize { get => _indentSize; set => SetStyledField(value); }
        [Styled(nameof(_nodeHeight), StateFlags.SelfInvalidLayout)] public float NodeHeight { get => _nodeHeight; set => SetStyledField(value); }

        [Styled(nameof(_activeStrokeWidth))] public ushort ActiveStrokeWidth { get => _activeStrokeWidth; set => SetStyledField(value); }
        [Styled(nameof(_activeStrokeColor))] public UIColor ActiveStrokeColor { get => _activeStrokeColor; set => SetStyledField(value); }
        [Styled(nameof(_activeCornerRadius))] public Vector4 ActiveCornerRadius { get => _activeCornerRadius; set => SetStyledField(value); }
        #endregion

        private record struct LineArrowBuffer
        {
            public Vector2 Point0;
            public Vector2 Point1;
            public Vector2 Point2;
        }
    }
}
