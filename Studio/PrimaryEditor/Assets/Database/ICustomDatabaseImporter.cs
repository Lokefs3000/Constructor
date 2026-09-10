using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets.Database
{
    public interface ICustomDatabaseImporter
    {
        public void RegisterInDatabase(AssetPipeline pipeline, AssetId id, string localPath);
        public void UnregisterInDatabase(AssetPipeline pipeline, AssetId id, string localPath);
    }
}
