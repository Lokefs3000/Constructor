using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Primary.Serialization.Arguments
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, RespectNullableAnnotations = true)]
    [JsonSerializable(typeof(ArgumentFile))]
    internal partial class ArgumentJsonContext : JsonSerializerContext
    {
    }
}
