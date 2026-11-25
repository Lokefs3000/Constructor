using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn.Serialization;
using Tomlyn.Text;

namespace Primary.Serialization.Toml
{
    public sealed class ColorTomlConverter : TomlConverter<Color>
    {
        public override Color Read(TomlReader reader)
        {
            if (reader.TokenType == TomlTokenType.StartArray)
            {
                Color color = default;

                reader.Read();

                color.R = (float)reader.GetDouble(); reader.Read();
                color.G = (float)reader.GetDouble(); reader.Read();
                color.B = (float)reader.GetDouble(); reader.Read();
                color.A = (float)reader.GetDouble(); reader.Read();

                if (reader.TokenType != TomlTokenType.EndArray)
                    throw reader.CreateException($"Expected {TomlTokenType.EndArray} token but was {reader.TokenType}");

                reader.Read();
                return color;
            }
            else if (reader.TokenType == TomlTokenType.String)
            {
                string str = reader.GetString();
                if (str.Length < 7 || str[0] != '#')
                    throw reader.CreateException("Expected hex specifier for color");

                return Color.FromHex(str.AsSpan(1));
            }
            else if (reader.TokenType == TomlTokenType.Integer)
            {
                return new Color((uint)reader.GetInt64());
            }
            else
            {
                throw reader.CreateException($"Expected {TomlTokenType.StartArray} or {TomlTokenType.String} or {TomlTokenType.Integer} token but was {reader.TokenType}");
            }
        }

        public override void Write(TomlWriter writer, Color value)
        {
            writer.WriteStartArray();
            writer.WriteFloatValue(value.R);
            writer.WriteFloatValue(value.G);
            writer.WriteFloatValue(value.B);
            writer.WriteFloatValue(value.A);
            writer.WriteEndArray();
        }
    }
}
