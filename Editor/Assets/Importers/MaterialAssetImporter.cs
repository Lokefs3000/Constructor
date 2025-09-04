using Editor.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Import(AssetPipeline pipeline, string fullFilePath, string outputFilePath)
        {
            pipeline.ReloadAsset(fullFilePath);
        }

        public string CustomFileIcon => "Content/Icons/FileMaterial.png";
    }
}
