using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets.Importers
{
    public interface IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, Stream outputStream, string localPath, string localOutputPath);
        public void PreloadFile(AssetPipeline pipeline, AssetId id);
        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath);
    }
}
