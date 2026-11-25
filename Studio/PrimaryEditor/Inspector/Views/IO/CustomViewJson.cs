using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PrimaryEditor.Inspector.Views.IO
{
    [JsonPolymorphic]
    [JsonDerivedType(typeof(CustomViewFieldJson), "Field")]
    [JsonDerivedType(typeof(CustomViewGroupJson), "Group")]
    [JsonDerivedType(typeof(CustomViewPresetJson), "Preset")]
    internal abstract class CustomViewJson
    {
        public ConditionJson[]? Conditions { get; set; } = null;
        public ViewConditionMode Comparison { get; set; } = ViewConditionMode.All;

        public float Padding { get; set; } = 0.0f;
        public float Indent { get; set; } = 0.0f;
    }

    internal sealed class ConditionJson
    {
        [JsonRequired]
        public string Name { get; set; } = string.Empty;
        [JsonRequired]
        public object?[]? Values { get; set; } = null;
    }

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        UseStringEnumConverter = true,
        RespectNullableAnnotations = true)]
    [JsonSerializable(typeof(CustomViewJson[]))]
    internal partial class CustomViewJsonContext : JsonSerializerContext
    {
    }
}
