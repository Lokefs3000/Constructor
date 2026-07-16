using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Dock;
using EditorUI.Statistics;
using EditorUI.Utility;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Collections;
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

        private List<LayoutGroup> _layoutGroups;

        private Stack<WidgetSearchData> _searchDataStack;
        private Stack<int> _groupIdStack;
        private Stack<bool> _forceRecalculateStack;

        internal LayoutManager()
        {
            _recalculateNext = new List<object>();

            _failedWidgets = new HashSet<Widget>();

            _layoutGroups = new List<LayoutGroup>();

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

            SetupLayoutGroups(rootWidget, layoutReporter);
            
            if (_layoutGroups.Count > 0)
            {
                Span<LayoutGroup> layoutGroups = _layoutGroups.AsSpan();
                bool forceRecalculate = false;

                // Measure all first
                for (int i = 0; i < layoutGroups.Length; ++i)
                {
                    ref LayoutGroup layoutGroup = ref layoutGroups[i];
                    MeasureWidgetsInGroup(ref layoutGroup, ref forceRecalculate, layoutReporter);
                }

                // Layout incrementally
                for (int i = layoutGroups.Length - 1; i >= 0; --i)
                {
                    ref LayoutGroup layoutGroup = ref layoutGroups[i];
                    if (layoutGroup.NeedsMeasure)
                        MeasureWidgetsInGroup(ref layoutGroup, ref forceRecalculate, layoutReporter);
                    LayoutWidgetsInGroup(ref layoutGroup, layoutReporter);

                    layoutGroup.NeedsRelayout = false;
                    layoutGroup.NeedsMeasure = false;

                    if (layoutGroup.TreeOwner.StateFlags.HasFlags(StateFlags.SelfInvalidLayout))
                    {
                        _groupIdStack.Push(layoutGroup.GroupId);
                        while (++i < layoutGroups.Length && _groupIdStack.Count > 0)
                        {
                            ref LayoutGroup previousLayoutGroup = ref layoutGroups[i];
                            while (_groupIdStack.TryPeek(out int result))
                            {
                                if (previousLayoutGroup.ParentGroupId == result)
                                {
                                    previousLayoutGroup.NeedsRelayout = true;
                                    previousLayoutGroup.NeedsMeasure = true;
                                    _groupIdStack.Push(previousLayoutGroup.GroupId);
                                    break;
                                }

                                _groupIdStack.Pop();
                            }
                        }

                        layoutGroup.NeedsRelayout = true;
                        layoutGroup.NeedsMeasure = true;

                        _groupIdStack.Clear();
                    }
                }

                // Compute window space rects
                for (int i = 0; i < layoutGroups.Length; ++i)
                {
                    ref LayoutGroup layoutGroup = ref layoutGroups[i];
                    ComputeRectForWidgetsInGroup(ref layoutGroup);
                }

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

            ClearLayoutData();

            widgetWindow?.LayoutStatistics = statistics;
        }

        private void MeasureWidgetsInGroup(ref LayoutGroup layoutGroup, ref bool forceRecalculate, ILayoutReporter? layoutReporter)
        {
            RentedList<WidgetData> widgets = layoutGroup.Widgets;
            int lastWidgetIndex = widgets.Count - 1;

            Widget? parentWidget = null;

            // Measure widgets
            for (int i = 0; i < widgets.Count; ++i)
            {
                WidgetData widgetData = widgets[i];
                bool measurementsChanged = false;

                if (forceRecalculate)
                    widgetData.Widget.AddStateFlags(StateFlags.SelfInvalidLayout);

                layoutReporter?.OnWidgetConsidered(widgetData.Widget, LayoutConsiderType.Measure);
                if (i == 0 || widgetData.Widget.StateFlags.HasFlags(StateFlags.SelfInvalidLayout))
                {
                    LayoutContext context;
                    if ((parentWidget = widgetData.Widget.Parent) != null)
                    {
                        Vector2 parentSize = parentWidget.ContentSize;
                        switch (parentWidget.AutoResize)
                        {
                            case AutoResizeMode.ResizeX: parentSize.X = 0.0f; break;
                            case AutoResizeMode.ResizeY: parentSize.Y = 0.0f; break;
                            case AutoResizeMode.ResizeXY: parentSize = Vector2.Zero; break;
                        }

                        context = new LayoutContext(parentSize, Vector2.Zero, parentWidget.SizeLockAxis);
                    }
                    else
                    {
                        context = new LayoutContext(Vector2.Zero, Vector2.Zero, LayoutLockAxis.None);
                    }

                    Vector128<float> previousSize = widgetData.Widget.LayoutState.Size;
                    LayoutLockAxis previousPositionAxisLock = widgetData.Widget.PositionLockAxis;
                    LayoutLockAxis previousSizeAxisLock = widgetData.Widget.SizeLockAxis;

                    MeasureStatus status = widgetData.Widget.MeasureSelf(ref context);
                    if (status == MeasureStatus.MissingPendingData)
                    {
                        _failedWidgets.Add(widgetData.Widget);
                    }
                    else
                    {
                        switch (widgetData.Widget.AutoResize)
                        {
                            case AutoResizeMode.ResizeX:
                                {
                                    widgetData.Widget.LayoutState.Size = Sse41.BlendVariable(
                                        widgetData.Widget.LayoutState.Size,
                                        previousSize,
                                        Vector128.Create(Unsafe.BitCast<uint, float>(uint.MaxValue), 0.0f, Unsafe.BitCast<uint, float>(uint.MaxValue), 0.0f));
                                    break;
                                }
                            case AutoResizeMode.ResizeY:
                                {
                                    widgetData.Widget.LayoutState.Size = Sse41.BlendVariable(
                                        widgetData.Widget.LayoutState.Size,
                                        previousSize,
                                        Vector128.Create(0.0f, Unsafe.BitCast<uint, float>(uint.MaxValue), 0.0f, Unsafe.BitCast<uint, float>(uint.MaxValue)));
                                    break;
                                }
                            case AutoResizeMode.ResizeXY:
                                {
                                    widgetData.Widget.LayoutState.Size = previousSize;
                                    break;
                                }
                        }

                        measurementsChanged =
                            status == MeasureStatus.DontCheckChanges ||
                            previousSize != widgetData.Widget.LayoutState.Size ||
                            previousPositionAxisLock != widgetData.Widget.PositionLockAxis ||
                            previousSizeAxisLock != widgetData.Widget.SizeLockAxis;

                        if (!measurementsChanged)
                        {
                            widgetData.Widget.RemoveStateFlags(StateFlags.ThisLayout);
                        }
                        else
                        {
                            parentWidget = widgetData.Widget;
                            while ((parentWidget = parentWidget?.Parent) != null)
                            {
                                if (parentWidget.AutoResize == AutoResizeMode.None && !parentWidget.LayoutBehaviour.ListenToChildren)
                                    break;
                                parentWidget.AddStateFlags(StateFlags.SelfInvalidLayout);
                            }
                        }
                    }
                }

                if (_forceRecalculateStack.Count > widgetData.Depth)
                {
                    do
                    {
                        forceRecalculate = _forceRecalculateStack.Pop();
                    } while (_forceRecalculateStack.Count > widgetData.Depth);
                }

                bool hasChildren = i < lastWidgetIndex && widgets[i + 1].Depth > widgetData.Depth;
                if (hasChildren)
                {
                    if (widgetData.Widget.StateFlags.HasFlags(StateFlags.InvalidLayout) || widgetData.Widget.AutoResize != AutoResizeMode.None)
                    {
                        _forceRecalculateStack.Push(forceRecalculate);
                        forceRecalculate = measurementsChanged;
                    }
                    // else
                    // {
                    //     // Skip all children as nothing has changed on their parent
                    //     while (++i < widgets.Count && widgets[i].Depth > widgetData.Depth)
                    //     {
                    //         widgets[i].Widget.RemoveStateFlags(StateFlags.SelfInvalidLayout);
                    //     }
                    // 
                    //     --i;
                    // }
                }
            }
        }

        private void LayoutWidgetsInGroup(ref LayoutGroup layoutGroup, ILayoutReporter? layoutReporter)
        {
            RentedList<WidgetData> widgets = layoutGroup.Widgets;
            int lastWidgetIndex = widgets.Count - 1;

            Widget? parentWidget = null;

            // Layout widgets
            for (int i = lastWidgetIndex; i >= 0; --i)
            {
                WidgetData widgetData = widgets[i];

                layoutReporter?.OnWidgetConsidered(widgetData.Widget, LayoutConsiderType.Layout);
                if (i == 0 || widgetData.Widget.StateFlags.HasFlags(StateFlags.SelfInvalidLayout))
                {
                    LayoutContext context;
                    if ((parentWidget = widgetData.Widget.Parent) != null)
                    {
                        Vector2 parentSize = parentWidget.ContentSize;
                        switch (parentWidget.AutoResize)
                        {
                            case AutoResizeMode.ResizeX: parentSize.X = 0.0f; break;
                            case AutoResizeMode.ResizeY: parentSize.Y = 0.0f; break;
                            case AutoResizeMode.ResizeXY: parentSize = Vector2.Zero; break;
                        }

                        context = new LayoutContext(parentSize, Vector2.Zero, parentWidget.PositionLockAxis);
                    }
                    else
                    {
                        context = new LayoutContext(Vector2.Zero, Vector2.Zero, LayoutLockAxis.None);
                    }

                    Vector128<float> previousPosition = widgetData.Widget.LayoutState.Position;

                    widgetData.Widget.RemoveStateFlags(StateFlags.SelfInvalidLayout);

                    layoutReporter?.OnWidgetRelayout(widgetData.Widget);
                    widgetData.Widget.LayoutSelf(ref context);

                    switch (widgetData.Widget.AutoResize)
                    {
                        case AutoResizeMode.ResizeX:
                            {
                                float contentOffset = widgetData.Widget.ContentPosition.X - widgetData.Widget.IdealPosition.X;
                                float maxWidth = 0.0f;
                                foreach (Widget childWidget in widgetData.Widget.Children)
                                {
                                    maxWidth = Math.Max(maxWidth, contentOffset + childWidget.IdealPosition.X + childWidget.IdealSize.X);
                                }

                                ref WidgetLayoutState layoutState = ref widgetData.Widget.LayoutState;
                                layoutState.IdealSize.X = maxWidth + widgetData.Widget.Padding.X;
                                layoutState.ContentSize.X += layoutState.IdealSize.X;
                                break;
                            }
                        case AutoResizeMode.ResizeY:
                            {
                                float contentOffset = widgetData.Widget.ContentPosition.Y - widgetData.Widget.IdealPosition.Y;
                                float maxHeight = 0.0f;
                                foreach (Widget childWidget in widgetData.Widget.Children)
                                {
                                    maxHeight = Math.Max(maxHeight, contentOffset + childWidget.IdealPosition.Y + childWidget.IdealSize.Y);
                                }

                                ref WidgetLayoutState layoutState = ref widgetData.Widget.LayoutState;
                                layoutState.IdealSize.Y = maxHeight + widgetData.Widget.Padding.Y;
                                layoutState.ContentSize.Y += layoutState.IdealSize.Y;
                                break;
                            }
                        case AutoResizeMode.ResizeXY:
                            {
                                Vector2 contentOffset = widgetData.Widget.ContentPosition - widgetData.Widget.IdealPosition;
                                Vector2 maxSize = Vector2.Zero;
                                foreach (Widget childWidget in widgetData.Widget.Children)
                                {
                                    maxSize = Vector2.Max(maxSize, contentOffset + childWidget.IdealPosition + childWidget.IdealSize);
                                }

                                ref WidgetLayoutState layoutState = ref widgetData.Widget.LayoutState;
                                layoutState.IdealSize = maxSize + widgetData.Widget.Padding.GetLower();
                                layoutState.ContentSize = layoutState.IdealSize + layoutState.ContentSize;
                                break;
                            }
                    }

                    if (previousPosition != widgetData.Widget.LayoutState.Position)
                    {
                        parentWidget = widgetData.Widget;
                        while ((parentWidget = parentWidget?.Parent) != null)
                        {
                            if (parentWidget.AutoResize == AutoResizeMode.None && !parentWidget.LayoutBehaviour.ListenToChildren)
                                break;
                            parentWidget.AddStateFlags(StateFlags.SelfInvalidLayout);
                        }
                    }
                }
                else
                {
                    widgetData.Widget.RemoveStateFlags(StateFlags.InvalidLayout);
                }
            }
        }

        private void ComputeRectForWidgetsInGroup(ref LayoutGroup layoutGroup)
        {
            for (int i = 0; i < layoutGroup.Widgets.Count; ++i)
            {
                WidgetData widgetData = layoutGroup.Widgets[i];

                Vector2 parentOffset = Vector2.Zero;
                if (widgetData.Widget.Parent != null)
                {
                    Widget parentWidget = widgetData.Widget.Parent;
                    parentOffset = parentWidget.ComputedRect.Minimum + (parentWidget.LayoutState.ContentPosition - parentWidget.LayoutState.IdealPosition);
                }

                parentOffset += widgetData.Widget.IdealPosition;
                widgetData.Widget.ComputedRect = new Boundaries(parentOffset, parentOffset + widgetData.Widget.IdealSize);
                widgetData.Widget.AfterComputedRectSelf();
            }
        }

        private void SetupLayoutGroups(Widget rootWidget, ILayoutReporter? layoutReporter)
        {
            using RentedQueue<GroupSearchData> widgetsWithGroups = new RentedQueue<GroupSearchData>();

            int currentGroupId = 0;

            widgetsWithGroups.Enqueue(new GroupSearchData(rootWidget, 0, -1, 0, LayoutBehaviour.Default));
            while (widgetsWithGroups.TryDequeue(out GroupSearchData currentGroupOwner))
            {
                layoutReporter?.OnWidgetGrouped(currentGroupOwner.Widget);

                RentedList<WidgetData> widgets = new RentedList<WidgetData>();

                _searchDataStack.Push(new WidgetSearchData(currentGroupOwner.Widget, currentGroupOwner.Depth, currentGroupOwner.GroupId, currentGroupOwner.Behaviour));

                while (_searchDataStack.TryPop(out WidgetSearchData result))
                {
                    widgets.Add(new WidgetData(result.Widget, result.Depth, result.Behaviour.ListenToChildren || result.Widget.AutoResize != AutoResizeMode.None));

                    ROList<Widget> children = result.Widget.Children;
                    if (children.Count > 0)
                    {
                        int depth = result.Depth + 1;

                        for (int i = children.Count - 1; i >= 0; --i)
                        {
                            Widget child = children[i];
                            if (child.IsEnabled)
                            {
                                LayoutBehaviour behaviour = child.LayoutBehaviour;
                                if (behaviour.AsGroup && HasActualChildrenForGroup(child))
                                    widgetsWithGroups.Enqueue(new GroupSearchData(child, depth, currentGroupOwner.GroupId, ++currentGroupId, behaviour));
                                else
                                    _searchDataStack.Push(new WidgetSearchData(child, depth, currentGroupOwner.GroupId, behaviour));
                            }
                        }
                    }
                }

                Debug.Assert(_searchDataStack.Count == 0);

                if (widgets.Count > 1)
                {
                    _layoutGroups.Add(new LayoutGroup(currentGroupOwner.Widget, currentGroupOwner.GroupId, currentGroupOwner.ParentGroupId, widgets, false));
                }
                else
                {
                    widgets.Dispose();
                }
            }

            Debug.Assert(widgetsWithGroups.Count == 0);

            static bool HasActualChildrenForGroup(Widget widget)
            {
                foreach (Widget child in widget.Children)
                {
                    if (!child.LayoutBehaviour.AsGroup)
                        return true;
                }

                return false;
            }
        }

        private void ClearLayoutData()
        {
            foreach (LayoutGroup layoutGroup in _layoutGroups)
            {
                layoutGroup.Widgets.Dispose();
            }

            _layoutGroups.Clear();
        }

        private readonly record struct GroupSearchData(Widget Widget, int Depth, int ParentGroupId, int GroupId, LayoutBehaviour Behaviour);
        private readonly record struct WidgetSearchData(Widget Widget, int Depth, int ParentGroupId, LayoutBehaviour Behaviour);

        private readonly record struct WidgetData(Widget Widget, int Depth, bool IsListeningToChildren);

        private record struct LayoutGroup(Widget TreeOwner, int GroupId, int ParentGroupId, RentedList<WidgetData> Widgets, bool NeedsMeasure = false, bool NeedsRelayout = false);
    }

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
