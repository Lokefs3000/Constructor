using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Widgets;

namespace EditorUI.Layout
{
    public interface ILayoutReporter
    {
        public void OnLayoutBegin();

        public void OnWidgetConsidered(Widget widget, LayoutConsiderType consideredFor);
        public void OnWidgetRelayout(Widget widget);
        public void OnWidgetGrouped(Widget widget);
    }

    public enum LayoutConsiderType : byte
    {
        Measure,
        Layout
    }
}
