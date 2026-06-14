using Editor.Assets.Types;
using Editor.Geometry;
using Editor.Geometry.Serialization;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common.Streams;

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
                using Stream? inputStream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom) ??
                    throw new Exception();

                byte[] sourceData = new byte[inputStream.Length];
                inputStream.ReadExactly(sourceData);

                GeoScene scene = new GeoScene();
                SceneDeserializer.Deserialize(sourceData, scene);

                MaterialAsset material = AssetManager.LoadAsset<MaterialAsset>("Editor/Materials/GeoDefault.mat");
                geoSceneData.UpdateAssetData(geoScene, scene, material);
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
