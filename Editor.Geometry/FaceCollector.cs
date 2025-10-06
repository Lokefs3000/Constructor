using Arch.LowLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    public sealed class FaceCollector : IDisposable
    {
        private Dictionary<uint, MaterialLayer> _layers;

        private int _vertexCount;
        private int _indexCount;

        private bool _disposedValue;

        internal FaceCollector()
        {
            _layers = new Dictionary<uint, MaterialLayer>();

            _vertexCount = 0;
            _indexCount = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (MaterialLayer layer in _layers.Values)
                {
                    layer.Dispose();
                }

                _layers.Clear();

                _disposedValue = true;
            }
        }

        ~FaceCollector()
        {
            Dispose(disposing: false);
        }

        /// <summary>Not thread-safe</summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not thread-safe</summary>
        public void AddTriangle(uint layer, FaceTriangleRaw triangle)
        {
            if (layer != uint.MaxValue)
            {
                ref MaterialLayer data = ref CollectionsMarshal.GetValueRefOrNullRef(_layers, layer);
                if (Unsafe.IsNullRef(ref data))
                {
                    data = new MaterialLayer();
                    _layers.Add(layer, data);
                }

                data.Triangles.Add(triangle);

                _vertexCount += 3;
                _indexCount += 3;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void AddQuad(uint layer, FaceQuadRaw quad)
        {
            if (layer != uint.MaxValue)
            {
                ref MaterialLayer data = ref CollectionsMarshal.GetValueRefOrNullRef(_layers, layer);
                if (Unsafe.IsNullRef(ref data))
                {
                    data = new MaterialLayer();
                    _layers.Add(layer, data);
                }

                data.Quads.Add(quad);

                _vertexCount += 4;
                _indexCount += 6;
            }
        }

        internal IReadOnlyDictionary<uint, MaterialLayer> Layers => _layers;

        internal int VertexCount => _vertexCount;
        internal int IndexCount => _indexCount;
    }

    public enum FaceDirection : byte
    {
        Normal,
        Inverted
    }

    public readonly record struct FaceVertexRaw
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly Vector2 UV;

        public FaceVertexRaw(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            UV = uv;
        }
    }

    internal struct MaterialLayer : IDisposable
    {
        public readonly UnsafeList<FaceTriangleRaw> Triangles;
        public readonly UnsafeList<FaceQuadRaw> Quads;

        public MaterialLayer()
        {
            Triangles = new UnsafeList<FaceTriangleRaw>(8);
            Quads = new UnsafeList<FaceQuadRaw>(8);
        }

        public void Dispose()
        {
            Triangles.Dispose();
            Quads.Dispose();
        }
    }

    public readonly record struct FaceTriangleRaw(FaceVertexRaw A, FaceVertexRaw B, FaceVertexRaw C);
    public readonly record struct FaceQuadRaw(FaceVertexRaw TopLeft, FaceVertexRaw TopRight, FaceVertexRaw BottomLeft, FaceVertexRaw BottomRight);
}
