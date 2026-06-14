using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Dock;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Common;
using Primary.Mathematics;
using Primary.Utility;

namespace EditorUI.Layout
{
    public sealed class LayoutManager
    {
        private List<object> _recalculateNext;

        internal LayoutManager()
        {
            _recalculateNext = new List<object>();
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
            if (rootWidget is WindowRoot windowRoot)
            {
                WidgetWindow? window = windowRoot.OwningWindow;
                if (window?.Parent != null && window.Parent.WindowRect != window.WindowRect)
                {
                    window.RecalculateLayout(window.Parent.WindowRect);
                }
            }

            // measure widgets
            MeasureDescending(rootWidget, Vector2.Zero, false);

            // layout widgets
            LayoutDescending(rootWidget, Vector2.Zero, Vector2.Zero, false);
        }

        private void MeasureDescending(Widget widget, Vector2 parentSize, bool forcedByAncestor)
        {
            forcedByAncestor = forcedByAncestor || widget.StateFlags.HasFlags(StateFlags.ThisLayout);

            if (forcedByAncestor)
            {
                LayoutContext context = new LayoutContext(parentSize, Vector2.Zero, false);
                widget.MeasureSelf(ref context);

                parentSize = widget.IdealSize;
                foreach (Widget childWidget in widget.Children)
                {
                    if (childWidget.IsEnabled)
                        MeasureDescending(childWidget, parentSize, forcedByAncestor);
                }
            }
        }

        private void LayoutDescending(Widget widget, Vector2 parentSize, Vector2 parentOffset, bool forcedByAncestor, bool lockLayout = false)
        {
            forcedByAncestor = forcedByAncestor || widget.StateFlags.HasFlags(StateFlags.ThisLayout);

            if (forcedByAncestor)
            {
                LayoutContext context = new LayoutContext(parentSize, Vector2.Zero, lockLayout);
                ChildrenLayoutMode layoutMode = widget.LayoutSelf(ref context);

                parentSize = widget.IdealSize;
                parentOffset = widget.ComputedRect.Minimum;

                lockLayout = layoutMode.HasFlags(ChildrenLayoutMode.LayoutPositionLocked);

                foreach (Widget childWidget in widget.Children)
                {
                    if (childWidget.IsEnabled)
                        LayoutDescending(childWidget, parentSize, parentOffset, forcedByAncestor);
                }
            }

            widget.RemoveStateFlags(StateFlags.SelfInvalidLayout);
        }
    }

    public enum ChildrenLayoutMode : byte
    {
        None = 0,

        LayoutPositionLocked,
    }
}
