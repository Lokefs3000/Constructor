using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Importers
{
    public interface IAssetImporter
    {
        public void ImportFile(in ImportContext context);
        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath);

        public string UniqueId { get; }
        public Type AssetDefinitionType { get; }

        public string? DefaultConfigName { get; }
        public Type? ConfigType { get; }

        public TomlConverter[] Converters { get; }
    }
}
