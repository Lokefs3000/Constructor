using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal interface ITypeConverter
    {
        public void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed);
        public void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed);
    }

    internal abstract class TypeConverter<T> : ITypeConverter
    {
        public abstract void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed);
        public abstract void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed);
    }
}
