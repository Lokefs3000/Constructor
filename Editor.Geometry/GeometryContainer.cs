using Arch.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    public sealed class GeometryContainer : IDisposable
    {
        private UnsafeList<GeoVertex> _vertices;
        private UnsafeList<ushort> _indices;

        private bool _disposedValue;

        internal GeometryContainer()
        {
            _vertices = new UnsafeList<GeoVertex>(8);
            _indices = new UnsafeList<ushort>(8);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _vertices.Dispose();
                _indices.Dispose();

                _disposedValue = true;
            }
        }

        ~GeometryContainer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResizeBuffers(int vertexCount, int indexCount)
        {
            _vertices.Clear();
            _indices.Clear();

            _vertices.EnsureCapacity(vertexCount);
            _indices.EnsureCapacity(indexCount);
        }

        internal ushort AddVertex(GeoVertex vertex)
        {
            ushort prev = (ushort)_vertices.Count;
            _vertices.Add(vertex);

            return prev;
        }

        internal void AddTriangle(ushort v1, ushort v2, ushort v3)
        {
            _indices.Add(v1);
            _indices.Add(v2);
            _indices.Add(v3);
        }

        public Span<GeoVertex> Vertices => _vertices.AsSpan();
        public Span<ushort> Indices => _indices.AsSpan();
    }
}
