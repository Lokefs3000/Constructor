using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal sealed class UInt16TypeConverter : TypeConverter<ushort>
    {
        public override void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed)
        {
            valueSource.Set(boxed, reader.GetUInt16());
        }

        public override void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed)
        {
            writer.WriteNumberValue(valueSource.Get<ushort>(boxed));
        }
    }
}
