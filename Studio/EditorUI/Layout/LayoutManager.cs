using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Dock;
using EditorUI.Statistics;
using EditorUI.Utility;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Collections;
using Primary.Common;
using Primary.Extensions;
using Primary.Mathematics;
using Primary.Utility;
using TerraFX.Interop.Windows;

namespace EditorUI.Layout
{
    public sealed class LayoutManager
    {
        private List<object> _recalculateNext;

        private HashSet<Widget> _failedWidgets;

        internal LayoutManager()
        {
            _recalculateNext = new List<object>();

            _failedWidgets = new HashSet<Widget>();
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

            WidgetWindow? widgetWindow = null;
            if (rootWidget is WindowRoot windowRoot)
            {
                widgetWindow = windowRoot.OwningWindow;
                if (widgetWindow?.Parent != null && widgetWindow.Parent.WindowRect != widgetWindow.WindowRect)
                {
                    widgetWindow.RecalculateLayout(widgetWindow.Parent.WindowRect);
                }
            }

            // measure widgets
            using (new StatTimingScope(ref statistics.MeasurePassTime))
                MeasureDescending(rootWidget, Vector2.Zero, LayoutLockAxis.None, false, ref statistics);

            // layout widgets
            using (new StatTimingScope(ref statistics.LayoutPassTime))
                LayoutDescending(rootWidget, Vector2.Zero, LayoutLockAxis.None, false, ref statistics);

            // finish layout
            using (new StatTimingScope(ref statistics.FinishPassTime))
                FinishDescending(rootWidget, Vector2.Zero, false);

            if (_failedWidgets.Count > 0)
            {
                statistics.FailedWidgets = _failedWidgets.Count;

                foreach (Widget widget in _failedWidgets)
                {
                    widget.AddStateFlags(StateFlags.SelfInvalidLayout);
                }

                _failedWidgets.Clear();
            }

            widgetWindow?.LayoutStatistics = statistics;
        }

        private void MeasureDescending(Widget widget, Vector2 parentSize, LayoutLockAxis lockAxis, bool forcedByAncestor, ref LayoutStatistics statistics)
        {
            if (forcedByAncestor || widget.StateFlags.HasFlag(StateFlags.InvalidLayout))
            {
                bool hasCustomSize = false;

                forcedByAncestor = forcedByAncestor || widget.StateFlags.HasFlag(StateFlags.ThisLayout);
                if (forcedByAncestor)
                {
                    LayoutContext context = new LayoutContext(parentSize, Vector2.Zero, lockAxis);
                    MeasureReturnData status = widget.MeasureSelf(ref context);

                    if (status.Status == MeasureStatus.MissingPendingData)
                    {
                        _failedWidgets.Add(widget);
                        return;
                    }

                    if (widget.Margin != Vector4.Zero)
                    {
                        switch (widget.AutoResize)
                        {
                            case AutoResizeMode.None: widget.IdealSize -= widget.Margin.GetLower() + widget.Margin.GetUpper(); break;
                            case AutoResizeMode.ResizeX: widget.IdealSize = new Vector2(widget.IdealSize.X, widget.IdealSize.Y - widget.Margin.Y - widget.Margin.W); break;
                            case AutoResizeMode.ResizeY: widget.IdealSize = new Vector2(widget.IdealSize.X - widget.Margin.X - widget.Margin.Z, widget.IdealSize.Y); break;
                        }
                    }

                    widget.AddStateFlags(StateFlags.SelfInvalidLayout);

                    lockAxis = status.LockAxis;
                    if (status.CustomSize.HasValue)
                    {
                        hasCustomSize = true;
                        parentSize = status.CustomSize.Value;
                    }
                }
                else
                {
                    if (widget.AutoResize != AutoResizeMode.None)
                        widget.AddStateFlags(StateFlags.SelfInvalidLayout);

                    switch (widget.AutoResize)
                    {
                        case AutoResizeMode.ResizeX: widget.IdealSize = new Vector2(float.NegativeZero, widget.IdealSize.Y); break;
                        case AutoResizeMode.ResizeY: widget.IdealSize = new Vector2(widget.IdealSize.X, float.NegativeZero); break;
                        case AutoResizeMode.ResizeXY: widget.IdealSize = Vector2.NegativeZero; break;
                    }
                }

                if (!hasCustomSize)
                    parentSize = widget.IdealSize;

                if (widget.Padding != Vector4.Zero)
                {
                    switch (widget.AutoResize)
                    {
                        case AutoResizeMode.None: parentSize -= widget.Padding.GetLower() + widget.Padding.GetUpper(); break;
                        case AutoResizeMode.ResizeX: parentSize.Y -= widget.Padding.Y + widget.Padding.W; break;
                        case AutoResizeMode.ResizeY: parentSize.X -= widget.Padding.X + widget.Padding.Z; break;
                    }
                }

                foreach (Widget childWidget in widget.Children)
                {
                    if (childWidget.IsEnabled)
                        MeasureDescending(childWidget, parentSize, lockAxis, forcedByAncestor, ref statistics);
                }
            }
        }

        private void LayoutDescending(Widget widget, Vector2 parentSize, LayoutLockAxis lockAxis, bool forcedByAncestor, ref LayoutStatistics statistics)
        {
            if (_failedWidgets.Contains(widget))
                return;

            if (widget.StateFlags.HasFlag(StateFlags.InvalidLayout))
            {
                ++statistics.UpdatedWidgets;

                LayoutContext context = new LayoutContext(parentSize, Vector2.Zero, lockAxis);
                LayoutReturnData layoutMode = widget.LayoutSelf(ref context);

                parentSize = Vector2.Abs(layoutMode.ContentSize ?? widget.IdealSize);
                lockAxis = layoutMode.LockAxis;

                foreach (Widget childWidget in widget.Children)
                {
                    if (childWidget.IsEnabled)
                        LayoutDescending(childWidget, parentSize, lockAxis, forcedByAncestor, ref statistics);
                }

                if (layoutMode.WantsPostLayout)
                {
                    widget.PostLayoutSelf(ref context);
                }

                switch (widget.AutoResize)
                {
                    case AutoResizeMode.ResizeX:
                        {
                            float maxX = 0.0f;
                            foreach (Widget childWidget in widget.Children)
                            {
                                maxX = Math.Max(maxX, childWidget.IdealSize.X + childWidget.IdealPosition.X);
                            }

                            widget.IdealSize = new Vector2(Math.Max(maxX, 0.0f) + (widget.Padding.X + widget.Padding.Z) - (widget.Margin.X + widget.Margin.Z), widget.IdealSize.Y);
                            break;
                        }
                    case AutoResizeMode.ResizeY:
                        {
                            float maxY = 0.0f;
                            foreach (Widget childWidget in widget.Children)
                            {
                                maxY = Math.Max(maxY, childWidget.IdealSize.Y + childWidget.IdealPosition.Y);
                            }

                            widget.IdealSize = new Vector2(widget.IdealSize.X, Math.Max(maxY, 0.0f) + (widget.Padding.Y + widget.Padding.W) - (widget.Margin.Y + widget.Margin.W));
                            break;
                        }
                    case AutoResizeMode.ResizeXY:
                        {
                            Vector2 max = Vector2.Zero;
                            foreach (Widget childWidget in widget.Children)
                            {
                                max = Vector2.Max(max, childWidget.IdealSize + childWidget.IdealPosition);
                            }

                            widget.IdealSize = Vector2.Max(max + (widget.Padding.GetLower() + widget.Padding.GetUpper()) - (widget.Margin.GetLower() + widget.Margin.GetUpper()), Vector2.Zero);
                            break;
                        }
                }

                widget.FinalizeSelf(ref context);
            }
        }

        private void FinishDescending(Widget widget, Vector2 parentOffset, bool forcedByAncestor)
        {
            if (_failedWidgets.Contains(widget))
                return;

            if (widget.StateFlags.HasFlag(StateFlags.InvalidLayout))
            {
                Vector2 screenPosition = widget.IdealPosition + parentOffset + widget.Margin.GetLower();
                widget.ComputedRect = new Boundaries(screenPosition, screenPosition + widget.IdealSize);

                parentOffset = screenPosition + widget.Padding.GetLower();

                foreach (Widget childWidget in widget.Children)
                {
                    if (childWidget.IsEnabled)
                        FinishDescending(childWidget, parentOffset, forcedByAncestor);
                }
            }

            widget.RemoveStateFlags(StateFlags.SelfInvalidLayout);
        }

        private record struct LayoutGroup(Widget? TreeOwner, int TreeDepth, RentedList<Widget> Widgets);
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

        MissingPendingData
    }
}
