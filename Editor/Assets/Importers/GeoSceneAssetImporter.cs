using Editor.Assets.Types;
using Editor.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

            pipeline.ReloadAsset(pipeline.Identifier.GetOrRegisterAsset(localInputFile));

            AssetCategoryDatabase category = Editor.GlobalSingleton.AssetDatabase.GetCategory<GeoSceneAsset>()!;
            category.AddEntry(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localInputFile), localInputFile, true));

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetCategoryDatabase category = Editor.GlobalSingleton.AssetDatabase.GetCategory<GeoSceneAsset>()!;
            category.AddEntry(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, true));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);
            if (stream == null)
                return false;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(stream);
                return true;
            }
            catch (Exception)
            {
            }
            
            return true;
        }

        public string? CustomFileIcon => "Editor/Textures/Icons/CbGeoSceneIcon.png";
    }
}
