using Editor.Geometry;
using Primary.Assets.Types;
using Primary.RHI2;
using System.Runtime.CompilerServices;

namespace Editor.Assets.Types
{
    internal class GeoSceneAsset : IAssetDefinition
    {
        private readonly GeoSceneAssetData _assetData;

        internal GeoSceneAsset(GeoSceneAssetData assetData)
        {
            _assetData = assetData;
        }

        internal void Regenerate() => _assetData.Regenerate();

        internal GeoBrushScene? BrushScene => _assetData.BrushScene;
        internal GeoVertexCache? VertexCache => _assetData.VertexCache;
        internal GeoGenerator? Generator => _assetData.Generator;

        internal bool NeedsRegeneration { get => _assetData.NeedsRegeneration; set => _assetData.NeedsRegeneration = value; }

        internal GeoSceneAssetData AssetData => _assetData;

        public ResourceStatus Status => _assetData.Status;

        public string Name => _assetData.Name;
        public AssetId Id => _assetData.Id;
    }

    internal class GeoSceneAssetData : IInternalAssetData//, IRenderMeshSource
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private readonly AssetId _id;
        private string _name;

        private GeoBrushScene? _brushScene;
        private GeoVertexCache? _vertexCache;
        private GeoGenerator? _generator;

        private bool _needsRegenerate;

        private RHIBuffer? _vertexBuffer;
        private RHIBuffer? _indexBuffer;

        private int _vertexBufferSize;
        private int _indexBufferSize;

        internal GeoSceneAssetData(AssetId id)
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _id = id;
            _name = string.Empty;

            _brushScene = null;
            _vertexCache = null;
            _generator = null;

            _needsRegenerate = false;

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;

            _generator?.Dispose();

            _brushScene = null;
            _vertexCache = null;
            _generator = null;

            _needsRegenerate = false;

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            _vertexBuffer = null;
            _indexBuffer = null;
        }

        public void SetAssetInternalStatus(ResourceStatus status)
        {
            _status = status;
        }

        public void SetAssetInternalName(string name)
        {
            _name = name;
        }

        internal void UpdateAssetData(GeoSceneAsset asset, GeoBrushScene brushScene, GeoVertexCache vertexCache, GeoGenerator generator)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;

            _brushScene = brushScene;
            _vertexCache = vertexCache;
            _generator = generator;

            _needsRegenerate = true;
        }

        internal void UpdateAssetFailed(GeoSceneAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal void Regenerate()
        {
            if (_needsRegenerate)
            {
                if (_brushScene != null && _vertexCache != null && _generator != null)
                {
                    _generator.GenerateMesh(_brushScene);

                    if (_vertexBuffer == null || _vertexBufferSize < _generator.Vertices.Length)
                    {
                        _vertexBufferSize = (int)(_generator.Vertices.Length * 1.5);
                        unsafe
                        {
                            _vertexBuffer = RHIDevice.Instance!.CreateBuffer(new RHIBufferDescription
                            {
                                Width = (uint)(Unsafe.SizeOf<GeoVertex>() * _vertexBufferSize),
                                Stride = Unsafe.SizeOf<GeoVertex>(),
                                Usage = RHIResourceUsage.VertexInput,
                            }, (nint)Unsafe.AsPointer(ref _generator.Vertices[0]));
                        }
                    }
                    //else
                    //    FrameUploadManager.ScheduleUpload(_vertexBuffer, _generator.Vertices, new UploadDescription(UploadScheduleTarget.Frame));

                    if (_indexBuffer == null || _indexBufferSize < _generator.Indices.Length)
                    {
                        _indexBufferSize = (int)(_generator.Indices.Length * 1.5);
                        unsafe
                        {
                            _indexBuffer = RHIDevice.Instance!.CreateBuffer(new RHIBufferDescription
                            {
                                Width = (uint)(Unsafe.SizeOf<ushort>() * _indexBufferSize),
                                Stride = Unsafe.SizeOf<ushort>(),
                                Usage = RHIResourceUsage.VertexInput,
                            }, (nint)Unsafe.AsPointer(ref _generator.Indices[0]));
                        }
                    }
                    //else
                    //    FrameUploadManager.ScheduleUpload(_indexBuffer, _generator.Indices, new UploadDescription(UploadScheduleTarget.Frame));
                }

                _needsRegenerate = false;
            }
        }

        internal GeoBrushScene? BrushScene => _brushScene;
        internal GeoVertexCache? VertexCache => _vertexCache;
        internal GeoGenerator? Generator => _generator;

        internal bool NeedsRegeneration { get => _needsRegenerate; set => _needsRegenerate = value; }

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        public int LoadIndex => 0;

        public Type AssetType => typeof(GeoSceneAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);

        AssetId IInternalAssetData.Id => Id;

        ResourceStatus IInternalAssetData.Status => Status;

        string IInternalAssetData.Name => Name;
    }
}
