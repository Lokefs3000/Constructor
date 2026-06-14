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
    internal sealed class AddBrushStep : IHistoryStep
    {
        private readonly BrushGroup _group;

        private readonly Brush _brush;
        private readonly int _brushId;

        private readonly Vector3 _offset;

        internal AddBrushStep(Brush brush, Vector3 offset)
        {
            _group = brush.Group;

            _brush = brush;
            _brushId = brush.Id.LocalId;

            _offset = offset;
        }

        public void PerformUndo()
        {
            _group.DestroyBrush(_brush);
        }

        public void PerformRedo()
        {
            _group.AddBrush(_brush);

            foreach (ref Vector3 vertex in _brush.Vertices)
            {
                vertex += _offset;
            }
        }

        public int EstimatedMemorySize => nint.Size * 2 + sizeof(int) + Unsafe.SizeOf<Vector3>();
        public string? Description => "Added new geometry brush";
    }
}
