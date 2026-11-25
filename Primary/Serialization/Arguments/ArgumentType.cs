using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Primary.Serialization.Arguments
{
    [JsonConverter(typeof(ArgumentTypeConverter))]
    internal record struct ArgumentType(ArgumentTypeLiteral Literal, bool IsArray);

    internal enum ArgumentTypeLiteral : byte
    {
        Null = 0,
        Integer,
        Number,
        String,
        Boolean
    }
}
