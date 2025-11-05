using Arch.Core;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Tree;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering
{
    internal sealed class RenderTreeSubCollector
    {
        private MaterialAsset _defaultMaterial;

        private Queue<RenderTreeNode> _validNodes;

        private Dictionary<IRenderMeshSource, uint> _meshSourceIndices;
        private Dictionary<MaterialAsset, uint> _materialSourceIndices;

        private PooledList<RenderKey> _renderingIndices;
        private PooledList<UnbatchedRenderData> _renderEntities;

        private Dictionary<ShaderAsset, int> _uniqueShaders;

        private Stack<(int l, int r)> _sortStack;

        internal RenderTreeSubCollector()
        {
            _defaultMaterial = AssetManager.LoadAsset<MaterialAsset>("Engine/Materials/Missing.mat");

            _validNodes = new Queue<RenderTreeNode>();

            _meshSourceIndices = new Dictionary<IRenderMeshSource, uint>();
            _materialSourceIndices = new Dictionary<MaterialAsset, uint>();

            _renderingIndices = new PooledList<RenderKey>();
            _renderEntities = new PooledList<UnbatchedRenderData>();

            _uniqueShaders = new Dictionary<ShaderAsset, int>();

            _sortStack = new Stack<(int, int)>();
        }

        internal void Execute(RenderTreeCollector collector, RenderBatcher batcher, byte index)
        {
            using (new ProfilingScope("BatchTree"))
            {
                _validNodes.Clear();

                _meshSourceIndices.Clear();
                _materialSourceIndices.Clear();

                _renderingIndices.Clear();
                _renderingIndices.TrimExcess();

                _renderEntities.Clear();
                _renderEntities.TrimExcess();

                _uniqueShaders.Clear();

                while (collector.TryPopPendingTree(out RenderTree? tree))
                {
                    RecursiveCullNodes(tree, tree.RootNode);

                    while (_validNodes.TryDequeue(out RenderTreeNode? node))
                    {
                        BatchEntitiesWithinNode(batcher, node, index);
                    }
                }
            }
        }

        internal Task ExecuteAsTask(RenderTreeCollector collector, RenderBatcher batcher, byte index) => Task.Factory.StartNew(() => Execute(collector, batcher, index));

        private void RecursiveCullNodes(RenderTree tree, RenderTreeNodeData nodeData)
        {
            if (nodeData.IsLeafNode)
            {
                _validNodes.Enqueue(nodeData.Node);
            }
            else
            {
                foreach (int indice in nodeData.Subnodes)
                {
                    RecursiveCullNodes(tree, tree.GetTreeNode(indice));
                }
            }
        }

        private void BatchEntitiesWithinNode(RenderBatcher batcher, RenderTreeNode node, byte index)
        {
            World world = Engine.GlobalSingleton.SceneManager.World;

            MaterialAsset defaultMaterial = _defaultMaterial;
            uint defaultMaterialIndex = batcher.GetMaterialIndex(_defaultMaterial);

            _renderingIndices.Capacity = Math.Max(node.Children!.Count, _renderingIndices.Count);
            _renderEntities.Capacity = Math.Max(node.Children!.Count, _renderEntities.Capacity);

            int i = 0;
            foreach (TreeEntityData entityData in node.Children!)
            {
                ref EntityData compData = ref world.GetEntityData(entityData.Entity.WrappedEntity);

                ref MeshRenderer renderer = ref compData.Get<MeshRenderer>();
                if (!Unsafe.IsNullRef(ref renderer))
                {
                    {
                        ref RenderBounds bounds = ref compData.Get<RenderBounds>();
                        if (!Unsafe.IsNullRef(ref bounds))
                        {

                        }
                    }

                    if (renderer.Mesh != null)
                    {
                        MaterialAsset? material = renderer.Material ?? defaultMaterial;
                        ShaderAsset? shader = material.Shader;

                        if (shader != null)
                        {
                            if (!_materialSourceIndices.TryGetValue(material, out uint materialId))
                            {
                                materialId = batcher.GetMaterialIndex(material);
                                _materialSourceIndices.Add(material, materialId);
                            }

                            if (!_meshSourceIndices.TryGetValue(renderer.Mesh.Source, out uint sourceId))
                            {
                                sourceId = batcher.GetMeshSourceIndex(renderer.Mesh.Source);
                                _meshSourceIndices.Add(renderer.Mesh.Source, sourceId);
                            }

                            ref WorldTransform transform = ref compData.Get<WorldTransform>();

                            if (!_uniqueShaders.ContainsKey(shader))
                            {
                                _uniqueShaders.Add(shader, _renderingIndices.Count);
                            }

                            _renderingIndices.Add(new RenderKey(shader.Id, sourceId, materialId, (uint)renderer.Mesh.UniqueId, index, _renderEntities.Count));
                            _renderEntities.Add(new UnbatchedRenderData(renderer.Mesh, material, transform.Transformation));
                        }
                    }
                }
            }

            //using (new ProfilingScope("Quicksort"))
            //{
            //    //QuickSort(_renderEntities.Span, 0, _renderEntities.Count - 1);
            //    _renderingIndices.Span.Sort();
            //}
        }

        internal ref readonly UnbatchedRenderData GetEntityRef(int index) => ref _renderEntities.Span.DangerousGetReferenceAt(index);

        internal ReadOnlySpan<RenderKey> Keys => _renderingIndices.Span;
        internal ReadOnlySpan<UnbatchedRenderData> Entities => _renderEntities.Span;

        internal IReadOnlyDictionary<ShaderAsset, int> UniqueShaders => _uniqueShaders;

        //private void QuickSort(Span<IntermediateRenderEntity> span, int l, int r)
        //{
        //    int le = 0, ri = 0, q = 0;
        //
        //    _sortStack.Clear();
        //    _sortStack.Push((l, r));
        //
        //    while (_sortStack.TryPop(out ValueTuple<int, int> result))
        //    {
        //        le = result.Item1;
        //        ri = result.Item2;
        //
        //        if (le >= ri)
        //            continue;
        //
        //        {
        //            ref IntermediateRenderEntity pivot = ref span[ri];
        //            int i = le - 1;
        //
        //            for (int j = le; le < ri; j++)
        //            {
        //                if (span[j].Shader.Id <= pivot.Shader.Id)
        //                {
        //                    i++;
        //                    IntermediateRenderEntity temp = span[ri];
        //                    span[ri] = span[j];
        //                    span[j] = temp;
        //                }
        //            }
        //
        //            q = i + 1;
        //
        //            IntermediateRenderEntity temp1 = span[q];
        //            span[q] = span[ri];
        //            span[ri] = temp1;
        //
        //        }
        //
        //        if (q - le < ri - q)
        //        {
        //            _sortStack.Push((le, q - 1));
        //            _sortStack.Push((q + 1, ri));
        //        }
        //        else
        //        {
        //            _sortStack.Push((q + 1, ri));
        //            _sortStack.Push((le, q - 1));
        //        } 
        //    }
        //}

        private record struct EntityRenderData(EntityData Data);
    }

    [StructLayout(LayoutKind.Explicit, Pack = 8)]
    internal readonly struct RenderKey : IComparable<RenderKey>
    {
        [FieldOffset(0)]
        public readonly uint ShaderId;
        [FieldOffset(4)]
        public readonly uint ModelId;
        [FieldOffset(8)]
        public readonly uint MaterialId;
        [FieldOffset(12)]
        public readonly uint MeshId;

        [FieldOffset(16)]
        private readonly int _listIndex;

        [FieldOffset(0)]
        private readonly ulong _code0;
        [FieldOffset(8)]
        private readonly ulong _code1;

        public RenderKey(uint shaderId, uint modelId, uint materialId, uint meshId, byte collectorIndex, int listIndex)
        {
            ShaderId = shaderId;
            ModelId = modelId;
            MaterialId = materialId;
            MeshId = meshId;

            _listIndex = collectorIndex | (listIndex << 2);
        }

        public int CompareTo(RenderKey other)
        {
            int v = _code0.CompareTo(other._code0);
            if (v == 0)
                return _code1.CompareTo(other._code1);
            return v;
        }

        public int ListIndex => (_listIndex & ~0x3) >> 2;
        public int CollectorIndex => _listIndex & 0x3;
    }
}
