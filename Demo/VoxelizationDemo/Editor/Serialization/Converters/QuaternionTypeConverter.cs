using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal sealed class QuaternionTypeConverter : TypeConverter<Quaternion>
    {
        public override void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start array");

            Quaternion v = default;
            for (int i = 0; i < 4; ++i)
            {
                reader.Read();
                v[i] = reader.GetSingle();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException("Expected end array");

            valueSource.Set(boxed, v);
        }

        public override void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed)
        {
            Quaternion value = valueSource.Get<Quaternion>(boxed);

            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.Z);
            writer.WriteNumberValue(value.W);
            writer.WriteEndArray();
        }
    }
}
