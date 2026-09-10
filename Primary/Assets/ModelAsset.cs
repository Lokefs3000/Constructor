using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering.Assets;
using Primary.RHI;
using System.Collections.Immutable;
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

        public bool TryGetMesh(ReadOnlySpan<char> name, [NotNullWhen(true)] out MeshAsset? mesh) => TryGetMesh(name, StringComparison.CurrentCulture, out mesh);

        public bool TryGetMesh(ReadOnlySpan<char> name, StringComparison comparison, [NotNullWhen(true)] out MeshAsset? mesh)
        {
            if (IsLoaded)
            {
                foreach (LazyMesh lazyMesh in AssetData.Meshes)
                {
                    if (name.SequenceEqual(lazyMesh.Name))
                    {
                        mesh = lazyMesh.Asset.Value;
                        return true;
                    }
                }
            }

            mesh = null;
            return false;
        }

        public MeshAsset? TryFindMeshOrNull(ReadOnlySpan<char> name) => TryFindMeshOrNull(name, StringComparison.CurrentCulture);

        public MeshAsset? TryFindMeshOrNull(ReadOnlySpan<char> name, StringComparison comparison)
        {
            if (IsLoaded)
            {
                foreach (LazyMesh lazyMesh in AssetData.Meshes)
                {
                    if (name.SequenceEqual(lazyMesh.Name))
                    {
                        return lazyMesh.Asset.Value;
                    }
                }
            }

            return null;
        }

        public ImmutableArray<LazyMesh> Meshes => IsLoaded ? AssetData.Meshes : ImmutableArray<LazyMesh>.Empty;
        public ModelNode? RootNode => IsLoaded ? AssetData.Root : null;
    }

    public sealed class ModelAssetData : BaseInternalAssetData<ModelAsset>, IRenderMeshSource
    {
        private ImmutableArray<LazyMesh> _meshes;
        private ModelNode? _node;

        private RHIBuffer? _vertexBuffer;
        private RHIBuffer? _indexBuffer;

        internal ModelAssetData(AssetId id) : base(id)
        {
            _meshes = ImmutableArray<LazyMesh>.Empty;
            _node = null;

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        public override void Dispose()
        {
            base.Dispose();

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            _meshes = ImmutableArray<LazyMesh>.Empty;
            _node = null;

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        internal void UpdateAssetData(ModelAsset asset, ImmutableArray<LazyMesh> meshes, ModelNode node, RHIBuffer vertexBuffer, RHIBuffer indexBuffer)
        {
            base.UpdateAssetData(asset);

            _meshes = meshes;
            _node = node;
            _vertexBuffer = vertexBuffer;
            _indexBuffer = indexBuffer;
        }

        public ImmutableArray<LazyMesh> Meshes => _meshes;
        public ModelNode? Root => _node;

        RHIBuffer? IRenderMeshSource.VertexBuffer => _vertexBuffer;
        RHIBuffer? IRenderMeshSource.IndexBuffer => _indexBuffer;

        bool IRenderMeshSource.IsLoaded => Status == ResourceStatus.Success;
    }

    public readonly record struct LazyMesh(Lazy<MeshAsset> Asset, int LocalId, string Name);

    public sealed record class ModelNode
    {
        private readonly ModelAsset _model;
        private readonly ImmutableArray<ModelNode> _children;

        private readonly ModelTransform _transform;

        private readonly string _name;
        private readonly Lazy<MeshAsset>? _mesh;

        internal ModelNode(ModelAsset model, ImmutableArray<ModelNode> children, ModelTransform transform, string name, Lazy<MeshAsset>? mesh)
        {
            _model = model;
            _children = children;
            _transform = transform;
            _name = name;
            _mesh = mesh;
        }

        public ModelAsset Model => _model;
        public ImmutableArray<ModelNode> Children => _children;

        public ModelTransform Transform => _transform;

        public string Name => _name;
        public MeshAsset? Mesh => _mesh?.Value;
    }

    public readonly record struct ModelTransform(Vector3 Position, Quaternion Quaternion, Vector3 Scale);
}
