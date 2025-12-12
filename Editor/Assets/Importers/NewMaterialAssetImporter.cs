using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Rendering2.Assets;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace Editor.Assets.Importers
{
    internal class NewMaterialAssetImporter : IAssetImporter
    {
        public NewMaterialAssetImporter()
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

            Editor.GlobalSingleton.AssetDatabase.AddEntry<MaterialAsset2>(new AssetDatabaseEntry(id, localInputFile, true));
            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            if (!ValidateFile(localFilePath, filesystem, pipeline))
            {
                Editor.GlobalSingleton.AssetDatabase.AddEntry<MaterialAsset2>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, false));
                return;
            }

            Editor.GlobalSingleton.AssetDatabase.AddEntry<MaterialAsset2>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, true));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            string? str = filesystem.ReadString(localFilePath);
            return str == null ? false : Toml.TryToModel(str, out TomlTable? _, out DiagnosticsBag? _);
        }

        public string CustomFileIcon => "Editor/Textures/Icons/FileMaterial.png";
    }
}
