using System;
using System.Collections.Generic;
using System.Text;
using VoxelizationDemo.Editor.History;

namespace VoxelizationDemo.Editor
{
    public static partial class UndoHistory
    {
        public static void Record(IHistoryStep step) => EditorManager.Instance.HistoryManager.Record(step);
        public static void Flush() => EditorManager.Instance.HistoryManager.Flush();
        public static void Undo() => EditorManager.Instance.HistoryManager.Undo();
        public static void Redo() => EditorManager.Instance.HistoryManager.Redo();
    }
}
