using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets.Importers
{
    public interface IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, string outputPath);
        public void PreloadFile(AssetPipeline pipeline, AssetId id);
        public bool ValidateFile(AssetPipeline pipeline, AssetId id);
    }
}
