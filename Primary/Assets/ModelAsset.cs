using CommunityToolkit.HighPerformance;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Assets
{
    public sealed class ModelAsset : IAssetDefinition
    {
        private readonly ModelAssetData _assetData;

        internal ModelAsset(ModelAssetData assetData)
        {
            _assetData = assetData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRenderMesh(ReadOnlySpan<char> name, out RenderMesh renderMesh)
        {
            return _assetData.TryGetRenderMesh(name.ToString(), out renderMesh);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderMesh GetRenderMesh(ReadOnlySpan<char> name)
        {
            if (_assetData.TryGetRenderMesh(name.ToString(), out RenderMesh renderMesh))
                return renderMesh;
            throw new KeyNotFoundException($"No render mesh with name: {name}");
        }

        internal ModelAssetData AssetData => _assetData;

        public RHI.Buffer? VertexBuffer => _assetData.VertexBuffer;
        public RHI.Buffer? IndexBuffer => _assetData.IndexBuffer;

        public ResourceStatus Status => _assetData.Status;
    }

    internal sealed class ModelAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private RenderMesh[] _meshes;
        private ModelNode? _node;

        private RHI.Buffer? _vertexBuffer;
        private RHI.Buffer? _indexBuffer;

        internal ModelAssetData()
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _meshes = Array.Empty<RenderMesh>();
            _node = null;

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            _asset.Target = null;

            foreach (RenderMesh rm in _meshes)
                rm.FreeHandle();

            _meshes = Array.Empty<RenderMesh>();
            _node = null;

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        public void ResetInternalState()
        {
            Dispose();

            _status = ResourceStatus.Pending;
        }

        public void PromoteStateToRunning()
        {
            if (_status == ResourceStatus.Pending)
                _status = ResourceStatus.Running;
        }

        internal void UpdateAssetData(ModelAsset asset, RenderMesh[] meshes, ModelNode node, RHI.Buffer vertexBuffer, RHI.Buffer indexBuffer)
        {
            _asset.Target = asset;
            _meshes = meshes;
            _node = node;
            _vertexBuffer = vertexBuffer;
            _indexBuffer = indexBuffer;

            _status = ResourceStatus.Success;
        }

        internal void UpdateAssetFailed(ModelAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal bool TryGetRenderMesh(string id, out RenderMesh renderMesh)
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

            Unsafe.SkipInit(out renderMesh);
            return false;
        }

        internal RHI.Buffer? VertexBuffer => _vertexBuffer;
        internal RHI.Buffer? IndexBuffer => _indexBuffer;

        internal ResourceStatus Status => _status;

        public Type AssetType => typeof(ModelAsset);
    }

    public class RenderMesh
    {
        private readonly ModelAsset _model;
        private readonly string _id;
        private readonly GCHandle _gc;
        private readonly nint _handle;

        private readonly uint _vertexOffset;
        private readonly uint _indexOffset;

        private readonly uint _indexCount;

        internal RenderMesh(ModelAsset modelAsset, string id, uint vertexOffset, uint indexOffset, uint indexCount)
        {
            _model = modelAsset;
            _id = id;
            _gc = GCHandle.Alloc(this, GCHandleType.Normal);
            _handle = GCHandle.ToIntPtr(_gc);
            _vertexOffset = vertexOffset;
            _indexOffset = indexOffset;
            _indexCount = indexCount;
        }

        internal void FreeHandle()
        {
            _gc.Free();
        }

        public ModelAsset Model => _model;
        public string Id => _id;

        internal nint Handle => _handle;

        public uint VertexOffset => _vertexOffset;
        public uint IndexOffset => _indexOffset;

        public uint IndexCount => _indexCount;
    }

    public sealed class ModelNode
    {
        private readonly ModelAsset _model;
        private readonly ModelNode? _parent;
        private readonly ModelNode[] _children;

        private readonly string _name;
        private readonly string[] _meshes;

        internal ModelNode(ModelAsset model, ModelNode? parent, ModelNode[] children, string name, string[] meshes)
        {
            _model = model;
            _parent = parent;
            _children = children;
            _name = name;
            _meshes = meshes;
        }

        public ModelAsset Model => _model;
        public ModelNode? Parent => _parent;
        public ModelNode[] Children => _children;

        public string Name => _name;
        public string[] Meshes => _meshes;
    }
}
