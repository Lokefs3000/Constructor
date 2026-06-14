using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering.Assets;
using Primary.RHI;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Assets
{
    public sealed class ModelAsset : BaseAssetDefinition<ModelAsset, ModelAssetData>
    {
        internal ModelAsset(ModelAssetData assetData) : base(assetData)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRenderMesh(ReadOnlySpan<char> name, [NotNullWhen(true)] out RenderMesh? renderMesh)
        {
            return AssetData.TryGetRenderMesh(name.ToString(), out renderMesh);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderMesh GetRenderMesh(ReadOnlySpan<char> name)
        {
            if (AssetData.TryGetRenderMesh(name.ToString(), out RenderMesh? renderMesh))
                return renderMesh;
            throw new KeyNotFoundException($"No render mesh with name: {name}");
        }
    }

    public sealed class ModelAssetData : BaseInternalAssetData<ModelAsset>, IRenderMeshSource
    {
        private RenderMesh[] _meshes;
        private ModelNode? _node;

        private RHIBuffer? _vertexBuffer;
        private RHIBuffer? _indexBuffer;

        internal ModelAssetData(AssetId id) : base(id)
        {
            _meshes = Array.Empty<RenderMesh>();
            _node = null;

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        public override void Dispose()
        {
            base.Dispose();

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            //foreach (RenderMesh rm in _meshes)
            //    rm.FreeHandle();

            _meshes = [];
            _node = null;

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        internal void UpdateAssetData(ModelAsset asset, RenderMesh[] meshes, ModelNode node, RHIBuffer vertexBuffer, RHIBuffer indexBuffer)
        {
            base.UpdateAssetData(asset);

            _meshes = meshes;
            _node = node;
            _vertexBuffer = vertexBuffer;
            _indexBuffer = indexBuffer;
        }

        internal bool TryGetRenderMesh(string id, [NotNullWhen(true)] out RenderMesh? renderMesh)
        {
            for (int i = 0; i < _meshes.Length; i++)
            {
                ref RenderMesh rm = ref _meshes[i];
                if (rm.Id == id)
                {
                    renderMesh = rm;
                    return true;
                }
            }

            renderMesh = null;
            return false;
        }

        public ReadOnlySpan<RenderMesh> Meshes => _meshes;

        public RHIBuffer? VertexBuffer => _vertexBuffer;
        public RHIBuffer? IndexBuffer => _indexBuffer;

        public bool IsLoaded => Status == ResourceStatus.Success;
    }

    public class RenderMesh : RawRenderMesh
    {
        protected string _id;

        internal RenderMesh(ModelAssetData modelAssetData, int uniqueId, string id, AABB boundaries, uint vertexOffset, uint indexOffset, uint indexCount, bool hasIndices) : base(modelAssetData, uniqueId, boundaries, vertexOffset, indexOffset, indexCount, hasIndices)
        {
            _id = id;
        }

        internal void UpdateMeshData(int uniqueId, string id, AABB boundaries, uint vertexOffset, uint indexOffset, uint indexCount, bool hasIndices)
        {
            _uniqueId = uniqueId;

            _boundaries = boundaries;

            _vertexOffset = vertexOffset;
            _indexOffset = indexCount;
            _indexCount = indexCount;

            _hasIndices = hasIndices;

            _id = id;
        }

        public ModelAsset Model => Unsafe.As<ModelAsset>(Unsafe.As<ModelAssetData>(Source).Definition ?? throw new NullReferenceException());
        public string Id => _id;
    }

    public sealed record class ModelNode
    {
        private readonly ModelAsset _model;
        private readonly ModelNode? _parent;
        private readonly ModelNode[] _children;

        private readonly ModelTransform _transform;

        private readonly string _name;
        private readonly string? _meshId;

        internal ModelNode(ModelAsset model, ModelNode? parent, ModelNode[] children, ModelTransform transform, string name, string? meshId)
        {
            _model = model;
            _parent = parent;
            _children = children;
            _transform = transform;
            _name = name;
            _meshId = meshId;
        }

        public ModelAsset Model => _model;
        public ModelNode? Parent => _parent;
        public IReadOnlyCollection<ModelNode> Children => _children;

        public ModelTransform Transform => _transform;

        public string Name => _name;
        public string? MeshId => _meshId;
    }

    public readonly record struct ModelTransform(Vector3 Position, Quaternion Quaternion, Vector3 Scale);
}
