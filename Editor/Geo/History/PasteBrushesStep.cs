using Editor.Geometry;
using Editor.History;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Geo.History
{
    internal sealed class PasteBrushesStep : IHistoryStep
    {
        private readonly BrushGroup _group;
        private readonly ImmutableArray<Brush> _brushes;

        internal PasteBrushesStep(BrushGroup group, ImmutableArray<Brush> brushes)
        {
            _group = group;
            _brushes = brushes;
        }

        public void PerformUndo()
        {
            foreach (Brush brush in _brushes)
            {
                _group.DestroyBrush(brush);
            }
        }

        public void PerformRedo()
        {
            foreach (Brush brush in _brushes)
            {
                _group.AddBrush(brush);
            }
        }

        public int EstimatedMemorySize => nint.Size * (_brushes.Length + 1);
        public string? Description => "Brushes pasted from clipboard";
    }
}
