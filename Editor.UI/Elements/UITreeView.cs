using Arch.Core;
using CommunityToolkit.HighPerformance;
using Editor.UI.Datatypes;
using Editor.UI.Elements.Tree;
using Editor.UI.Interaction;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Input.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("TreeView")]
    public class UITreeView : UIElement
    {
        private List<BaseTreeNode> _shownNodes;
        private BaseTreeNode _rootNode;

        private UIColor _arrowColor;

        public UITreeView()
        {
            _shownNodes = new List<BaseTreeNode>();
            _rootNode = new BaseTreeNode();

            _arrowColor = Color.Black;

            {
                _shownNodes.Add(_rootNode);
                _rootNode.SetRootData(this);

                _rootNode.Expand();
            }
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            Boundaries boundaries = PixelCoordinates;

            int offset = 0;
            int countLimit = Math.Min(offset + (int)MathF.Ceiling((boundaries.Maximum.Y - boundaries.Minimum.Y) / 18.0f), _shownNodes.Count - 1);

            ReadOnlySpan<BaseTreeNode> span = _shownNodes.AsSpan().Slice(offset + 1, countLimit);

            for (int i = 0; i < span.Length; i++)
            {
                BaseTreeNode node = span[i];
                float yOffset = i * 18.0f;

                Vector2 basePosition = new Vector2(boundaries.Minimum.X + node.Depth * 18.0f + 2.0f, boundaries.Minimum.Y + yOffset);

                const float ArrowPadding = 4.0f;
                if (node.IsOpened)
                    painter.DrawTriangle(basePosition + new Vector2(ArrowPadding, 17.0f - ArrowPadding + 4.0f), basePosition + new Vector2(16.0f - ArrowPadding, 9.0f + 4.0f), basePosition + new Vector2(ArrowPadding, ArrowPadding + 4.0f), UIPaint.FromColor(_arrowColor));
                else
                    painter.DrawTriangle(basePosition + new Vector2(ArrowPadding, ArrowPadding + 5.0f), basePosition + new Vector2(9.0f, 17.0f - ArrowPadding + 5.0f), basePosition + new Vector2(17.0f - ArrowPadding, ArrowPadding + 5.0f), UIPaint.FromColor(_arrowColor));

                node.DrawVisual(new Vector2(basePosition.X + 18.0f, basePosition.Y), painter);
            }

            return base.DrawVisual(painter);
        }

        public override void HandleEvent(HostInteractionManager interaction, ref readonly UIEvent @event)
        {
            if (@event.Type == UIEventType.MouseActivate)
            {
                if (@event.Mouse.Button == MouseButton.Left)
                {
                    Vector2 localPosition = @event.Mouse.Position - PixelCoordinates.Minimum;

                    int mouseIndex = (int)MathF.Floor(localPosition.Y / 18.0f) + 1;
                    if (mouseIndex >= 0 && mouseIndex < _shownNodes.Count)
                    {
                        BaseTreeNode treeNode = _shownNodes[mouseIndex];
                        Vector2 basePosition = new Vector2(treeNode.Depth * 18.0f, mouseIndex * 18.0f);

                        if (localPosition.X >= basePosition.X)
                        {
                            localPosition -= basePosition;

                            if (localPosition.X <= 18.0f)
                            {
                                treeNode.IsOpened = !treeNode.IsOpened;
                            }
                            else
                            {

                            }
                        }
                    }
                }
            }
        }

        internal void AddNodeToTree(BaseTreeNode treeNode)
        {
            BaseTreeNode parent = treeNode.Parent!;

            //int shownNodeCount = CountAllShownNodes(treeNode);
            using RentedArray<BaseTreeNode> childNodes = RentArrayForShownNodes(treeNode);

            for (int i = 0; i < parent.Children.Count; ++i)
            {
                if (parent.Children[i] == treeNode)
                {
                    if (i == 0)
                    {
                        int index = _shownNodes.IndexOf(parent);
                        _shownNodes.InsertRange(index + 1, childNodes.Span);
                    }
                    else
                    {
                        BaseTreeNode previous = parent.Children[i - 1];

                        int index = _shownNodes.IndexOf(previous) + previous.ShownNodeCount;
                        _shownNodes.InsertRange(index, childNodes.Span);
                    }
                }
            }
        }

        internal void RemoveNodeFromTree(BaseTreeNode treeNode)
        {
            int index = _shownNodes.IndexOf(treeNode);
            if (index != -1)
            {
                _shownNodes.RemoveRange(index, treeNode.ShownNodeCount);
            }
        }

        internal void MoveNodeWithinTree(BaseTreeNode treeNode, int newIndex)
        {
            int index = _shownNodes.IndexOf(treeNode);
            if (index != -1)
            {
                using RentedArray<BaseTreeNode> nodes = RentedArray<BaseTreeNode>.Rent(treeNode.ShownNodeCount, true);
                _shownNodes.AsSpan().Slice(index, treeNode.ShownNodeCount).CopyTo(nodes.Span);

                int newDestIndex;
                if (newIndex == 0)
                {
                    newDestIndex = _shownNodes.IndexOf(treeNode.Parent!) + 1;
                }
                else
                {
                    BaseTreeNode prevNode = _shownNodes[index - 1];
                    newDestIndex = _shownNodes.IndexOf(prevNode) + prevNode.ShownNodeCount;
                }

                int count = index - newDestIndex;

                ListMoveRange(_shownNodes, newDestIndex, newDestIndex + treeNode.ShownNodeCount, count);
                _shownNodes.InsertRange(newDestIndex, nodes.Span);
            }
        }

        internal void ExpandNode(BaseTreeNode treeNode)
        {
            int index = _shownNodes.IndexOf(treeNode);
            if (index != -1)
            {
                using RentedArray<BaseTreeNode> childNodes = RentArrayForShownNodes(treeNode);
                _shownNodes.InsertRange(index + 1, childNodes.Span.Slice(1));
            }
        }

        internal void CollapseNode(BaseTreeNode treeNode)
        {
            int index = _shownNodes.IndexOf(treeNode);
            if (index != -1)
            {
                int previousCount = treeNode.Children.Sum((x) => x.ShownNodeCount);
                _shownNodes.RemoveRange(index + 1, previousCount);
            }
        }

        private int CountAllShownNodes(BaseTreeNode treeNode)
        {
            int count = 0;
            if (treeNode.IsOpened)
                IterateChildren(treeNode, ref count);

            return count;

            static void IterateChildren(BaseTreeNode treeNode, ref int count)
            {
                ++count;

                for (int i = 0; i < treeNode.Children.Count; i++)
                {
                    BaseTreeNode child = treeNode.Children[i];
                    if (child.IsOpened)
                    {
                        IterateChildren(child, ref count);
                    }
                }
            }
        }

        private RentedArray<BaseTreeNode> RentArrayForShownNodes(BaseTreeNode treeNode)
        {
            int count = treeNode.ShownNodeCount;//CountAllShownNodes(treeNode);
            if (count == 0)
                return default;

            RentedArray<BaseTreeNode> treeNodes = RentedArray<BaseTreeNode>.Rent(count, true);
            try
            {
                treeNodes[0] = treeNode;

                if (treeNode.IsOpened)
                {
                    int offset = 1;
                    IterateChildren(treeNode, ref offset, treeNodes.Span);

                    Debug.Assert(offset == treeNodes.Count);
                }

                static void IterateChildren(BaseTreeNode treeNode, ref int offset, Span<BaseTreeNode> span)
                {
                    for (int i = 0; i < treeNode.Children.Count; i++)
                    {
                        BaseTreeNode child = treeNode.Children[i];
                        span[offset++] = child;

                        if (child.IsOpened)
                        {
                            IterateChildren(child, ref offset, span);
                        }
                    }
                }
            }
            catch (Exception)
            {
                treeNodes.Dispose();
            }

            return treeNodes;
        }

        private static void ListMoveRange<T>(List<T> list, int sourceIndex, int destinationIndex, int count)
        {
            Span<T> values = list.AsSpan();

            int min = Math.Min(sourceIndex, destinationIndex);
            int max = Math.Max(sourceIndex, destinationIndex);

            for (int i = 0; i < count; i++)
            {
                (values[max + i], values[min + i]) = (values[min + i], values[max + i]);
            }
        }

        public BaseTreeNode RootNode => _rootNode;
        
        #region Styleable
        [StyleableProperty(nameof(_arrowColor), UIStateFlags.InvalidVisual)] public UIColor ArrowColor { get => _arrowColor; set => SetStyleProperty(value); }
        #endregion
    }
}
