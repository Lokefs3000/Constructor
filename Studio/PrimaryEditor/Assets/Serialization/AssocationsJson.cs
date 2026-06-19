using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Primary;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets.Serialization
{
    public sealed class AssocationsJson
    {
        public int Version { get; set; } = FileVersion;
        public KeyValuePair<AssetId, AssetId[]>[] Assocations { get; set; } = [];

        public const int FileVersion = 1;
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        Converters = [typeof(AssocationDataJsonConverter)],
        WriteIndented = Engine.IsDebugBuild,
        IndentSize = 4)]
    [JsonSerializable(typeof(AssocationsJson))]
    internal partial class AssocationsJsonContext : JsonSerializerContext
    {

    }
}
