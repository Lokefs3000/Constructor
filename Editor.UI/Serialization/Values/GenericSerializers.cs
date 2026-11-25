using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Editor.UI.Serialization.Values
{
    [ValueSerializerTarget(typeof(byte))]
    internal sealed class ByteSerializer : IValueSerializer<byte>
    {
        public static string Serialize(byte value) => value.ToString();
        public static bool Deserialize(string value, out byte deserialized) => byte.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(sbyte))]
    internal sealed class SByteSerializer : IValueSerializer<sbyte>
    {
        public static string Serialize(sbyte value) => value.ToString();
        public static bool Deserialize(string value, out sbyte deserialized) => sbyte.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(short))]
    internal sealed class ShortSerializer : IValueSerializer<short>
    {
        public static string Serialize(short value) => value.ToString();
        public static bool Deserialize(string value, out short deserialized) => short.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(ushort))]
    internal sealed class UShortSerializer : IValueSerializer<ushort>
    {
        public static string Serialize(ushort value) => value.ToString();
        public static bool Deserialize(string value, out ushort deserialized) => ushort.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(int))]
    internal sealed class IntSerializer : IValueSerializer<int>
    {
        public static string Serialize(int value) => value.ToString();
        public static bool Deserialize(string value, out int deserialized) => int.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(uint))]
    internal sealed class UIntSerializer : IValueSerializer<uint>
    {
        public static string Serialize(uint value) => value.ToString();
        public static bool Deserialize(string value, out uint deserialized) => uint.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(long))]
    internal sealed class LongSerializer : IValueSerializer<long>
    {
        public static string Serialize(long value) => value.ToString();
        public static bool Deserialize(string value, out long deserialized) => long.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(ulong))]
    internal sealed class ULongSerializer : IValueSerializer<ulong>
    {
        public static string Serialize(ulong value) => value.ToString();
        public static bool Deserialize(string value, out ulong deserialized) => ulong.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(bool))]
    internal sealed class BooleanSerializer : IValueSerializer<bool>
    {
        public static string Serialize(bool value) => value.ToString();
        public static bool Deserialize(string value, out bool deserialized) => bool.TryParse(value, out deserialized);
    }

    [ValueSerializerTarget(typeof(string))]
    internal sealed class StringSerializer : IValueSerializer<string>
    {
        public static string? Serialize(string? value) => value?.ToString();
        public static bool Deserialize(string value, out string deserialized)
        {
            deserialized = value.ToString();
            return true;
        }
    }

    [ValueSerializerTarget(typeof(float))]
    internal sealed class SingleSerializer : IValueSerializer<float>
    {
        public static string Serialize(float value) => value.ToString(CultureInfo.InvariantCulture);
        public static bool Deserialize(string value, out float deserialized) => float.TryParse(value, CultureInfo.InvariantCulture, out deserialized);
    }

    [ValueSerializerTarget(typeof(double))]
    internal sealed class DoubleSerializer : IValueSerializer<double>
    {
        public static string Serialize(double value) => value.ToString(CultureInfo.InvariantCulture);
        public static bool Deserialize(string value, out double deserialized) => double.TryParse(value, CultureInfo.InvariantCulture, out deserialized);
    }
}
