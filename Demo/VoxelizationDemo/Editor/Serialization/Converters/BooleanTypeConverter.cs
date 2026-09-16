using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal sealed class BooleanTypeConverter : TypeConverter<bool>
    {
        public override void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed)
        {
            valueSource.Set(boxed, reader.GetBoolean());
        }

        public override void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed)
        {
            writer.WriteBooleanValue(valueSource.Get<bool>(boxed));
        }
    }
}
