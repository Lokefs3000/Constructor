using Editor.Processors;
using Primary.Common;
using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Assets.Importers
{
    internal class ShaderAssetImporter : IAssetImporter
    {
        private ShaderProcessor _processor;

        public ShaderAssetImporter()
        {
            _processor = new ShaderProcessor();
        }

        public void Dispose()
        {

        }

        public void Import(AssetPipeline pipeline, string fullFilePath, string outputFilePath)
        {
            string tomlFile = Path.ChangeExtension(fullFilePath, ".toml");
            if (!File.Exists(tomlFile))
            {
                return;
            }

            ShaderProcessor processor = new ShaderProcessor();

            bool r = processor.Execute(new ShaderProcessorArgs
            {
                AbsoluteFilepath = fullFilePath,
                AbsoluteOutputPath = outputFilePath,

                ContentSearchDir = EditorFilepaths.ContentPath,

                Target = Primary.RHI.GraphicsAPI.Direct3D12,

                Logger = EdLog.Assets
            });

            if (!r)
            {
                EdLog.Assets.Error("Failed to import shader: {local}", fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                return;
            }

            string localOutputFile = outputFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length);

            Editor.GlobalSingleton.ProjectShaderLibrary.AddFileToMapping(NullableUtility.AlwaysThrowIfNull(processor.ShaderPath), localOutputFile);
            //Editor.GlobalSingleton.ProjectSubFilesystem.RemapFile(localInputFile, localOutputFile);

            pipeline.MakeFileAssociations(fullFilePath, [.. processor.ReadFiles, tomlFile]);
            if (processor.ShaderPath != null)
                pipeline.ReloadAsset(processor.ShaderPath);
        }

        public string CustomFileIcon => "Content/Icons/FileShader.png";
    }
}
