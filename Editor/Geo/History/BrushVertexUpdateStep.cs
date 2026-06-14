using Editor.Geometry;
using Editor.History;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Geo.History
{
    internal sealed class BrushVertexUpdateStep : IHistoryStep
    {
        private readonly Brush _brush;

        private readonly byte _updatedVertices;
        private readonly (Vector3 Old, Vector3 New)[] _vertices;

        internal BrushVertexUpdateStep(Brush brush, byte updatedVertices)
        {
            _brush = brush;

            _updatedVertices = updatedVertices;
            _vertices = new (Vector3 Old, Vector3 New)[int.PopCount(updatedVertices)];

            Span<Vector3> vertices = brush.Vertices;

            int j = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Flags.HasFlag(_updatedVertices, 1 << i))
                {
                    _vertices[j++] = (vertices[i], Vector3.Zero);
                }
            }
        }

        internal void CaptureNewVertices()
        {
            Span<Vector3> vertices = _brush.Vertices;

            int j = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Flags.HasFlag(_updatedVertices, 1 << i))
                {
                    _vertices[j] = (_vertices[j].Old, vertices[i]);
                    ++j;
                }
            }
        }

        public void PerformUndo()
        {
            Span<Vector3> vertices = _brush.Vertices;

            int j = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Flags.HasFlag(_updatedVertices, 1 << i))
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
                if (Flags.HasFlag(_updatedVertices, 1 << i))
                {
                    vertices[i] = _vertices[j++].New;
                }
            }
        }

        public int EstimatedMemorySize => nint.Size + sizeof(byte) + _vertices.Length * Unsafe.SizeOf<Vector3>() * 2;
        public string? Description => "Changed geometry brush vertices";
    }
}
