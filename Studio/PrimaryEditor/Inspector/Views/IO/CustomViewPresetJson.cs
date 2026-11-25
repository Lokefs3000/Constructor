using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrimaryEditor.Inspector.Views.IO
{
    internal sealed class CustomViewPresetJson : CustomViewJson
    {
        [JsonRequired]
        public string Display { get; set; } = string.Empty;
        [JsonRequired]
        public Dictionary<string, PresetValueJson[]> Presets { get; set; } = [];

        public CustomViewJson[]? Values { get; set; } = null;
    }

    internal sealed class PresetValueJson
    {
        [JsonRequired]
        public string Name { get; set; } = string.Empty;
        [JsonRequired]
        public object? Value { get; set; } = null;
    }
}
