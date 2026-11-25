using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PrimaryEditor.Scripting.Serialization
{
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        IndentSize = 4,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(AsmDefFile))]
    [JsonSerializable(typeof(AsmDefReference))]
    internal partial class SerializerContext : JsonSerializerContext
    {
    }
}
