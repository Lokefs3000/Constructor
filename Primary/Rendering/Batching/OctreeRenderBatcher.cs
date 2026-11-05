using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.Rendering.Tree;
using Primary.Threading;

namespace Primary.Rendering.Batching
{
    internal sealed class OctreeRenderBatcher : IJob
    {
        private readonly byte _id;
        private readonly RenderingManager _manager;

        private Queue<RenderOctant> _octants;

        private Dictionary<ShaderAsset, ushort> _shaderSourceIndices;
        private Dictionary<IRenderMeshSource, ushort> _meshSourceIndices;
        private Dictionary<MaterialAsset, uint> _materialSourceIndices;

        private PooledList<RenderKey> _renderingKeys;
        private PooledList<UnbatchedRenderFlag> _renderingData;

        private ConcurrentQueue<BatchRequestData> _batchingQueue;

        private readonly ushort[] _octantVisibilityArray;

        internal OctreeRenderBatcher(byte id, RenderingManager manager)
        {
            _id = id;
            _manager = manager;

            _octants = new Queue<RenderOctant>();

            _shaderSourceIndices = new Dictionary<ShaderAsset, ushort>();
            _meshSourceIndices = new Dictionary<IRenderMeshSource, ushort>();
            _materialSourceIndices = new Dictionary<MaterialAsset, uint>();

            _renderingKeys = new PooledList<RenderKey>();
            _renderingData = new PooledList<UnbatchedRenderFlag>();

            _batchingQueue = new ConcurrentQueue<BatchRequestData>();

            _octantVisibilityArray = new ushort[byte.MaxValue];
        }

        internal void ClearFrameData()
        {
            _shaderSourceIndices.Clear();
            _meshSourceIndices.Clear();
            _materialSourceIndices.Clear();

            _renderingKeys.Clear();
            _renderingData.Clear();
        }

        internal void Execute(RenderList list, RenderOctree octree, in BatchingOptions options)
        {
            using (new ProfilingScope("BatchOctree"))
            {
                using (new ProfilingScope("OctantVisibility"))
                {
                    ComputeOctantVisibility(octree, in options);
                }

                using (new ProfilingScope("Iterate"))
                {
                    Vector3 origin = options.Origin;
                    float sqrMaxViewDist = options.MaxViewingDistance * options.MaxViewingDistance;
                    Frustrum viewFrustrum = options.ViewFrustrum;

                    MaterialShaderType shaderType = options.ShaderType;
                    ulong allowedLayers = options.AllowedLayers;

                    ushort[] visibilityMasks = _octantVisibilityArray;

                    foreach (ref readonly EntityChunk chunk in octree.Chunks)
                    {
                        foreach (ref RenderableEntity entity in chunk.Entities)
                        {
                            if ((entity.Layers & allowedLayers) == 0)
                                continue;

                            float sqrDistFromOrigin = Vector3.DistanceSquared(entity.Extents.Center, origin);
                            if (sqrDistFromOrigin - entity.SqrBoundingRadius > sqrMaxViewDist)
                                continue;

                            ref ushort visibilityMask = ref visibilityMasks.DangerousGetReferenceAt(entity.OctantIndex);
                            FrustrumIntersection octantIntersection = (FrustrumIntersection)((visibilityMask >> entity.OctantBitshift) & 0x3);
                            if (octantIntersection != FrustrumIntersection.Outside && (octantIntersection == FrustrumIntersection.Inside || entity.Extents.IntersectAny(in viewFrustrum)))
                            {
                                MaterialAsset material = (entity.Material == null || entity.Material.GetShaderOrNull(shaderType) == null) ? list.DefaultMaterial! : entity.Material;
                                ShaderAsset? shader = material.GetShaderOrNull(shaderType)!;

                                if (!material.IsLoaded || shader == null || !shader.IsLoaded)
                                    continue;

                                IRawRenderMesh? mesh = entity.Mesh;
                                if (mesh == null)
                                    continue;

                                IRenderMeshSource? meshSource = mesh.MeshSource;
                                if (meshSource == null || !meshSource.IsLoaded)
                                    continue;

                                if (!_shaderSourceIndices.TryGetValue(shader, out ushort shaderId))
                                {
                                    shaderId = list.GetShaderId(shader);
                                    _shaderSourceIndices.Add(shader, shaderId);
                                }

                                if (!_meshSourceIndices.TryGetValue(meshSource, out ushort modelId))
                                {
                                    modelId = list.GetModelId(meshSource);
                                    _meshSourceIndices.Add(meshSource, modelId);
                                }

                                if (!_materialSourceIndices.TryGetValue(material, out uint materialId))
                                {
                                    materialId = list.GetMaterialId(material);
                                    _materialSourceIndices.Add(material, materialId);
                                }

                                Matrix4x4 transform = entity.Transform;

                                // HACK: The mesh id WILL eventually break this
                                _renderingKeys.Add(new RenderKey(shaderId, modelId, materialId, (ushort)((uint)mesh.UniqueId % 0x3ff), _renderingData.Count, _id));
                                _renderingData.Add(new UnbatchedRenderFlag(material, mesh, transform));
                            }
                        }
                    }
                }
            }
        }

        void IJob.Execute()
        {
            if (_batchingQueue.TryDequeue(out BatchRequestData requestData))
            {
                Execute(requestData.List, requestData.Octree, requestData.Options);
            }
        }

        private void ComputeOctantVisibility(RenderOctree octree, in BatchingOptions options)
        {
            if (octree.Octants != null)
            {
                Frustrum viewFrustrum = options.ViewFrustrum;
                foreach (RenderOctant subOctant in octree.Octants)
                {
                    SearchRecursive(subOctant, 0, in viewFrustrum);
                }
            }

            if (octree.CurrentExtents.IntersectAny(options.ViewFrustrum))
            {
                _octantVisibilityArray[0] = ushort.MaxValue;
            }

            void SearchRecursive(RenderOctant octant, byte entityIndex, in Frustrum viewFrustrum)
            {
                FrustrumIntersection intersection = octree.SourceExtents.Intersect(in viewFrustrum);
                if (intersection != FrustrumIntersection.Outside)
                {
                    ref ushort visbilityMask = ref _octantVisibilityArray[entityIndex];
                    visbilityMask |= (ushort)(((ushort)intersection) << octant.EntityBitShift);
                }

                if (octant.Octants != null)
                {
                    foreach (RenderOctant subOctant in octant.Octants)
                    {
                        SearchRecursive(subOctant, octant.EntityIndex, in viewFrustrum);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref readonly UnbatchedRenderFlag GetFlagRef(int index)
        {
            Debug.Assert((uint)index < _renderingData.Count);
            return ref _renderingData.Span.DangerousGetReferenceAt(index);
        }

        internal void AddToQueue(RenderList list, RenderOctree octree, in BatchingOptions options)
        {
            _batchingQueue.Enqueue(new BatchRequestData(list, octree, options));
        }

        internal ReadOnlySpan<RenderKey> Keys => _renderingKeys.Span;
        internal ReadOnlySpan<UnbatchedRenderFlag> Flags => _renderingData.Span;

        private readonly record struct BatchRequestData(RenderList List, RenderOctree Octree, BatchingOptions Options);
    }
}
