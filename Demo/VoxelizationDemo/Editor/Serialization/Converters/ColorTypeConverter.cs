using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Serialization.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal sealed class ColorTypeConverter : TypeConverter<Color>
    {
        public override void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start array");

            Color v = default;
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
            Color value = valueSource.Get<Color>(boxed);

            writer.WriteStartArray();
            writer.WriteNumberValue(value.R);
            writer.WriteNumberValue(value.G);
            writer.WriteNumberValue(value.B);
            writer.WriteNumberValue(value.A);
            writer.WriteEndArray();
        }
    }
}
