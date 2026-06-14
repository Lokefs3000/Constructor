using Editor.Assets.Types;
using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Types;
using System.Text.Json;
using TerraFX.Interop.Windows;

namespace Editor.Assets.Importers
{
    internal class GeoSceneAssetImporter : IAssetImporter
    {
        public GeoSceneAssetImporter()
        {

        }

        public void Dispose()
        {

        }

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);

            byte[] sourceData;
            {
                using Stream? stream = filesystem.OpenStream(localInputFile)
                    ?? throw new AssetImportException("Failed to open source stream for reading", id);

                sourceData = new byte[stream.Length];
                stream.ReadExactly(sourceData);
            }

            Utf8JsonReader reader = new Utf8JsonReader(sourceData);
            while (!reader.IsFinalBlock)
                reader.Read();

            pipeline.ReloadAsset(id);

            AssetCategoryDatabase category = EditorRuntime.GlobalSingleton.AssetDatabase.GetCategory<GeoSceneAsset>()!;
            category.AddEntry(new AssetDatabaseEntry(id, localInputFile, true));

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetDatabase database = EditorRuntime.GlobalSingleton.AssetDatabase;
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);
            
            database.AddEntry<GeoSceneAsset>(new AssetDatabaseEntry(id, localFilePath, ValidateFile(localFilePath, filesystem, pipeline)));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);
            if (stream == null)
                return false;

            byte[] sourceData;

            sourceData = new byte[stream.Length];
            stream.ReadExactly(sourceData);

            Utf8JsonReader reader = new Utf8JsonReader(sourceData);
            while (!reader.IsFinalBlock)
                reader.Read();

            return true;
        }

        public string? CustomFileIcon => "Editor/Textures/Icons/CbGeoSceneIcon.png";
    }
}
