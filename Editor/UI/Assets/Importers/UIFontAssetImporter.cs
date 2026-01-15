using Editor.Assets;
using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace Editor.UI.Assets.Importers
{
    internal sealed class UIFontAssetImporter : IAssetImporter
    {
        public UIFontAssetImporter()
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
                Editor.GlobalSingleton.AssetDatabase.AddEntry<UIFontAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, false));
            }

            Editor.GlobalSingleton.AssetDatabase.AddEntry<UIFontAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, true));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            string? str = filesystem.ReadString(localFilePath);
            if (str != null && Toml.TryToModel(str, out TomlTable? table, out DiagnosticsBag? _))
            {
                if (table.TryGetValue("font_file", out object fontFile) && fontFile is string && AssetPipeline.TryGetFullPathFromLocal((string)fontFile, out string? fullPath))
                    return File.Exists(fullPath);
            }

            return false;
        }

        public string? CustomFileIcon => null;
    }
}
