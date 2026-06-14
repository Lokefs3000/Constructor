using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn.Serialization;
using Tomlyn.Text;

namespace Primary.Serialization.Toml
{
    public sealed class Int2TomlConverter : TomlConverter<Int2>
    {
        public override Int2 Read(TomlReader reader)
        {
            if (reader.TokenType == TomlTokenType.StartArray)
            {
                Int2 value = default;

                for (int i = 0; i < 2; i++)
                {
                    reader.Read();
                    if (reader.TokenType != TomlTokenType.Integer)
                        throw reader.CreateException($"Expected {TomlTokenType.Integer} token but was {reader.TokenType}");

                    value[i] = (int)reader.GetInt64();
                }

                reader.Read();
                if (reader.TokenType != TomlTokenType.EndArray)
                    throw reader.CreateException($"Expected {TomlTokenType.EndArray} token but was {reader.TokenType}");

                reader.Read();
                return value;
            }
            else
            {
                throw reader.CreateException($"Expected {TomlTokenType.StartArray} token but was {reader.TokenType}");
            }
        }

        public override void Write(TomlWriter writer, Int2 value)
        {
            writer.WriteStartArray();
            writer.WriteIntegerValue(value.X);
            writer.WriteIntegerValue(value.Y);
            writer.WriteEndArray();
        }
    }
}
