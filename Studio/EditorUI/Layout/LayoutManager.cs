using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Dock;
using EditorUI.Mathematics;
using EditorUI.Statistics;
using EditorUI.Utility;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Collections;
using Primary.Collections.Extensions;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Extensions;
using Primary.Mathematics;
using Primary.Utility;

namespace EditorUI.Layout
{
    public sealed class LayoutManager
    {
        private List<object> _recalculateNext;

        private HashSet<Widget> _failedWidgets;

        private List<WidgetTreeData> _treeData;

        private Stack<WidgetSearchData> _searchDataStack;
        private Stack<int> _groupIdStack;
        private Stack<bool> _forceRecalculateStack;

        internal LayoutManager()
        {
            _recalculateNext = new List<object>();

            _failedWidgets = new HashSet<Widget>();

            _treeData = new List<WidgetTreeData>();

            _searchDataStack = new Stack<WidgetSearchData>();
            _groupIdStack = new Stack<int>();
            _forceRecalculateStack = new Stack<bool>();
        }

        internal void RecalculateAll()
        {
            if (_recalculateNext.Count > 0)
            {
                foreach (object obj in _recalculateNext)
                {
                    if (obj is DockHost dockHost)
                        dockHost.RecalculateLayout();
                    else if (obj is Widget widget)
                        RecalculateWidgetTree(widget);
                }

                _recalculateNext.Clear();
            }
        }

        internal void RecalculateNext(DockHost dockHost) => _recalculateNext.AddUnique(dockHost);
        internal void RecalculateNext(Widget widget) => _recalculateNext.AddUnique(widget);

        private void RecalculateWidgetTree(Widget rootWidget)
        {
            LayoutStatistics statistics = new LayoutStatistics();
            ILayoutReporter? layoutReporter = null;

            WidgetWindow? widgetWindow = null;
            if (rootWidget is WindowRoot windowRoot)
            {
                widgetWindow = windowRoot.OwningWindow;
                if (widgetWindow?.Parent != null && widgetWindow.Parent.WindowRect != widgetWindow.WindowRect)
                {
                    widgetWindow.RecalculateLayout(widgetWindow.Parent.WindowRect);
                }

                layoutReporter = widgetWindow?.LayoutReporter;
            }

            layoutReporter?.OnLayoutBegin();

            GatherWidgetsForLayout(rootWidget, layoutReporter);
            if (_treeData.Count > 0)
            {
                UpdatePartialTree();

                _forceRecalculateStack.Clear();

                if (_failedWidgets.Count > 0)
                {
                    statistics.FailedWidgets = _failedWidgets.Count;

                    foreach (Widget widget in _failedWidgets)
                    {
                        widget.AddStateFlags(StateFlags.SelfInvalidLayout);
                    }

                    _failedWidgets.Clear();
                }
            }

            widgetWindow?.LayoutStatistics = statistics;
        }

        // This method likely has a good chunk of comments as im pretty much walking myself through all the rules to laying widgets out

        private void UpdatePartialTree()
        {
            Span<WidgetTreeData> treeRecursion = _treeData.AsSpan();

            using RentedStack<int> deferredLayouts = new RentedStack<int>();

            for (int i = 1; i < treeRecursion.Length; ++i)
            {
                bool isReturningAfterDefer = false;

                ref WidgetTreeData treeData = ref treeRecursion[i];
                if (deferredLayouts.TryPeek(out int deferredLayoutIndex))
                {
                    ref WidgetTreeData deferredLayoutData = ref treeRecursion[deferredLayoutIndex];
                    if (deferredLayoutData.Depth <= treeData.Depth)
                    {
                        i = deferredLayoutData.TreeRange.Start;
                        treeData = ref deferredLayoutData;
                        deferredLayouts.Pop();

                        isReturningAfterDefer = true;
                    }
                }
                
                ref WidgetTreeData parentTreeData = ref treeRecursion[treeData.ParentIndex];

                StateFlags stateFlags = treeData.Widget.StateFlags;

                if (stateFlags.HasFlags(StateFlags.InvalidLayout))
                {
                    if (stateFlags.HasFlags(StateFlags.ThisLayout))
                    {
                        Widget widget = treeData.Widget;
                        LayoutChangeMask changeMask = widget.ChangeMask;

                        if (!treeData.HasLayoutData)
                            QueryLayoutDataFor(ref treeData);

                        WidgetLayout sourceLayout = treeData.Layout;
                        WidgetLayout currentLayout = treeData.Layout;

                        bool hasChildren = treeData.TreeRange.IsEmpty;
                        bool shouldSkipChildren = true;

                        if (changeMask.HasAny(LayoutChangeMask.Transform | LayoutChangeMask.Display | LayoutChangeMask.ChildLayout | LayoutChangeMask.Anchor | LayoutChangeMask.Position))
                        {
                            InvalidateLayoutsAbove(ref i);
                            continue;
                        }

                        if (changeMask.HasFlags(LayoutChangeMask.Display))
                        {
                            changeMask |= LayoutChangeMask.Transform | LayoutChangeMask.Overflow | LayoutChangeMask.ChildLayout;
                        }

                        // Transform rect incase something changed
                        if (changeMask.HasAny(LayoutChangeMask.Transform | LayoutChangeMask.Overflow))
                        {
                            PositionMode position = widget.Position;

                            bool canHavePosition;
                            bool canHaveSize;
                            if (position == PositionMode.Absolute)
                            {
                                canHavePosition = true;
                                canHaveSize = true;
                            }
                            else
                            {
                                canHavePosition = false;
                                canHaveSize = IsParentResponsibleForSize(treeData);
                            }

                            if ((canHavePosition || canHaveSize) && !parentTreeData.HasLayoutData)
                                QueryLayoutDataFor(ref parentTreeData);

                            // If either left or right is defined they control the positioning
                            // If both are set they represent insets from the parents width
                            //    If width is defined the left position takes priority

                            UIValue? left = widget.Left;
                            UIValue? right = widget.Right;

                            UIValue? width = widget.Width;
                            UIValue? height = widget.Height;

                            float? aspectRatio = widget.AspectRatio;

                            ref Int2 clientOffset = ref currentLayout.ClientOffset;
                            ref Int2 clientSize = ref currentLayout.ClientSize;

                            clientOffset = Int2.Zero;
                            clientSize = Int2.Zero;

                            if (canHavePosition)
                            {
                                if (left.HasValue)
                                {
                                    int pixelsLeft = left.DangerousGetValueOrDefaultReference().Evaluate(parentTreeData.Layout.InteriorSize.X);
                                    if (!width.HasValue && right.HasValue)
                                    {
                                        int pixelsRight = right.DangerousGetValueOrDefaultReference().Evaluate(parentTreeData.Layout.InteriorSize.X);

                                        clientSize.X = parentTreeData.Layout.InteriorSize.X - pixelsLeft - pixelsRight;

                                        // Collapse widget where left and right meet
                                        if (clientSize.X < 0)
                                        {
                                            clientOffset.X = (int)(float.Lerp(pixelsLeft, pixelsRight, 0.5f) + 0.5f);
                                            clientSize.X = 0;
                                        }

                                        clientOffset.X = pixelsLeft;
                                    }
                                    else
                                    {
                                        clientOffset.X = pixelsLeft;
                                        clientSize.X = width.HasValue ? width.DangerousGetValueOrDefaultReference().Evaluate(parentTreeData.Layout.InteriorSize.X) : 0;
                                    }
                                }
                                else if (right.HasValue)
                                {
                                    int pixelsRight = right.DangerousGetValueOrDefaultReference().Evaluate(parentTreeData.Layout.InteriorSize.X);

                                    clientSize.X = width.HasValue ? width.DangerousGetValueOrDefaultReference().Evaluate(parentTreeData.Layout.InteriorSize.X) : 0;
                                    clientOffset.X = parentTreeData.Layout.InteriorSize.X - pixelsRight - clientSize.X;
                                }

                                if (aspectRatio.HasValue && !height.HasValue && clientSize.X != clientSize.Y)
                                {
                                    clientSize.Y = (int)(clientSize.X / aspectRatio.DangerousGetValueOrDefaultReference());
                                }
                            }

                            if (canHaveSize)
                            {
                                if (!canHavePosition)
                                {
                                    if (width.HasValue)
                                        clientSize.X = width.DangerousGetValueOrDefaultReference().Evaluate(parentTreeData.Layout.InteriorSize.X);
                                }

                                if (height.HasValue)
                                    clientSize.Y = width.DangerousGetValueOrDefaultReference().Evaluate(parentTreeData.Layout.InteriorSize.Y);

                                if (aspectRatio.HasValue && clientSize.X != clientSize.Y)
                                {
                                    if (!width.HasValue || !height.HasValue)
                                    {
                                        if (width.HasValue)
                                            clientSize.Y = (int)(clientSize.X / aspectRatio.DangerousGetValueOrDefaultReference());
                                        else
                                            clientSize.X = (int)(clientSize.Y * aspectRatio.DangerousGetValueOrDefaultReference());
                                    }
                                }
                            }

                            ref Int2 boxOffset = ref currentLayout.BoxPosition;
                            ref Int2 boxSize = ref currentLayout.BoxSize;

                            boxOffset = clientOffset;
                            boxSize = clientSize;

                            LayoutBox margins = widget.Margin;

                            if (margins.Left.HasValue)
                            {
                                clientOffset.X += margins.Left.DangerousGetValueOrDefaultReference();
                                boxSize.X += margins.Left.DangerousGetValueOrDefaultReference();
                            }
                            if (margins.Right.HasValue)
                                boxSize.X += margins.Right.DangerousGetValueOrDefaultReference();

                            if (margins.Top.HasValue)
                            {
                                clientOffset.X += margins.Left.DangerousGetValueOrDefaultReference();
                                boxSize.Y += margins.Top.DangerousGetValueOrDefaultReference();
                            }
                            if (margins.Bottom.HasValue)
                                boxSize.Y += margins.Bottom.DangerousGetValueOrDefaultReference();

                            OverflowMode overflowX = widget.OverflowX;
                            OverflowMode overflowY = widget.OverflowY;

                            if (overflowX == OverflowMode.Scroll)
                                clientSize.X -= 6;
                            if (overflowY == OverflowMode.Scroll)
                                clientSize.Y -= 6;

                            boxSize = Int2.Max(boxSize, Int2.Zero);
                            clientSize = Int2.Max(clientSize, Int2.Zero);

                            if (currentLayout.HasChanged(sourceLayout))
                            {
                                if (hasChildren)
                                    changeMask |= LayoutChangeMask.ChildLayout;

                                sourceLayout = currentLayout;
                            }
                        }

                        if (changeMask.HasFlags(LayoutChangeMask.ChildLayout))
                        {
                            ref Int2 interiorOffset = ref currentLayout.InteriorOffset;
                            ref Int2 interiorSize = ref currentLayout.InteriorSize;

                            interiorOffset = currentLayout.ClientOffset;
                            interiorSize = currentLayout.ClientSize;

                            if (Int2.GreaterThanAny(interiorSize, Int2.Zero))
                            {
                                LayoutBox padding = widget.Padding;

                                if (padding.Left.HasValue)
                                {
                                    interiorOffset.X += padding.Left.DangerousGetValueOrDefaultReference();
                                    interiorSize.X -= padding.Left.DangerousGetValueOrDefaultReference();
                                }
                                if (padding.Right.HasValue)
                                    interiorSize.X -= padding.Right.DangerousGetValueOrDefaultReference();

                                if (padding.Top.HasValue)
                                {
                                    interiorOffset.Y += padding.Top.DangerousGetValueOrDefaultReference();
                                    interiorSize.Y -= padding.Top.DangerousGetValueOrDefaultReference();
                                }
                                if (padding.Bottom.HasValue)
                                    interiorSize.Y -= padding.Bottom.DangerousGetValueOrDefaultReference();

                                interiorSize = Int2.Max(interiorSize, Int2.Zero);
                            }

                            shouldSkipChildren = Int2.LessThanOrEqualAny(interiorSize, Int2.Zero);
                        }

                        if (hasChildren)
                        {
                            if (isReturningAfterDefer)
                            {
                                switch (widget.DisplayInside)
                                {
                                    case DisplayInside.Flow: ApplyFlowLayoutTo(ref treeData); break;
                                    case DisplayInside.Flex: ApplyFlexLayoutTo(ref treeData); break;
                                    case DisplayInside.Grid: ApplyGridLayoutTo(ref treeData); break;
                                }
                            }
                            else if (!shouldSkipChildren)
                            {
                                deferredLayouts.Push(i);
                            }
                        }
                        

                        widget.ChangeMask = LayoutChangeMask.None;
                    }
                }
                else
                {
                    // Skip this branch of the tree as there is nothing to do

                    i = treeData.TreeRange.End;
                }
            }

            bool IsParentResponsibleForSize(WidgetTreeData widget)
            {
                Widget parentWidget = _treeData[widget.ParentIndex].Widget;
                DisplayInside layoutRule = parentWidget.DisplayInside;

                switch (layoutRule)
                {
                    case DisplayInside.Flow:
                        {
                            return parentWidget.AlignItems switch
                            {
                                ItemAlignment.Stretch => true,
                                _ => false,
                            };
                        }
                    case DisplayInside.Flex:
                        {
                            return parentWidget.FlexAlignItems switch
                            {
                                FlexItemAlignment.Stretch => true,
                                _ => false,
                            };
                        }
                    case DisplayInside.Grid:
                        {
                            return Flags.HasEither(~parentWidget.DisplayState.Grid.Value, 0b11 << (int)GridColumnsMethod.RowShift);
                        }
                }

                return false;
            }
        }

        private void ApplyFlowLayoutTo(ref WidgetTreeData treeData)
        {
            FlowLayout layout = new FlowLayout();
            layout.Layout(ref treeData, _treeData, this);
        }

        private void ApplyFlexLayoutTo(ref WidgetTreeData treeData)
        {

        }

        private void ApplyGridLayoutTo(ref WidgetTreeData treeData)
        {

        }

        private void GatherWidgetsForLayout(Widget rootWidget, ILayoutReporter? layoutReporter)
        {
            using RentedQueue<WidgetSearchData> widgetsToSearch = new RentedQueue<WidgetSearchData>();
            using RentedStack<int> widgetTreeStartIndices = new RentedStack<int>();

            widgetsToSearch.Enqueue(new WidgetSearchData(rootWidget, 0));
            widgetTreeStartIndices.Push(0);

            while (widgetsToSearch.TryDequeue(out WidgetSearchData searchData))
            {
                int stackDepth = searchData.Depth + 1;
                if (widgetTreeStartIndices.Count > stackDepth)
                {
                    int treeEndIndex = _treeData.Count - 1;
                    while (widgetTreeStartIndices.TryPop(out int index) && widgetTreeStartIndices.Count > stackDepth)
                    {
                        ref WidgetTreeData treeData = ref _treeData.GetRefAtIndex(index);
                        treeData.TreeRange = new IndexRange(index, treeEndIndex);
                    }
                }

                int thisIndex = _treeData.Count;
                _treeData.Add(new WidgetTreeData(searchData.Widget, searchData.Depth, IndexRange.Empty, _treeData.Count == 0 ? -1 : widgetTreeStartIndices.Peek(), false, default));

                ROList<Widget> children = searchData.Widget.Children;
                if (children.Count > 0)
                {
                    widgetTreeStartIndices.Push(thisIndex);
                    foreach (Widget child in children)
                    {
                        if (child.Children.Count == 0)
                        {
                            _treeData.Add(new WidgetTreeData(child, stackDepth, new IndexRange(_treeData.Count, _treeData.Count), thisIndex, false, default));
                        }
                        else
                        {
                            widgetsToSearch.Enqueue(new WidgetSearchData(child, stackDepth));
                        }
                    }
                }
            }
        }

        internal void QueryLayoutDataFor(ref WidgetTreeData treeData)
        {
            if (!treeData.HasLayoutData)
            {
                throw new NotImplementedException();
            }
        }

        internal int TryFindActualInteriorWidth(ref WidgetTreeData treeData)
        {
            Span<WidgetTreeData> treeRecursion = _treeData.AsSpan();
            while (true)
            {
                if (!treeData.HasLayoutData)
                    QueryLayoutDataFor(ref treeData);

                if (treeData.Layout.HasWidth)
                {
                    return treeData.Layout.InteriorSize.X;
                }
                else
                {
                    if (treeData.ParentIndex == -1)
                    {
                        return 0;
                    }
                    else
                    {
                        treeData = ref treeRecursion[treeData.ParentIndex];
                    }
                }
            }
        }

        internal void InvalidateLayoutsAbove(ref int i)
        {

        }

        private enum LayoutChangeType : byte
        {
            None,
            Child,
            This
        }
    }

    internal readonly record struct WidgetSearchData(Widget Widget, int Depth);
    internal record struct WidgetTreeData(Widget Widget, int Depth, IndexRange TreeRange, int ParentIndex, bool HasLayoutData, WidgetLayout Layout);

    [StructLayout(LayoutKind.Explicit, Pack = 16)]
    internal record struct WidgetLayout
    {
        [FieldOffset(0)] public Int2 BoxPosition;
        [FieldOffset(8)] public Int2 BoxSize;
        [FieldOffset(16)] public Int2 ClientOffset;
        [FieldOffset(24)] public Int2 ClientSize;
        [FieldOffset(32)] public Int2 InteriorOffset;
        [FieldOffset(40)] public Int2 InteriorSize;

        [FieldOffset(49)] public bool HasWidth;
        [FieldOffset(50)] public bool HasHeight;

        [FieldOffset(0)] public Rect Box;
        [FieldOffset(16)] public Rect Client;
        [FieldOffset(32)] public Rect Interior;

        [FieldOffset(0)] private Vector256<int> _v256;
        [FieldOffset(32)] private Vector128<int> _v128;

        public WidgetLayout(WidgetLayout other) => this = other;

        public readonly bool HasChanged(WidgetLayout layoutToCompare)
        {
            return _v256 != layoutToCompare._v256 && _v128 != layoutToCompare._v128;
        }

        public readonly bool HasBoxChanged(Rect other) => Box.Equals(other);
        public readonly bool HasClientChanged(Rect other) => Client.Equals(other);
        public readonly bool HasInteriorChanged(Rect other) => Interior.Equals(other);
    }

    internal readonly record struct WidgetMetricsForLayout(int LineEndIndex, int LineWidth, int LineHeight, int ItemsWithoutWidth);

    public readonly record struct MeasureReturnData(MeasureStatus Status, LayoutLockAxis LockAxis, Vector2? CustomSize = null)
    {
        public static MeasureReturnData Success => new MeasureReturnData(MeasureStatus.Success, LayoutLockAxis.None, null);
        public static MeasureReturnData MissingPendingData => new MeasureReturnData(MeasureStatus.MissingPendingData, LayoutLockAxis.None, null);
    }

    public readonly record struct LayoutReturnData(LayoutLockAxis LockAxis = LayoutLockAxis.None, bool WantsPostLayout = false, Vector2? ContentSize = null, bool WantsNewMeasure = false)
    {
        public static LayoutReturnData Success => new LayoutReturnData(LayoutLockAxis.None, false, null, false);
    }

    public enum MeasureStatus : byte
    {
        Success = 0,

        MissingPendingData,
        DontCheckChanges
    }
}
