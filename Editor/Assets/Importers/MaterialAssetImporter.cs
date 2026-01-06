using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Types;
using Tomlyn;
using Tomlyn.Model;

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

            TomlTable table = Toml.ToModel<TomlTable>(File.ReadAllText(fullFilePath));
            if (!table.TryGetValue("shader", out object value))
            {
                EdLog.Assets.Error("[{a}]: No key named shader in material file", localInputFile);
                return false;
            }

            if (value is not string or long)
            {
                EdLog.Assets.Error("[{a}]: Shader value is in an incorrect format: {k}", value);
                return false;
            }

            //TODO: verify if id is actually a shader
            AssetId shaderId = value is string ?
                pipeline.Identifier.GetOrRegisterAsset((string)value) :
                new AssetId((uint)(long)value);

            pipeline.Associator.MakeAssocation(id, shaderId, true);
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
