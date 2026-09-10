using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets
{
    public sealed class AssetDataConfig
    {
        [TomlRequired, TomlPropertyName("asset_id")]
        public FileId Id { get; set; } = FileId.Invalid;
        [TomlRequired]
        public int MetaVersion { get; set; } = 1;

        [TomlInlineTable(TomlInlineTablePolicy.Always)]
        public SubAssetConfig[] SubAssets { get; set; } = [];

        public object? UniqueConfig { get; set; } = null;

        public const int CurrentVersion = 1;
    }

    public sealed class SubAssetConfig
    {
        [TomlRequired]
        public string AssetType { get; set; } = string.Empty;
        [TomlRequired]
        public int LocalId { get; set; } = AssetId.NoLocalId;
        [TomlRequired]
        public string? Name { get; set; } = string.Empty;
    }
}
