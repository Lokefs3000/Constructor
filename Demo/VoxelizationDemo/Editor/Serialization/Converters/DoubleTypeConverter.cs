using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal sealed class DoubleTypeConverter : TypeConverter<double>
    {
        public override void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed)
        {
            valueSource.Set(boxed, reader.GetDouble());
        }

        public override void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed)
        {
            writer.WriteNumberValue(valueSource.Get<double>(boxed));
        }
    }
}
