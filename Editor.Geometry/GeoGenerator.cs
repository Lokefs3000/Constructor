using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Editor.Geometry.Shapes;
using Primary.Assets;
using Primary.Common;
using System.Buffers;
using System.Numerics;
using Array = System.Array;

namespace Editor.Geometry
{
    public sealed class GeoGenerator : IDisposable
    {
        private readonly GeoVertexCache _vertexCache;

        private List<CachedGeoShape> _activeShapes;
        private HashSet<uint> _cachedLayers;

        private List<GeoRenderMesh> _renderMeshes;
        private UnsafeList<GeoVertex> _vertices;
        private UnsafeList<ushort> _indices;

        private bool _disposedValue;

        public GeoGenerator(GeoVertexCache vertexCache)
        {
            _vertexCache = vertexCache;

            _activeShapes = new List<CachedGeoShape>();
            _cachedLayers = new HashSet<uint>();

            _renderMeshes = new List<GeoRenderMesh>();
            _vertices = new UnsafeList<GeoVertex>();
            _indices = new UnsafeList<ushort>();
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

        ~GeoGenerator()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void GenerateMesh(GeoBrushScene scene)
        {
            _activeShapes.Clear();
            _cachedLayers.Clear();

            _vertices.Clear();
            _indices.Clear();

            foreach (GeoBrush brush in scene.Brushes)
            {
                if (brush.Shape != null)
                {
                    IGeoShape shape = brush.Shape;
                    if (!_vertexCache.Retrieve(shape, out CachedGeoShape cachedShape) || shape.IsDirty)
                    {
                        GeoMesh mesh = shape.GenerateMesh();
                        cachedShape = Transform(mesh, brush.Transform);

                        _vertexCache.Store(shape, cachedShape);
                    }

                    _activeShapes.Add(cachedShape);
                }
            }

            if (_activeShapes.Count > 0)
            {
                HashSet<ushort> uniqueIds = new HashSet<ushort>();

                Span<CachedGeoShape> geoShapes = _activeShapes.AsSpan();
                int totalLayerCount = CountTotalLayers(geoShapes);

                using PoolArray<SortedGeoLayer> borrowedArray1 = ArrayPool<SortedGeoLayer>.Shared.Rent(totalLayerCount);

                Span<SortedGeoLayer> sorted = borrowedArray1.AsSpan(0, totalLayerCount);
                sorted.Sort();

                MaterialAsset? activeMaterial = null;

                int currentVtxOffset = 0;
                int startIdxOffset = 0;
                for (int i = 0; i < sorted.Length; i++)
                {
                    ref SortedGeoLayer sort = ref sorted[i];
                    ref CachedGeoShape shape = ref geoShapes[sort.ShapeIndex];
                    ref GeoShapeLayer layer = ref shape.Layers[sort.LayerIndex];

                    int indexCount = (sort.LayerIndex < shape.Layers.Length - 1) ? shape.Layers[sort.LayerIndex + 1].IndexOffset - layer.IndexOffset : shape.Indices.Length - layer.IndexOffset;
                    Span<ushort> indices = shape.Indices.AsSpan(layer.IndexOffset, indexCount);

                    if (layer.Material != activeMaterial)
                    {
                        if (activeMaterial != null)
                        {
                            int meshIndexCount = _indices.Count - startIdxOffset;
                            if (meshIndexCount > 0)
                            {
                                _renderMeshes.Add(new GeoRenderMesh(activeMaterial, currentVtxOffset, startIdxOffset, meshIndexCount));
                            }

                            currentVtxOffset = 0;
                            startIdxOffset = _indices.Count;
                        }

                        activeMaterial = layer.Material;
                    }

                    for (int j = 0; j < indices.Length; j++)
                    {
                        _indices.Add((ushort)(indices[j] + currentVtxOffset));
                        uniqueIds.Add(indices[j]);
                    }

                    foreach (ushort id in uniqueIds)
                    {
                        ref GeoVertex vertex = ref shape.Vertices[id];
                        _vertices.Add(vertex);
                    }

                    currentVtxOffset += uniqueIds.Count;
                }

                if (startIdxOffset < _indices.Count && activeMaterial != null)
                {
                    int meshIndexCount = _indices.Count - startIdxOffset;
                    if (meshIndexCount > 0)
                    {
                        _renderMeshes.Add(new GeoRenderMesh(activeMaterial, currentVtxOffset, startIdxOffset, meshIndexCount));
                    }
                }
            }
        }

        private static int CountTotalLayers(Span<CachedGeoShape> shapes)
        {
            int count = 0;

            for (int i = 0; i < shapes.Length; i++)
            {
                count += shapes[i].Layers.Length;
            }

            return count;
        }

        public static CachedGeoShape Transform(GeoMesh mesh, GeoTransform transform, bool generateMatLayers = true)
        {
            GeoFace[] faces = new GeoFace[mesh.Faces.Length];
            Array.Copy(mesh.Faces, faces, faces.Length);

            int vertexCount = 0, indexCount = 0;
            HashSet<MaterialAsset> uniqueMaterials = new HashSet<MaterialAsset>();

            for (int i = 0; i < mesh.Faces.Length; i++)
            {
                ref GeoFace face = ref mesh.Faces[i];
                if (face.MaterialIndex != null)
                {
                    if (face.Type == GeoFaceType.Triangle)
                    {
                        vertexCount += 3;
                        indexCount += 3;
                    }
                    else
                    {
                        vertexCount += 4;
                        indexCount += 6;
                    }

                    uniqueMaterials.Add(face.MaterialIndex);
                }
            }

            if (vertexCount == 0 || indexCount == 0)
            {
                return new CachedGeoShape(Array.Empty<GeoVertex>(), Array.Empty<ushort>(), Array.Empty<GeoShapeLayer>());
            }

            GeoVertex[] vertices = new GeoVertex[vertexCount];
            ushort[] indices = new ushort[indexCount];

            GeoShapeLayer[] layers = new GeoShapeLayer[uniqueMaterials.Count];

            bool hasRotation = transform.Rotation != Quaternion.Identity;
            bool hasPivot = transform.Origin != Vector3.Zero;

            Vector3 pivotOffset = transform.Position + (hasPivot ? Vector3.Lerp(mesh.Boundaries.Minimum, mesh.Boundaries.Maximum, transform.Origin) : Vector3.Zero);

            int vtxOffset = 0, idxOffset = 0;
            if (generateMatLayers)
            {
                //1. generate layers
                uniqueMaterials.Clear();

                Array.Sort(faces, (a, b) =>
                {
                    ulong aNum = a.MaterialIndex?.Id.Value ?? ulong.MaxValue;
                    ulong bNum = b.MaterialIndex?.Id.Value ?? ulong.MaxValue;
                    return aNum.CompareTo(bNum);
                });

                for (int i = 0, j = 0; i < faces.Length; i++)
                {
                    ref GeoFace face = ref faces[i];
                    if (face.MaterialIndex != null)
                    {
                        if (uniqueMaterials.Add(face.MaterialIndex))
                        {
                            layers[j++] = new GeoShapeLayer(idxOffset, face.MaterialIndex);
                        }

                        idxOffset += face.Type == GeoFaceType.Triangle ? 3 : 6;
                    }
                }
            }

            //2. generate vertices
            vtxOffset = 0;
            idxOffset = 0;

            //TODO: accelerate using SIMD for "vtxOffset" and "idxOffset"
            for (int i = 0; i < mesh.Faces.Length; i++)
            {
                int startVtx = vtxOffset;

                ref GeoFace face = ref mesh.Faces[i];
                if (face.MaterialIndex != null)
                {
                    if (face.Type == GeoFaceType.Triangle)
                    {
                        ref GeoTriangle triangle = ref face.Triangle;

                        Vector3 v0 = mesh.Vertices[triangle.Point0.Index] + pivotOffset;
                        Vector3 v1 = mesh.Vertices[triangle.Point1.Index] + pivotOffset;
                        Vector3 v2 = mesh.Vertices[triangle.Point2.Index] + pivotOffset;

                        if (hasRotation)
                        {
                            v0 = Vector3.Transform(v0, transform.Rotation);
                            v1 = Vector3.Transform(v1, transform.Rotation);
                            v2 = Vector3.Transform(v2, transform.Rotation);
                        }

                        Vector3 edge1 = v1 - v0;
                        Vector3 edge2 = v2 - v0;

                        Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                        vertices[vtxOffset++] = new GeoVertex(v0, normal, triangle.Point0.UV);
                        vertices[vtxOffset++] = new GeoVertex(v1, normal, triangle.Point1.UV);
                        vertices[vtxOffset++] = new GeoVertex(v2, normal, triangle.Point2.UV);

                        indices[idxOffset++] = (ushort)startVtx;
                        indices[idxOffset++] = (ushort)(startVtx + 1);
                        indices[idxOffset++] = (ushort)(startVtx + 2);
                    }
                    else
                    {
                        ref GeoQuad quad = ref face.Quad;

                        Vector3 v0 = mesh.Vertices[quad.Point0.Index] + pivotOffset;
                        Vector3 v1 = mesh.Vertices[quad.Point1.Index] + pivotOffset;
                        Vector3 v2 = mesh.Vertices[quad.Point2.Index] + pivotOffset;
                        Vector3 v3 = mesh.Vertices[quad.Point3.Index] + pivotOffset;

                        if (hasRotation)
                        {
                            v0 = Vector3.Transform(v0, transform.Rotation);
                            v1 = Vector3.Transform(v1, transform.Rotation);
                            v2 = Vector3.Transform(v2, transform.Rotation);
                            v3 = Vector3.Transform(v3, transform.Rotation);
                        }

                        Vector3 edge20 = v2 - v0;
                        Vector3 edge30 = v3 - v0;

                        Vector3 normal023 = Vector3.Normalize(Vector3.Cross(edge20, edge30));

                        vertices[vtxOffset++] = new GeoVertex(v0, normal023, quad.Point0.UV);
                        vertices[vtxOffset++] = new GeoVertex(v1, normal023, quad.Point1.UV);
                        vertices[vtxOffset++] = new GeoVertex(v2, normal023, quad.Point2.UV);
                        vertices[vtxOffset++] = new GeoVertex(v3, normal023, quad.Point3.UV);

                        indices[idxOffset++] = (ushort)startVtx;
                        indices[idxOffset++] = (ushort)(startVtx + 2);
                        indices[idxOffset++] = (ushort)(startVtx + 3);
                        indices[idxOffset++] = (ushort)startVtx;
                        indices[idxOffset++] = (ushort)(startVtx + 3);
                        indices[idxOffset++] = (ushort)(startVtx + 1);
                    }
                }
            }

            return new CachedGeoShape(vertices, indices, layers);
        }

        public Span<GeoVertex> Vertices => _vertices.AsSpan();
        public Span<ushort> Indices => _indices.AsSpan();

        public IReadOnlyList<GeoRenderMesh> Meshes => _renderMeshes;

        private struct MaterialGeoContainer : IDisposable
        {
            internal UnsafeList<GeoVertex> Vertices;
            internal UnsafeList<ushort> Indices;

            public MaterialGeoContainer()
            {
                Vertices = new UnsafeList<GeoVertex>(8);
                Indices = new UnsafeList<ushort>(8);
            }

            public void Dispose()
            {
                Vertices.Dispose();
                Indices.Dispose();
            }
        }

        private readonly record struct SortedGeoLayer(uint SortKey, int ShapeIndex, int LayerIndex) : IComparable<SortedGeoLayer>
        {
            public int CompareTo(SortedGeoLayer other) => SortKey.CompareTo(other.SortKey);
        }
    }

    public readonly record struct GeoRenderMesh(MaterialAsset Material, int BaseVertexOffset, int StartIndexLocation, int IndexCount);
}
