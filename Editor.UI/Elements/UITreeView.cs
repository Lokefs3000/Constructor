using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.UI.Datatypes;
using Editor.UI.Elements.Tree;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Assets;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Rendering.Assets;
using Primary.Utility;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("TreeView"), StyleableStates("Normal")]
    public class UITreeView : UIScrollView
    {
        private List<BaseTreeNode> _nodes;
        private int _maxNodeDepth;

        private Dictionary<NodeButtonKey, bool> _buttonValues;
        private List<NodeButtonColumn> _columns;

        private Stack<(IReadOnlyList<BaseTreeNode>, int)> _nodeStack;

        private HashSet<BaseTreeNode> _selectedNodes;

        private BaseTreeNode? _activeNode;
        private BaseTreeNode? _tempNode;

        private float _cornerRadius;

        private UIColor _columnColor;
        private UIColor _nodeBackgroundColor;
        private UIColor _arrowColor;

        private Vector2 _mousePosition;

        public UITreeView()
        {
            _nodes = new List<BaseTreeNode>();
            _maxNodeDepth = 0;

            _buttonValues = new Dictionary<NodeButtonKey, bool>();
            _columns = new List<NodeButtonColumn>();

            _nodeStack = new Stack<(IReadOnlyList<BaseTreeNode>, int)>();

            _selectedNodes = new HashSet<BaseTreeNode>();
            _activeNode = null;

            _cornerRadius = 4.0f;

            _columnColor = new Color(0.15f);
            _nodeBackgroundColor = new Color(0.3f);
            _arrowColor = new Color(0.9f);

            _mousePosition = Vector2.NegativeInfinity;
        }

        protected override void DestroySelf()
        {
            ClearSelectedNodes();

            while (_nodes.Count > 0)
            {
                BaseTreeNode treeNode = _nodes[0];
                treeNode.SetNewOwner(null, null);
            }

            base.DestroySelf();
        }

        public override void RecalculateLayout(UILayoutContext context)
        {
            _maxNodeDepth = _nodes.Count == 0 ? 0 : _nodes.Max(static (x) => x.MaxDepth);
            _childExtents = new Vector2(_maxNodeDepth * 18.0f + _viewSize.X, _nodes.Sum(static (x) => x.ShownNodeCount) * 20.0f);

            base.RecalculateLayout(context);
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            bool ret = base.DrawVisual(painter);

            painter.PushClippingRect(_pixelCoordinates);

            IndexRange shownRange = GetShownNodeRange();
            Vector2 mousePosition = InputSystem.Pointer.MousePosition;

            Span<Vector2> lineSpan = stackalloc Vector2[3];

            Vector2 drawMinimum = _viewCoordinates.Minimum + new Vector2(2.0f - _scrollPosition.X, 2.0f - _scrollPosition.Y % 20.0f);
            Vector2 columnMinimum = drawMinimum;

            if (_columns.Count > 0)
            {
                painter.DrawRect(new Boundaries(_viewCoordinates.Minimum, new Vector2(_viewCoordinates.Minimum.X + _columns.Count * 20.0f, _viewCoordinates.Maximum.Y)), UIPaint.FromColor(_columnColor));
                drawMinimum.X += _columns.Count * 20.0f;
            }
            
            UIPaint arrowPaint = UIPaint.FromColor(_arrowColor);
            int currentPosition = 0;

            _nodeStack.Push((_nodes, 0));
            while (_nodeStack.Count > 0)
            {
                (IReadOnlyList<BaseTreeNode> list, int start) = _nodeStack.Pop();
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
                            Vector2 basePosition = drawMinimum + new Vector2(currentNode.Depth, currentPosition - shownRange.Start) * new Vector2(18.0f, 20.0f);

                            if (_columns.Count > 0)
                            {
                                Vector2 minimum = new Vector2(columnMinimum.X, basePosition.Y);
                                for (int j = 0; j < _columns.Count; ++j)
                                {
                                    NodeButtonColumn column = _columns[j];

                                    ref bool value = ref CollectionsMarshal.GetValueRefOrAddDefault(_buttonValues, new NodeButtonKey(currentNode, column.Id), out bool exists);
                                    if (!exists)
                                        value = OnNewColumnValueNeeded?.Invoke(currentNode, column.Id) ?? column.DefaultValue;

                                    Sprite? sprite = value ? column.WhenTrue : column.WhenFalse;
                                    if (sprite != null)
                                        painter.DrawImage(new Boundaries(minimum, minimum + new Vector2(16.0f)), UIPaint.FromColor(Color.White), sprite);
                                    else
                                        painter.DrawRect(new Boundaries(minimum, minimum + new Vector2(16.0f)), UIPaint.FromColor(Color.White));

                                    minimum.X += 20.0f;
                                }
                            }

                            const float ArrowPadding = 4.0f;
                            const float ArrowAspect = 1.5f;

                            if (!currentNode.ChildrenSpan.IsEmpty)
                            {
                                if (currentNode.IsExpanded)
                                {
                                    lineSpan[0] = basePosition + new Vector2(ArrowPadding, ArrowPadding * ArrowAspect);
                                    lineSpan[1] = basePosition + new Vector2(18.0f * 0.5f, 18.0f - ArrowPadding * ArrowAspect);
                                    lineSpan[2] = basePosition + new Vector2(18.0f - ArrowPadding, ArrowPadding * ArrowAspect);
                                }
                                else
                                {
                                    lineSpan[0] = basePosition + new Vector2(ArrowPadding * ArrowAspect, ArrowPadding);
                                    lineSpan[1] = basePosition + new Vector2(18.0f - ArrowPadding * ArrowAspect, 18.0f * 0.5f);
                                    lineSpan[2] = basePosition + new Vector2(ArrowPadding * ArrowAspect, 18.0f - ArrowPadding);
                                }
                            }

                            if (currentNode.IsSelected)
                            {
                                Boundaries boundaries = new Boundaries(new Vector2(basePosition.X, basePosition.Y) + new Vector2(16.0f, -1.0f), new Vector2(_viewCoordinates.Maximum.X - 6.0f, basePosition.Y + 19.0f));
                                painter.DrawRect(boundaries, UIPaint.FromColor(_nodeBackgroundColor), _cornerRadius);
                            }

                            if (_tempNode == currentNode)
                            {
                                const float OutlineWidth = 2.0f;

                                Boundaries boundaries = new Boundaries(new Vector2(basePosition.X, basePosition.Y) + new Vector2(-2.0f + OutlineWidth, 1.0f - OutlineWidth), new Vector2(_viewCoordinates.Maximum.X - (6.0f + OutlineWidth), basePosition.Y + (17.0f + OutlineWidth)));
                                painter.DrawRect(boundaries, new UIPaint().SetStroke(true).SetStrokeWidth(OutlineWidth).SetStrokeColor(new Color(1.0f, 0.75f)), _cornerRadius);
                            }

                            if (!currentNode.ChildrenSpan.IsEmpty)
                                painter.DrawLines(lineSpan, arrowPaint, UILineMode.Strip, 1.75f);
                            currentNode.DrawVisual(new Vector2(basePosition.X + 18.0f, basePosition.Y), painter);
                        }

                        ++currentPosition;
                        if (currentNode.IsExpanded && currentNode.Children.Count > 0)
                        {
                            // move iteration to after the current nodes children have been processed
                            _nodeStack.Push((list, i + 1));
                            _nodeStack.Push((currentNode.Children, 0));

                            break;
                        }
                    }
                    else
                        currentPosition = nextTreePosition;
                }
            }

        FinishIteration:
            _nodeStack.Clear();

            painter.PopClippingRect();
            return ret;
        }

        public override void HandleEvent(ref readonly UIEvent @event)
        {
            if (@event.Type == UIEventType.MouseDown)
            {
                (BaseTreeNode? treeNode, int position) = GetTreeNodeAt(@event.Mouse.Position.Y);
                if (treeNode != null)
                {
                    float baseXPosition = 0.0f;
                    if (_columns.Count > 0)
                    {
                        for (int i = 0; i < _columns.Count; ++i)
                        {
                            NodeButtonColumn column = _columns[i];
                            baseXPosition += 20.0f;

                            if (@event.Mouse.Position.X < baseXPosition)
                            {
                                ref bool currentValue = ref CollectionsMarshal.GetValueRefOrAddDefault(_buttonValues, new NodeButtonKey(treeNode, column.Id), out bool exists);
                                if (!exists)
                                    currentValue = OnNewColumnValueNeeded?.Invoke(treeNode, column.Id) ?? column.DefaultValue;

                                currentValue = !currentValue;

                                OnColumnButtonChanged?.Invoke(treeNode, column.Id, currentValue);
                                return;
                            }
                        }
                    }

                    float xPosition = baseXPosition + 2.0f + treeNode.Depth * 18.0f - _scrollPosition.X;
                    if (@event.Mouse.Position.X >= xPosition)
                    {
                        if (@event.Mouse.Position.X <= xPosition + 18.0f)
                        {
                            if (@event.Mouse.Button == MouseButton.Left)
                            {
                                if (treeNode.IsExpanded && _tempNode != null)
                                {
                                    int tempPos = GetNodePosition(_tempNode);
                                    if (tempPos > position && tempPos < position + treeNode.ShownNodeCount)
                                    {
                                        _tempNode = null;
                                    }
                                }

                                treeNode.IsExpanded = !treeNode.IsExpanded;
                            }
                        }
                        else
                        {
                            _tempNode = null;
                            treeNode.Activate(@event.Mouse.Button);

                            if (@event.Mouse.Button == MouseButton.Left)
                            {
                                if (_activeNode != null && InputSystem.Keyboard.IsKeyDown(KeyCode.LeftShift))
                                {
                                    if (!InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl))
                                        ClearSelectedNodes(false);
                                    SelectAllBetween(treeNode, _activeNode);
                                }
                                else
                                {
                                    if (!InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl))
                                        ClearSelectedNodes();

                                    _activeNode = treeNode;
                                    treeNode.Select();

                                    OnNodePressed?.Invoke(treeNode, MouseButton.Left);
                                }
                            }
                            else
                            {
                                OnNodePressed?.Invoke(treeNode, @event.Mouse.Button);
                            }
                        }
                    }
                }
                else
                {
                    if (_selectedNodes.Count > 0 && !InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl))
                        ClearSelectedNodes();
                    _tempNode = null;
                }

                return;
            }
            else if (@event.Type == UIEventType.KeyDown)
            {
                BaseTreeNode? treeNode = _tempNode ?? _activeNode;
                if (treeNode != null)
                {
                    if (@event.Key.Key == KeyCode.Up || @event.Key.Key == KeyCode.Down)
                    {
                        BaseTreeNode? newNode = @event.Key.Key == KeyCode.Up ?
                            GetPreviousNode(treeNode) :
                            GetNextNode(treeNode);

                        if (newNode != null)
                        {
                            bool isControlPressed = InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl);
                            bool isShiftPressed = InputSystem.Keyboard.IsKeyDown(KeyCode.LeftShift);

                            if (isShiftPressed && _activeNode != null)
                            {
                                if (!isControlPressed)
                                    ClearSelectedNodes(false);

                                UIManager.Logger?.Information("{x} - {y}", _activeNode, newNode);

                                SelectAllBetween(_activeNode, newNode);
                                _tempNode = newNode;
                            }
                            else
                            {
                                if (!isControlPressed)
                                    _activeNode = null;
                                _tempNode = newNode;
                            }

                            NavigateViewTo(newNode);
                            AddStateFlags(UIStateFlags.InvalidVisual);
                        }
                    }
                    else if (@event.Key.Key == KeyCode.Right)
                    {
                        treeNode.Expand();
                    }
                    else if (@event.Key.Key == KeyCode.Left)
                    {
                        treeNode.Collapse();
                    }
                    else if (@event.Key.Key == KeyCode.Space)
                    {
                        if (!InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl))
                            ClearSelectedNodes();

                        if (_tempNode != null)
                        {
                            if (_tempNode.IsSelected)
                            {
                                _tempNode.Deselect();
                            }
                            else
                            {
                                _tempNode.Select();
                                _activeNode = _tempNode;
                            }
                        }
                    }
                    else if (@event.Key.Key == KeyCode.Escape)
                    {
                        _tempNode = null;
                    }
                }
            }

            base.HandleEvent(in @event);
        }

        public override IInteractable GetInteractable(Vector2 point)
        {
            (BaseTreeNode? treeNode, int position) = GetTreeNodeAt(point.Y);
            if (treeNode != null && treeNode is IInteractable interactable)
            {
                point -= new Vector2(_viewCoordinates.Minimum.X + 2.0f, (position - 1) * 20.0f + (_viewCoordinates.Minimum.Y - (_scrollPosition.Y % 20.0f)));
                return (interactable.Shape?.Intersects(point) ?? false) ? treeNode : this;
            }

            return this;
        }

        internal void AddNodeToList(BaseTreeNode treeNode)
        {
            if (_nodes.AddUnique(treeNode))
            {
                AddStateFlags(UIStateFlags.InvalidVisual);
                _maxNodeDepth = Math.Max(_maxNodeDepth, treeNode.MaxDepth);
            }
        }

        internal void RemoveNodeFromList(BaseTreeNode treeNode)
        {
            if (_nodes.Remove(treeNode))
            {
                AddStateFlags(UIStateFlags.InvalidVisual);
                _maxNodeDepth = -1;
            }
        }

        internal void SelectNode(BaseTreeNode treeNode)
        {
            if (_selectedNodes.Add(treeNode))
            {
                _activeNode ??= treeNode;
                OnNodeSelected?.Invoke(treeNode);
            }
        }

        internal void DeselectNode(BaseTreeNode treeNode)
        {
            if (_selectedNodes.Remove(treeNode))
            {
                if (_activeNode == treeNode)
                    _activeNode = _selectedNodes.FirstOrDefault();

                OnNodeDeselected?.Invoke(treeNode);
            }
        }

        internal void RemoveNodeFromInternal(BaseTreeNode treeNode)
        {
            foreach (NodeButtonColumn column in _columns)
            {
                _buttonValues.Remove(new NodeButtonKey(treeNode, column.Id));
            }
        }

        /// <summary>NOTE: Removes the <paramref name="treeNode"/> from its previous parent aswell</summary>
        public void AddNode(BaseTreeNode treeNode)
        {
            treeNode.SetNewOwner(this, null);
        }

        public void RemoveNode(BaseTreeNode treeNode)
        {
            if (treeNode.ParentTree == this && treeNode.Parent == null)
                treeNode.SetNewOwner(null, null);
        }

        public void MoveNode(BaseTreeNode child, int newIndex)
        {
            if (child.ParentTree != this)
                return;

            Guard.IsInRange(newIndex, 0, _nodes.Count);

            int oldIndex = _nodes.IndexOf(child);
            if (oldIndex != newIndex)
            {
                (_nodes[oldIndex], _nodes[newIndex]) = (_nodes[newIndex], child);
                AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        public void ClearNodes()
        {
            while (_nodes.Count > 0)
            {
                RemoveNode(_nodes[0]);
            }
        }

        public void AddButtonColumn(string id, Sprite? whenFalse, Sprite? whenTrue, bool defaultValue)
        {
            int index = _columns.FindIndex((x) => x.Id == id);
            if (index != -1)
                _columns[index] = new NodeButtonColumn(id, whenFalse, whenTrue, defaultValue);
            else
                _columns.Add(new NodeButtonColumn(id, whenFalse, whenTrue, defaultValue));

            AddStateFlags(UIStateFlags.InvalidVisual);
        }

        public void RemoveButtonColumn(string id)
        {
            if (_columns.RemoveWhere((x) => x.Id == id))
            {
                foreach (BaseTreeNode node in _nodes)
                {
                    RecursiveRemoval(node);
                }

                void RecursiveRemoval(BaseTreeNode treeNode)
                {
                    _buttonValues.Remove(new NodeButtonKey(treeNode, id));

                    foreach (BaseTreeNode child in treeNode.Children)
                    {
                        RecursiveRemoval(child);
                    }
                }

                AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        public void SetNodeColumnValue(BaseTreeNode treeNode, string id, bool value)
        {
            if (treeNode.ParentTree == this && _columns.Exists((x) => x.Id == id))
            {
                NodeButtonKey key = new NodeButtonKey(treeNode, id);
                ref bool currentValue = ref CollectionsMarshal.GetValueRefOrAddDefault(_buttonValues, new NodeButtonKey(treeNode, id), out bool exists);
                
                if (!exists || currentValue != value)
                {
                    currentValue = value;
                    OnColumnButtonChanged?.Invoke(treeNode, id, value);
                }
            }
        }

        public bool TryGetNodeColumnValue(BaseTreeNode treeNode, string id, out bool value)
        {
            return _buttonValues.TryGetValue(new NodeButtonKey(treeNode, id), out value);
        }

        public void ClearSelectedNodes(bool clearActiveNode = true)
        {
            BaseTreeNode? prevActiveNode = _activeNode;

            while (_selectedNodes.Count > 0)
            {
                BaseTreeNode treeNode = _selectedNodes.First();
                treeNode.Deselect();
            }

            if (clearActiveNode)
                _activeNode = null;
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
                (IReadOnlyList<BaseTreeNode> list, int start) = _nodeStack.Pop();
                for (int i = start; i < list.Count; ++i)
                {
                    BaseTreeNode currentNode = list[i];

                    currentNode.Select();

                    if (currentNode == endNode)
                        goto FinishIteration;

                    if (currentNode.IsExpanded && currentNode.Children.Count > 0)
                    {
                        // move iteration to after the current nodes children have been processed
                        _nodeStack.Push((list, i + 1));
                        _nodeStack.Push((currentNode.Children, 0));

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

            IReadOnlyList<BaseTreeNode> list = treeNode.Parent?.Children ?? _nodes;
            int index = list.IndexOf(treeNode);

            if (index == 0)
            {
                return treeNode.Parent;
            }
            else
            {
                BaseTreeNode currentNode = list[index - 1];

                while (currentNode.ShownNodeCount > 1)
                {
                    currentNode = currentNode.Children[currentNode.Children.Count - 1];
                }

                return currentNode;
            }
        }

        public BaseTreeNode? GetNextNode(BaseTreeNode treeNode)
        {
            if (treeNode.ParentTree != this)
                return null;

            IReadOnlyList<BaseTreeNode> list = treeNode.Parent?.Children ?? _nodes;
            int index = list.IndexOf(treeNode);

            if (treeNode.ShownNodeCount == 1 && index == list.Count - 1)
            {
                BaseTreeNode? currentNode = treeNode;

                while (true)
                {
                    list = currentNode?.Parent?.Children ?? _nodes;
                    index = list.IndexOf(currentNode);

                    if (index < list.Count - 1)
                        return list[index + 1];

                    currentNode = currentNode?.Parent;
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
                        currentNode = currentNode.Children[0];
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
            float yPosition = position * 20.0f + 2.0f;

            if (_scrollPosition.Y > yPosition - 20.0f)
            {
                _scrollPosition.Y = Math.Max(_scrollPosition.Y - (_scrollPosition.Y - (yPosition - 22.0f)), 0.0f);
                AddStateFlags(UIStateFlags.InvalidVisual);
            }
            else if (_scrollPosition.Y + _viewSize.Y < yPosition)
            {
                _scrollPosition.Y = Math.Min(yPosition - _viewSize.Y - 6.0f, _childExtents.Y - _viewSize.Y);
                AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        private IndexRange GetShownNodeRange()
        {
            int start = (int)MathF.Floor(_scrollPosition.Y / 20.0f);
            return new IndexRange(start, start + (int)MathF.Ceiling(_currentSize.Y / 20.0f));
        }

        private (BaseTreeNode? treeNode, int position) GetTreeNodeAt(float y)
        {
            IndexRange shownRange = GetShownNodeRange();

            float nodeYMinimum = _viewCoordinates.Minimum.Y + 2.0f - (_scrollPosition.Y % 20.0f);
            int currentPosition = 0;

            _nodeStack.Push((_nodes, 0));
            while (_nodeStack.Count > 0)
            {
                (IReadOnlyList<BaseTreeNode> list, int start) = _nodeStack.Pop();
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
                            float baseYPosition = nodeYMinimum + (currentPosition - shownRange.Start) * 20.0f;

                            if (baseYPosition <= y && baseYPosition + 20.0f >= y)
                            {
                                _nodeStack.Clear();
                                return (currentNode, currentPosition + 1);
                            }
                        }

                        ++currentPosition;
                        if (currentNode.IsExpanded && currentNode.Children.Count > 0)
                        {
                            // move iteration to after the current nodes children have been processed
                            _nodeStack.Push((list, i + 1));
                            _nodeStack.Push((currentNode.Children, 0));

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
            IReadOnlyList<BaseTreeNode>? list = treeNode.Parent?.Children ?? _nodes;

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

                currentNode = currentNode.Parent!;

                if (currentNode == null)
                    break;
                else
                    list = currentNode.Parent?.Children ?? _nodes;

            } while (list != null);

            return position;
        }

        private void PrepareStackForTraversal(BaseTreeNode origin)
        {
            Stack<(IReadOnlyList<BaseTreeNode>, int)> queue = new Stack<(IReadOnlyList<BaseTreeNode>, int)>();
            _nodeStack.Clear();

            BaseTreeNode? currentNode = origin;
            do
            {
                IReadOnlyList<BaseTreeNode> list = currentNode.Parent?.Children ?? _nodes;
                if (currentNode == origin)
                    queue.Push((list, list.IndexOf(currentNode)));
                else
                    queue.Push((list, list.IndexOf(currentNode) + 1));
            } while ((currentNode = currentNode.Parent) != null);

            _nodeStack.Clear();
            while (queue.TryPop(out var tuple))
                _nodeStack.Push(tuple);
        }

        internal BaseTreeNode? TempNode
        {
            get => _tempNode; set
            {
                if (value == null)
                    _tempNode = null;
            }
        }

        public ROList<BaseTreeNode> Nodes => _nodes;

        #region Styleable
        [StyleableProperty(nameof(_columnColor), UIStateFlags.InvalidVisual)] public UIColor ColumnColor { get => _columnColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_nodeBackgroundColor), UIStateFlags.InvalidVisual)] public UIColor NodeBackgroundColor { get => _nodeBackgroundColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_arrowColor), UIStateFlags.InvalidVisual)] public UIColor ArrowColor { get => _arrowColor; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_cornerRadius), UIStateFlags.InvalidVisual)] public float CornerRadius { get => _cornerRadius; set => SetStyleProperty(value); }
        #endregion
        #region Events
        public event Action<BaseTreeNode, MouseButton>? OnNodePressed;
        public event Action<BaseTreeNode, string, bool>? OnColumnButtonChanged;

        public event Action<BaseTreeNode>? OnNodeSelected;
        public event Action<BaseTreeNode>? OnNodeDeselected;

        public event Func<BaseTreeNode, string, bool>? OnNewColumnValueNeeded;
        #endregion
    }

    public readonly record struct NodeButtonKey(BaseTreeNode TreeNode, FastStringHash Id);
    public readonly record struct NodeButtonColumn(string Id, Sprite? WhenFalse, Sprite? WhenTrue, bool DefaultValue);
}
