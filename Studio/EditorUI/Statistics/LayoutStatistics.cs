using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Statistics
{
    public record struct LayoutStatistics
    {
        public TimeSpan MeasurePassTime;
        public TimeSpan LayoutPassTime;
        public TimeSpan FinishPassTime;

        public int UpdatedWidgets;
        public int FailedWidgets;
    }
}
