using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PrimaryEditor.Inspector.Views.IO
{
    internal sealed class CustomViewFieldJson : CustomViewJson
    {
        [JsonRequired]
        public string Name { get; set; } = string.Empty;
        public string? Display { get; set; } = null;
    }
}
