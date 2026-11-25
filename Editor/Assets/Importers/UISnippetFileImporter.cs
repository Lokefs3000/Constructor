using Editor.UI;
using Primary.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Assets.Importers
{
    internal sealed class UISnippetFileImporter : IAssetImporter
    {
        public UISnippetFileImporter()
        {

        }

        public void Dispose()
        {

        }

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            // editor has not yet loaded fully and is instead waiting on asset preload
            if (EditorRuntime.GlobalSingleton.UIManager == null)
                return true;

            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);
            ThreadHelper.ExecuteOnMainThread(() =>
            {
                UIManager ui = UIManager.Instance;
                ui.ReloadWindowSnippets(localInputFile);
            });

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            return true;
        }

        public string? CustomFileIcon => null;
    }
}
