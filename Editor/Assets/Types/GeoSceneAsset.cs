using Editor.Geometry;
using Primary.Assets;
using Silk.NET.Assimp;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Assets.Types
{
    internal class GeoSceneAsset : IAssetDefinition
    {
        private readonly GeoSceneAssetData _assetData;

        internal GeoSceneAsset(GeoSceneAssetData assetData)
        {
            _assetData = assetData;
        }

        internal GeoBrushScene? BrushScene => _assetData.BrushScene;
        internal GeoVertexCache? VertexCache => _assetData.VertexCache;
        internal GeoGenerator? Generator => _assetData.Generator;

        internal GeoSceneAssetData AssetData => _assetData;

        public ResourceStatus Status => _assetData.Status;

        public string Name => _assetData.Name;
        public AssetId Id => _assetData.Id;
    }

    internal class GeoSceneAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private readonly AssetId _id;
        private string _name;

        private GeoBrushScene? _brushScene;
        private GeoVertexCache? _vertexCache;
        private GeoGenerator? _generator;

        internal GeoSceneAssetData(AssetId id)
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _id = id;
            _name = string.Empty;

            _brushScene = null;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;
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
        }

        internal void UpdateAssetFailed(GeoSceneAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal GeoBrushScene? BrushScene => _brushScene;
        internal GeoVertexCache? VertexCache => _vertexCache;
        internal GeoGenerator? Generator => _generator;

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        public Type AssetType => typeof(GeoSceneAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);
    }
}
