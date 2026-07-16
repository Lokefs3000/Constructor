using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PrimaryEditor.Inspector.Views.IO
{
    internal sealed class CustomViewGroupJson : CustomViewJson
    {
        [JsonRequired]
        public CustomViewJson[] Values { get; set; } = [];
    }
}
