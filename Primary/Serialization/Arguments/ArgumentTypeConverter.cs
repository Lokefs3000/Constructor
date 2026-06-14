using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Primary.Serialization.Arguments
{
    internal sealed class ArgumentTypeConverter : JsonConverter<ArgumentType>
    {
        public override ArgumentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new ArgumentType(ArgumentTypeLiteral.Null, false);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                ArgumentTypeLiteral typeLiteral = ArgumentTypeLiteral.Null;

                string format = reader.GetString()!;
                int index = format.IndexOf('[');

                if (index == -1)
                {
                    if (!Enum.TryParse(format, out typeLiteral))
                        throw new JsonException("Malformed format string");
                }
                else
                {
                    if (!Enum.TryParse(format.AsSpan(0, index), out typeLiteral))
                        throw new JsonException("Malformed format string");

                    if (index != format.Length - 2 || format[^1] != ']')
                        throw new JsonException("Malformed format string");
                }

                return new ArgumentType(typeLiteral, index != -1);
            }
            else
            {
                throw new JsonException("Expected format string");
            }
        }

        public override void Write(Utf8JsonWriter writer, ArgumentType value, JsonSerializerOptions options)
        {
            switch (value.Literal)
            {
                case ArgumentTypeLiteral.Null: writer.WriteNullValue(); break;
                case ArgumentTypeLiteral.Integer: writer.WriteStringValue(value.IsArray ? "Integer[]" : "Integer"); break;
                case ArgumentTypeLiteral.Number: writer.WriteStringValue(value.IsArray ? "Number[]" : "Number"); break;
                case ArgumentTypeLiteral.String: writer.WriteStringValue(value.IsArray ? "String[]" : "String"); break;
            }
        }
    }
}
