using Editor.Geometry;
using Editor.History;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Geo.History
{
    internal sealed class BrushTranslatedStep : IHistoryStep
    {
        private Brush _brush;
        private byte _vertexMask;

        private (Vector3 Old, Vector3 New)[] _vertices;

        internal BrushTranslatedStep(Brush brush, byte vertexMask, Vector3[] oldVertices)
        {
            _brush = brush;
            _vertexMask = vertexMask;

            _vertices = new (Vector3 Old, Vector3 New)[int.PopCount(vertexMask)];

            Span<Vector3> vertices = brush.Vertices;

            int j = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Flags.HasFlag(vertexMask, 1 << i))
                {
                    _vertices[j++] = (oldVertices[i], vertices[i]);
                }
            }
        }

        public void PerformUndo()
        {
            Span<Vector3> vertices = _brush.Vertices;

            int j = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Flags.HasFlag(_vertexMask, 1 << i))
                {
                    vertices[i] = _vertices[j++].Old;
                }
            }
        }

        public void PerformRedo()
        {
            Span<Vector3> vertices = _brush.Vertices;

            int j = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Flags.HasFlag(_vertexMask, 1 << i))
                {
                    vertices[i] = _vertices[j++].New;
                }
            }
        }

        public int EstimatedMemorySize => nint.Size + sizeof(byte) + _vertices.Length * Unsafe.SizeOf<Vector3>() * 2;
        public string? Description => "Translated geometry brush vertices";
    }
}
