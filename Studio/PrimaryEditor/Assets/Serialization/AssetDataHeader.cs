using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Primary.Assets.Types;
using PrimaryEditor.Assets.Utility;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Serialization
{
    public record struct AssetDataHeader
    {
        public AssetDataHeader(AssetId id, bool skipLaunchImport)
        {
            Id = id;
            Version = TargetVersion;
        }

        [TomlPropertyName("asset_id"), TomlRequired] public AssetId Id { get; set; }
        [TomlPropertyName("meta_version"), TomlRequired] public int Version { get; set; }

        public const int TargetVersion = 1;
    }

    [TomlSourceGenerationOptions(Converters = [typeof(OnlyGuidAssetIdTomlConverter)], PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    [TomlSerializable(typeof(AssetDataHeader))]
    public partial class AssetDataHeaderTomlContext : TomlSerializerContext
    {

    }
}
