using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Widgets;

namespace EditorUI.Layout
{
    public sealed class LogLayoutReporter : ILayoutReporter
    {
        private int _consideredIndex;
        private int _relayoutIndex;
        private int _groupedIndex;

        private LogLayoutReporter()
        {
        }

        public void OnLayoutBegin()
        {
            _consideredIndex = 0;
            _relayoutIndex = 0;
            _groupedIndex = 0;
        }

        public void OnWidgetConsidered(Widget widget, LayoutConsiderType consideredFor)
        {
            UILog.Logger?.Information("CONSIDERED({why}) {idx}: {w} (id: {n})", consideredFor, _consideredIndex++, widget, widget.Id ?? "null");
        }

        public void OnWidgetRelayout(Widget widget)
        {
            UILog.Logger?.Information("RELAYOUT {idx}: {w} (id: {n})", _relayoutIndex++, widget, widget.Id ?? "null");
        }

        public void OnWidgetGrouped(Widget widget)
        {
            UILog.Logger?.Information("GROUPED {idx}: {w} (id: {n})", _groupedIndex++, widget, widget.Id ?? "null");
        }

        public static readonly LogLayoutReporter Instance = new LogLayoutReporter();
    }
}
