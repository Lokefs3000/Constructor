using Editor.Storage;
using Primary.Assets;

namespace Editor.Assets.Importers
{
    internal class MaterialAssetImporter : IAssetImporter
    {
        public MaterialAssetImporter()
        {

        }

        public void Dispose()
        {

        }

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);

            pipeline.ReloadAsset(id);

            Editor.GlobalSingleton.AssetDatabase.AddEntry<MaterialAsset>(new AssetDatabaseEntry(id, localInputFile, true));
            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            if (!ValidateFile(localFilePath, filesystem, pipeline))
            {
                Editor.GlobalSingleton.AssetDatabase.AddEntry<MaterialAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, false));
                return;
            }

            Editor.GlobalSingleton.AssetDatabase.AddEntry<MaterialAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, true));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline) => true;

        public string CustomFileIcon => "Editor/Textures/Icons/FileMaterial.png";
    }
}
