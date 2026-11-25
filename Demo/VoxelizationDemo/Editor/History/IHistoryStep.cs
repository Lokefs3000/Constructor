using System;
using System.Collections.Generic;
using System.Text;

namespace VoxelizationDemo.Editor.History
{
    public interface IHistoryStep
    {
        public void Undo();
        public void Redo();
    }
}
