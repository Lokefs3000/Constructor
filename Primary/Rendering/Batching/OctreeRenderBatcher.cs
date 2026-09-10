using Arch.Core;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Components;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.Rendering.Statistics;
using Primary.Rendering.Tree;
using Primary.Scenes;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Batching
{
    internal sealed class OctreeRenderBatcher
    {
        private readonly byte _id;
        private readonly RenderingManager _manager;

        private Queue<RenderOctant> _octants;

        private Dictionary<ShaderAsset, ushort> _shaderSourceIndices;
        private Dictionary<IRenderMeshSource, ushort> _meshSourceIndices;
        private Dictionary<MaterialAsset, uint> _materialSourceIndices;

        private PooledList<RenderKey> _renderingKeys;
        private PooledList<UnbatchedRenderFlag> _renderingData;

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
        }

        internal void ClearFrameData()
        {
            _shaderSourceIndices.Clear();
            _meshSourceIndices.Clear();
            _materialSourceIndices.Clear();

            _renderingKeys.Clear();
            _renderingData.Clear();
        }

        internal void Execute(RenderList list, RegionOctree octree, in Frustrum cullingFrustrum)
        {
            using (new ProfilingScope("BatchOctree"))
            {
                ref BatchStatistics batchStats = ref _manager.Statistics.Batch;

                _octants.Clear();
                _octants.Enqueue(octree.RootOctant);

                if (octree.Children.Count > 0)
                {
                    ++batchStats.RegionsIterated;
                    BatchEntitiesWithinOctant(list, octree.Children.AsSpan(), in cullingFrustrum, ref batchStats);
                }

                while (_octants.TryDequeue(out RenderOctant? octant))
                {
                    if (!cullingFrustrum.Intersects(octant.Boundaries))
                        continue;

                    if (octant.Children.Count > 0)
                    {
                        ++batchStats.OctantsTraversed;
                        BatchEntitiesWithinOctant(list, octant.Children.AsSpan(), in cullingFrustrum, ref batchStats);
                    }

                    foreach (RenderOctant subOctant in octant.Octants)
                    {
                        _octants.Enqueue(subOctant);
                    }
                }

                batchStats.OctantObjectsPassed += _renderingKeys.Count;
            }
        }

        private void BatchEntitiesWithinOctant(RenderList list, ReadOnlySpan<SceneEntity> entities, in Frustrum cullingFrustrum, ref BatchStatistics batchStats)
        {
            World world = Engine.GlobalSingleton.SceneManager.World;

            for (int i = 0; i < entities.Length; i++)
            {
                ref readonly SceneEntity entity = ref entities[i];
                ref readonly EntityData rawData = ref world.GetEntityData(entity.WrappedEntity);

                ref readonly EntityEnabled enabled = ref rawData.Get<EntityEnabled>();
                if (!enabled.Enabled)
                    continue;

                ref readonly MeshRenderer renderer = ref rawData.Get<MeshRenderer>();
                //"renderer" SHOULD not be null but a crash will occur if it is
                if (!Unsafe.IsNullRef(in renderer) && renderer.Mesh != null)
                {
                    ref readonly RenderBounds bounds = ref rawData.Get<RenderBounds>();
                    ref readonly WorldTransform transform = ref rawData.Get<WorldTransform>();

                    Debug.Assert(!Unsafe.IsNullRef(in bounds));
                    Debug.Assert(!Unsafe.IsNullRef(in transform));

                    if (!cullingFrustrum.Intersects(bounds.ComputedBounds))
                        continue;

                    MaterialAsset material = (renderer.Material == null || renderer.Material.Shader == null) ? list.DefaultMaterial! : renderer.Material;
                    ShaderAsset? shader = material.Shader;

                    if (shader == null)
                        continue;

                    IRawRenderMesh mesh = renderer.Mesh;
                    IRenderMeshSource? meshSource = mesh.MeshSource;

                    if (meshSource == null || !meshSource.IsLoaded)
                        continue;

                    if (shader == null)
                    {
                        material = list.DefaultMaterial!;
                        shader = list.DefaultMaterial!.Shader!;
                    }

                    if (!shader.IsLoaded || !material.IsLoaded)
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

                    _renderingKeys.Add(new RenderKey(shaderId, modelId, materialId, (ushort)mesh.UniqueId, _renderingData.Count, _id));
                    _renderingData.Add(new UnbatchedRenderFlag(material, mesh, transform.Transformation));
                }
            }

            batchStats.OctantObjectsConsidered += entities.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref readonly UnbatchedRenderFlag GetFlagRef(int index)
        {
            Debug.Assert((uint)index < _renderingData.Count);
            return ref _renderingData.Span.DangerousGetReferenceAt(index);
        }

        internal ReadOnlySpan<RenderKey> Keys => _renderingKeys.Span;
        internal ReadOnlySpan<UnbatchedRenderFlag> Flags => _renderingData.Span;
    }
}
