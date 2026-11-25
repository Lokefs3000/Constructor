using Editor.Assets;
using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace Editor.UI.Assets.Importers
{
    internal sealed class StylesheetAssetImporter : IAssetImporter
    {
        public StylesheetAssetImporter()
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

            EditorRuntime.GlobalSingleton.AssetDatabase.AddEntry<StylesheetAsset>(new AssetDatabaseEntry(id, localInputFile, true));
            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            if (!ValidateFile(localFilePath, filesystem, pipeline))
            {
                EditorRuntime.GlobalSingleton.AssetDatabase.AddEntry<StylesheetAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, false));
            }
            else
                EditorRuntime.GlobalSingleton.AssetDatabase.AddEntry<StylesheetAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, true));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            return true;
        }

        public string? CustomFileIcon => null;
    }
}
