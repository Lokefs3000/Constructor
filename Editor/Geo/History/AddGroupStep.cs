using Editor.Geometry;
using Editor.History;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Geo.History
{
    internal sealed class AddGroupStep : IHistoryStep
    {
        private BrushGroup _group;

        internal AddGroupStep(BrushGroup group)
        {
            _group = group;
        }

        public void PerformUndo()
        {
            _group.Scene.DestroyGroup(_group);
        }

        public void PerformRedo()
        {
            _group.Scene.AddGroup(_group);
        }

        public int EstimatedMemorySize => nint.Size;
        public string? Description => "Added new geometry group";
    }
}
