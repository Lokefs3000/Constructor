using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using TerraFX.Interop.Windows;
using VoxelizationDemo.Editor.Serialization.Converters;

namespace VoxelizationDemo.Editor.Serialization
{
    internal sealed class ObjectSerializer
    {
        private readonly SerializationManager _manager;

        internal ObjectSerializer(SerializationManager manager)
        {
            _manager = manager;
        }

        internal void DeserializeIntoBox(object boxed, IEnumerable<SerializedPropertyInfo> propertyInfos)
        {
            PropertyList properties = _manager.PropertyDatabase.QueryProperties(boxed.GetType());
            foreach (SerializedPropertyInfo propertyInfo in propertyInfos)
            {
                if (properties.TryGetSource(propertyInfo.Name, out ValueSource valueSource))
                {
                    if (_manager.ConverterDatabase.TryGetConverter(valueSource.TargetType, out ITypeConverter? converter))
                    {
                        Utf8JsonReader reader = propertyInfo.Value;
                        converter.DeserializeToProperty(ref reader, valueSource, boxed);
                    }
                    else
                    {
                        AppLog.Editor.Warning("Failed to find converter for type '{n}' to deserialize property '{p}'", valueSource.TargetType, Encoding.UTF8.GetString(propertyInfo.Name));
                    }
                }
                else
                {
                    AppLog.Editor.Warning("Failed to find property with name '{n}' in object '{t}'", Encoding.UTF8.GetString(propertyInfo.Name), boxed.GetType());
                }
            }
        }

        internal void DeserializeIntoBox(object boxed, ReadOnlySpan<byte> propertyName, ref Utf8JsonReader reader)
        {
            PropertyList properties = _manager.PropertyDatabase.QueryProperties(boxed.GetType());
            if (properties.TryGetSource(propertyName, out ValueSource valueSource))
            {
                if (_manager.ConverterDatabase.TryGetConverter(valueSource.TargetType, out ITypeConverter? converter))
                {
                    converter.DeserializeToProperty(ref reader, valueSource, boxed);
                }
                else
                {
                    AppLog.Editor.Warning("Failed to find converter for type '{n}' to deserialize property '{p}'", valueSource.TargetType, Encoding.UTF8.GetString(propertyName));
                }
            }
            else
            {
                AppLog.Editor.Warning("Failed to find property with name '{n}' in object '{t}'", Encoding.UTF8.GetString(propertyName), boxed.GetType());
            }
        }

        internal void SerializeFromBox(Utf8JsonWriter writer, object boxed)
        {
            PropertyList properties = _manager.PropertyDatabase.QueryProperties(boxed.GetType());
            foreach (var (nameAsBytes, valueSource) in properties.GetSourceEnumerator())
            {
                if (_manager.ConverterDatabase.TryGetConverter(valueSource.TargetType, out ITypeConverter? converter))
                {
                    writer.WritePropertyName(nameAsBytes);
                    converter.SerializeToWriter(writer, valueSource, boxed);
                }
                else
                {
                    AppLog.Editor.Warning("Failed to find converter for type '{n}' to deserialize property '{p}'", valueSource.TargetType, Encoding.UTF8.GetString(nameAsBytes));
                }
            }
        }
    }
}
