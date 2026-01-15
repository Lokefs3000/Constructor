using Editor.Geometry.Shapes;
using Primary.Assets;
using System.Numerics;

namespace Editor.Geometry
{
    public sealed class GeoVertexCache
    {
        private object _cacheLock;
        private Dictionary<IGeoShape, CachedGeoShape> _cachedShapes;

        public GeoVertexCache()
        {
            _cacheLock = new object();
            _cachedShapes = new Dictionary<IGeoShape, CachedGeoShape>();
        }

        /// <summary>Thread-safe</summary>
        public void Store(IGeoShape shape, CachedGeoShape data)
        {
            lock (_cacheLock)
            {
                _cachedShapes[shape] = data;
            }
        }

        /// <summary>Thread-safe</summary>
        public bool Retrieve(IGeoShape shape, out CachedGeoShape data)
        {
            lock (_cacheLock)
            {
                return _cachedShapes.TryGetValue(shape, out data);
            }
        }
    }

    public readonly record struct CachedGeoShape(GeoVertex[] Vertices, ushort[] Indices, GeoShapeLayer[] Layers);
    public readonly record struct GeoShapeLayer(int IndexOffset, MaterialAsset Material);

    public record struct GeoVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public Vector2 UV0;

        public GeoVertex(Vector3 position, Vector3 normal, Vector2 uv0)
        {
            Position = position;
            Normal = normal;
            Tangent = Vector3.Zero;
            Bitangent = Vector3.Zero;
            UV0 = uv0;
        }
    }
}
