using Editor.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace Editor.Geo.Clipboard
{
    internal sealed class BrushClipboardData
    {
        private readonly ImmutableArray<Vector3> _vertices;

        internal BrushClipboardData(IEnumerable<Brush> brushes)
        {
            Vector3[] temp = new Vector3[brushes.Count() * 8];

            int i = 0;
            foreach (Brush brush in brushes)
            {
                brush.Vertices.CopyTo(temp.AsSpan(i, 8));
                i += 8;
            }

            _vertices = [.. temp];
        }

        public ImmutableArray<Vector3> Vertices => _vertices;
    }
}
