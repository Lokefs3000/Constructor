using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Primary;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets.Serialization
{
    public sealed class PhysicalFileJson
    {
        public int Version { get; set; } = FileVersion;
        public KeyValuePair<AssetId, PhysicalFileInfo>[] Files { get; set; } = [];

        public const int FileVersion = 1;
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        Converters = [typeof(PhysicalFileInfoConverter)],
        WriteIndented = Engine.IsDebugBuild,
        IndentSize = 4)]
    [JsonSerializable(typeof(PhysicalFileJson))]
    internal partial class PhysicalFileJsonContext : JsonSerializerContext
    {

    }
}
