using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Primary.Serialization.Json
{
    public class Vector4JsonConverter : JsonConverter<Vector4>
    {
        public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            Vector4 value = default;
            for (int i = 0; i < 4; ++i)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.Number)
                    throw new JsonException();

                value[i] = reader.GetSingle();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();

            return value;
        }

        public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.Z);
            writer.WriteNumberValue(value.W);

            writer.WriteEndArray();
        }

        public static readonly Vector4JsonConverter Default = new Vector4JsonConverter();
    }
}
