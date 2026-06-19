using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Primary;

namespace PrimaryEditor.Assets.Serialization
{
    internal sealed class RemappingDataJson
    {
        public int Version { get; set; } = FileVersion;
        public RemappingDataEntry[] Remappings { get; set; } = [];

        public const int FileVersion = 1;
    }

    internal record struct RemappingDataEntry(string Source, string Remap);

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = Engine.IsDebugBuild,
        IndentSize = 4)]
    [JsonSerializable(typeof(RemappingDataJson))]
    internal partial class RemappingDataJsonContext : JsonSerializerContext
    {

    }
}
