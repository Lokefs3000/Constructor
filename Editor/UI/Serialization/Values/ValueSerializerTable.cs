using Editor.UI.Datatypes;
using Editor.UI.Serialization.Helpers;
using Editor.UI.Serialization.Values.Structs;
using Primary.Common;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Values
{
    internal sealed class ValueSerializerTable
    {
        private readonly FrozenDictionary<Type, IGenericValueSerializer> _serializers;

        internal ValueSerializerTable(Dictionary<Type, IGenericValueSerializer> serializers)
        {
            _serializers = serializers.ToFrozenDictionary();
        }

        internal string? Serialize<T>(T value)
        {
            Type type = typeof(T);
            if (type.IsEnum)
            {
                return value?.ToString();
            }
            else if (_serializers.TryGetValue(type, out IGenericValueSerializer? serializer))
            {
                return serializer.Serialize(value);
            }

            EdLog.Serialization.Error("No value serializer found for type: {t}", type);
            return null;
        }

        internal bool Deserialize<T>(string value, out T? deserialized)
        {
            Type type = typeof(T);
            if (type.IsEnum)
            {
                return EnumGenericHelper<T>.TryParse(value, false, out deserialized);
            }
            else if (_serializers.TryGetValue(type, out IGenericValueSerializer? serializer))
            {
                return serializer.Deserialize(value, out deserialized);
            }

            deserialized = default;

            EdLog.Serialization.Error("No value serializer found for type: {t}", type);
            return false;
        }

        internal static readonly ValueSerializerTable Default = new ValueSerializerTable(new() {
            { typeof(byte), new ByteSerializer() },
            { typeof(sbyte), new SByteSerializer() },
            { typeof(short), new ShortSerializer() },
            { typeof(ushort), new UShortSerializer() },
            { typeof(int), new IntSerializer() },
            { typeof(uint), new UIntSerializer() },
            { typeof(long), new LongSerializer() },
            { typeof(ulong), new ULongSerializer() },
            { typeof(bool), new BooleanSerializer() },
            { typeof(string), new StringSerializer() },
            { typeof(float), new SingleSerializer() },
            { typeof(double), new DoubleSerializer() },
            { typeof(UIValue), new UIValueSerializer() },
            { typeof(UIValue2), new UIValue2Serializer() },
            { typeof(Color), new ColorSerializer() },
            { typeof(UIColor), new UIColorSerializer() },
            });
    }
}
