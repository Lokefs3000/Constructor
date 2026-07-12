using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Widgets;

namespace EditorUI.Layout
{
    public interface ILayoutReporter
    {
        public void OnWidgetConsidered(Widget widget);
        public void OnWidgetRelayout(Widget widget);
        public void OnWidgetGrouped(Widget widget);
    }
}
