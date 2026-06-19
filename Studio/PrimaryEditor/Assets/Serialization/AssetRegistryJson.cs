using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Primary;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets.Serialization
{
    public sealed class AssetRegistryJson
    {
        public int Version { get; set; } = FileVersion;
        public AssetIdDataJson[] Assets { get; set; } = [];

        public const int FileVersion = 1;
    }

    public record struct AssetIdDataJson
    {
        public AssetId Id { get; set; }
        public string Path { get; set; }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = Engine.IsDebugBuild,
        IndentSize = 4)]
    [JsonSerializable(typeof(AssetRegistryJson))]
    internal partial class AssetRegistryJsonContext : JsonSerializerContext
    {

    }
}
