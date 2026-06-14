using Editor.Geometry;
using Editor.Geometry.Mesh;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.Rendering.Assets;
using Primary.RHI;
using System.Runtime.CompilerServices;

namespace Editor.Assets.Types
{
    public sealed class GeoSceneAsset : BaseAssetDefinition<GeoSceneAsset, GeoSceneAssetData>
    {
        internal GeoSceneAsset(GeoSceneAssetData assetData) : base(assetData)
        {
        }

        public bool RegenerateMeshes()
        {
            if (Status != ResourceStatus.Success)
                return false;
            return AssetData.RegenerateMeshes();
        }

        public void ConsumeNewMeshData()
        {
            if (Status != ResourceStatus.Success)
                return;
            AssetData.ConsumeNewMeshData();
        }

        public void ResizeVertexBuffer(out bool isBufferNew)
        {
            if (Status != ResourceStatus.Success)
            {
                isBufferNew = false;
                return;
            }

            AssetData.ResizeVertexBuffer(out isBufferNew);
        }

        internal GeoScene? Scene => Status == ResourceStatus.Success ? AssetData.Scene : null;
        internal RHIBuffer? VertexBuffer => Status == ResourceStatus.Success ? AssetData.VertexBuffer : null;

        internal IReadOnlyDictionary<GeoMeshKey, GeoSceneRenderMesh>? RenderMeshes => Status == ResourceStatus.Success ? AssetData.RenderMeshes : null;
    }

    public sealed class GeoSceneAssetData : BaseInternalAssetData<GeoSceneAsset>, IRenderMeshSource
    {
        private GeoScene? _scene;
        private MaterialAsset? _defaultMaterial;

        private Dictionary<GeoMeshKey, GeoSceneRenderMesh> _renderMeshes;
        private HashSet<GeoMeshKey> _activeKeys;

        private RHIBuffer? _vertexBuffer;
        private int _vertexBufferSize;

        private bool _hasNewestUpdateBeenConsumed;

        internal GeoSceneAssetData(AssetId id) : base(id)
        {
            _scene = null;
            _defaultMaterial = null;

            _renderMeshes = new Dictionary<GeoMeshKey, GeoSceneRenderMesh>();
            _activeKeys = new HashSet<GeoMeshKey>();

            _vertexBuffer = null;
            _vertexBufferSize = 0;

            _hasNewestUpdateBeenConsumed = true;
        }

        public override void Dispose()
        {
            if (_scene != null)
                EditorRuntime.GlobalSingleton.GeoSceneManager.RemoveInvalidScene(Definition!, _scene);

            _scene = null;
            _defaultMaterial = null;

            _renderMeshes.Clear();
            _activeKeys.Clear();

            _vertexBuffer?.Dispose();
            _vertexBuffer = null;

            _vertexBufferSize = 0;

            _hasNewestUpdateBeenConsumed = true;

            base.Dispose();
        }

        public void UpdateAssetData(GeoSceneAsset asset, GeoScene scene, MaterialAsset material)
        {
            base.UpdateAssetData(asset);

            _scene = scene;
            _defaultMaterial = material;

            _hasNewestUpdateBeenConsumed = true;
        }

        public bool RegenerateMeshes()
        {
            if (_scene == null)
                return false;

            if (!_hasNewestUpdateBeenConsumed)
            {
                EditorRuntime.GlobalSingleton.GeoSceneManager.AddInvalidScene(Definition!, _scene);
                return false;
            }

            _hasNewestUpdateBeenConsumed = false;

            MeshContainer container = _scene.Container;
            foreach (MeshSlice slice in container.Slices)
                _activeKeys.Add(new GeoMeshKey(slice.Group, slice.Material));

            _scene.RegenerateInvalidData(_defaultMaterial);

            int i = 0;
            foreach (MeshSlice slice in container.Slices)
            {
                GeoMeshKey key = new GeoMeshKey(slice.Group, slice.Material);
                _activeKeys.Remove(key);
                
                if (slice.UpdateFlags == MeshSliceUpdateFlags.None)
                    break;

                if (_renderMeshes.TryGetValue(key, out GeoSceneRenderMesh? renderMesh))
                {
                    if (Flags.HasFlag(slice.UpdateFlags, MeshSliceUpdateFlags.VerticesChanged))
                        renderMesh.Boundaries = MeshContainer.GetAABB(_scene.Container, slice);

                    renderMesh.UniqueId = i++;
                    renderMesh.VertexOffset = (uint)slice.VtxOffset;
                    renderMesh.IndexCount = (uint)slice.VtxCount;
                }
                else
                {
                    _renderMeshes.Add(key, new GeoSceneRenderMesh(
                        this,
                        i++,
                        MeshContainer.GetAABB(_scene.Container, slice),
                        (uint)slice.VtxOffset,
                        0,
                        (uint)slice.VtxCount,
                        false));
                }
            }

            foreach (GeoMeshKey material in _activeKeys)
            {
                _renderMeshes.Remove(material);
            }

            _activeKeys.Clear();

            EditorRuntime.GlobalSingleton.GeoSceneManager.AddInvalidScene(Definition!, _scene);
            return true;
        }

        public void ConsumeNewMeshData() => _hasNewestUpdateBeenConsumed = true;

        public void ResizeVertexBuffer(out bool isBufferNew)
        {
            if (_scene == null)
            {
                isBufferNew = false;
                return;
            }

            if (_vertexBuffer == null || _vertexBufferSize < _scene.Container.VertexCount)
            {
                _vertexBufferSize = _scene.Container.VertexCount * 2;

                _vertexBuffer?.Dispose();
                _vertexBuffer = RHIDevice.Instance!.CreateBuffer(new RHIBufferDescription
                {
                    Width = (uint)(Unsafe.SizeOf<BrushVertex>() * _vertexBufferSize),
                    Stride = Unsafe.SizeOf<BrushVertex>(),
                    Usage = RHIResourceUsage.VertexInput,
                }, ArrayPtr<byte>.Null, "GeoSceneVtx");

                isBufferNew = true;
            }
            else
                isBufferNew = false;
        }

        internal GeoScene? Scene => _scene;

        public RHIBuffer? VertexBuffer => _vertexBuffer;
        public RHIBuffer? IndexBuffer => null;

        public bool IsLoaded => Status == ResourceStatus.Success;

        internal IReadOnlyDictionary<GeoMeshKey, GeoSceneRenderMesh>? RenderMeshes => _renderMeshes;
    }

    internal readonly record struct GeoMeshKey(BrushGroup Group, MaterialAsset Material);

    internal sealed class GeoSceneRenderMesh : RawRenderMesh
    {
        public GeoSceneRenderMesh(IRenderMeshSource source, int uniqueId, AABB boundaries, uint vertexOffset, uint indexOffset, uint indexCount, bool hasIndices) : base(source, uniqueId, boundaries, vertexOffset, indexOffset, indexCount, hasIndices)
        {
        }

        internal new int UniqueId { get => _uniqueId; set => _uniqueId = value; }

        internal new AABB Boundaries { get => _boundaries; set => _boundaries = value; }

        internal new uint VertexOffset { get => _vertexOffset; set => _vertexOffset = value; }
        internal new uint IndexOffset { get => _indexOffset; set => _indexOffset = value; }
        internal new uint IndexCount { get => _indexCount; set => _indexCount = value; }
    }
}
