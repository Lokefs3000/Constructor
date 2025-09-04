using Editor.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Assets.Importers
{
    internal class ModelAssetImporter : IAssetImporter
    {
        private ModelProcessor _processor;

        public ModelAssetImporter()
        {
            _processor = new ModelProcessor();
        }

        public void Dispose()
        {
            
        }

        public void Import(AssetPipeline pipeline, string fullFilePath, string outputFilePath)
        {
            bool r = _processor.Execute(new ModelProcessorArgs
            {
                AbsoluteFilepath = fullFilePath,
                AbsoluteOutputPath = outputFilePath
            });

            if (!r)
            {
                EdLog.Assets.Error("Failed to import model: {local}", fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                return;
            }

            string localInputFile = fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length);
            string localOutputFile = outputFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length);

            Editor.GlobalSingleton.ProjectSubFilesystem.RemapFile(localInputFile, localOutputFile);
            pipeline.ReloadAsset(fullFilePath);
        }

        public string CustomFileIcon => "Content/Icons/FileModel.png";
    }
}
