using Editor.Assets.Types;
using Editor.Geometry;
using Primary.Assets.Types;
using Primary.Common.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Assets.Loaders
{
    internal sealed class GeoSceneAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new GeoSceneAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not GeoSceneAssetData geoSceneData)
                throw new ArgumentException(nameof(assetData));

            return new GeoSceneAsset(geoSceneData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not GeoSceneAsset geoScene)
                throw new ArgumentException(nameof(asset));
            if (assetData is not GeoSceneAssetData geoSceneData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                GeoBrushScene brushScene = new GeoBrushScene();
                GeoVertexCache vertexCache = new GeoVertexCache();
                GeoGenerator generator = new GeoGenerator(vertexCache);

                geoSceneData.UpdateAssetData(geoScene, brushScene, vertexCache, generator);
            }
#if !DEBUG
            catch (Exception ex)
            {
                geoSceneData.UpdateAssetFailed(geoScene);
                EdLog.Assets.Error(ex, "Failed to load geo scene: {name}", sourcePath);
            }
#endif
            finally
            {
                
            }
        }
    }
}
