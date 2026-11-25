using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.History
{
    public interface IHistoryStep
    {
        public void PerformUndo();
        public void PerformRedo();

        public int EstimatedMemorySize { get; }
        public string?Description { get; }
    }
}
