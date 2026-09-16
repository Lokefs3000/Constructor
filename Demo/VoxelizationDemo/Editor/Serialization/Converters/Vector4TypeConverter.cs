using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal sealed class Vector4TypeConverter : TypeConverter<Vector4>
    {
        public override void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start array");

            Vector4 v = default;
            for (int i = 0; i < 4; ++i)
            {
                v[i] = reader.GetSingle();
                reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException("Expected end array");

            valueSource.Set(boxed, v);
        }

        public override void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed)
        {
            Vector4 value = valueSource.Get<Vector4>(boxed);

            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.Z);
            writer.WriteNumberValue(value.W);
            writer.WriteEndArray();
        }
    }
}
