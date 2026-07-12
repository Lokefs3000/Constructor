using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Statistics
{
    public record struct VisualStatistics
    {
        public TimeSpan GatherCommands;
        public TimeSpan PaintBuildTime;
    }
}
