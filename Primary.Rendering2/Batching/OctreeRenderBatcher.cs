using Arch.Core;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering.Data;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Temporary;
using Primary.Rendering2.Tree;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Batching
{
    internal sealed class OctreeRenderBatcher
    {
        private readonly byte _id;

        private Queue<RenderOctant> _octants;

        private Dictionary<ShaderAsset2, ushort> _shaderSourceIndices;
        private Dictionary<IRenderMeshSource, ushort> _meshSourceIndices;
        private Dictionary<MaterialAsset2, uint> _materialSourceIndices;

        private PooledList<RenderKey> _renderingKeys;
        private PooledList<UnbatchedRenderFlag> _renderingData;

        internal OctreeRenderBatcher(byte id)
        {
            _id = id;

            _octants = new Queue<RenderOctant>();

            _shaderSourceIndices = new Dictionary<ShaderAsset2, ushort>();
            _meshSourceIndices = new Dictionary<IRenderMeshSource, ushort>();
            _materialSourceIndices = new Dictionary<MaterialAsset2, uint>();

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

        internal void Execute(RenderList list, RegionOctree octree)
        {
            using (new ProfilingScope("BatchOctree"))
            {
                _octants.Clear();
                _octants.Enqueue(octree.RootOctant);

                while (_octants.TryDequeue(out RenderOctant? octant))
                {
                    if (octant.Children.Count > 0)
                    {
                        BatchEntitiesWithinOctant(list, octant);
                    }

                    foreach (RenderOctant subOctant in octant.Octants)
                    {
                        _octants.Enqueue(subOctant);
                    }
                }
            }
        }

        private void BatchEntitiesWithinOctant(RenderList list, RenderOctant octant)
        {
            World world = Engine.GlobalSingleton.SceneManager.World;

            ReadOnlySpan<SceneEntity> entities = octant.ChildrenList.AsSpan();
            for (int i = 0; i < entities.Length; i++)
            {
                ref readonly SceneEntity entity = ref entities[i];
                ref readonly EntityData rawData = ref world.GetEntityData(entity.WrappedEntity);

                ref readonly MeshRenderer2 renderer = ref rawData.Get<MeshRenderer2>();
                //"renderer" SHOULD not be null but a crash will occur if it is
                if (!Unsafe.IsNullRef(in renderer) && renderer.Mesh != null)
                {
                    ref readonly RenderBounds bounds = ref rawData.Get<RenderBounds>();
                    ref readonly WorldTransform transform = ref rawData.Get<WorldTransform>();

                    Debug.Assert(!Unsafe.IsNullRef(in bounds));
                    Debug.Assert(!Unsafe.IsNullRef(in transform));

                    /*
                        TODO: CULL OBJECT 
                    */

                    MaterialAsset2 material = renderer.Material ?? list.DefaultMaterial!;
                    ShaderAsset2? shader = material.Shader;

                    RawRenderMesh mesh = renderer.Mesh;

                    if (shader == null)
                    {
                        material = list.DefaultMaterial!;
                        shader = list.DefaultMaterial!.Shader!;
                    }

                    if (!_shaderSourceIndices.TryGetValue(shader, out ushort shaderId))
                    {
                        shaderId = list.GetShaderId(shader);
                        _shaderSourceIndices.Add(shader, shaderId);
                    }

                    if (!_meshSourceIndices.TryGetValue(mesh.Source, out ushort modelId))
                    {
                        modelId = list.GetModelId(mesh.Source);
                        _meshSourceIndices.Add(mesh.Source, modelId);
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
