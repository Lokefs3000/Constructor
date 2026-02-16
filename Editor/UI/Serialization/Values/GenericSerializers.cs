using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Editor.UI.Serialization.Values
{
    internal sealed class ByteSerializer : ValueSerializer<byte>
    {
        public override string Serialize(byte value) => value.ToString();
        public override bool Deserialize(string value, out byte deserialized) => byte.TryParse(value, out deserialized);
    }

    internal sealed class SByteSerializer : ValueSerializer<sbyte>
    {
        public override string Serialize(sbyte value) => value.ToString();
        public override bool Deserialize(string value, out sbyte deserialized) => sbyte.TryParse(value, out deserialized);
    }

    internal sealed class ShortSerializer : ValueSerializer<short>
    {
        public override string Serialize(short value) => value.ToString();
        public override bool Deserialize(string value, out short deserialized) => short.TryParse(value, out deserialized);
    }

    internal sealed class UShortSerializer : ValueSerializer<ushort>
    {
        public override string Serialize(ushort value) => value.ToString();
        public override bool Deserialize(string value, out ushort deserialized) => ushort.TryParse(value, out deserialized);
    }

    internal sealed class IntSerializer : ValueSerializer<int>
    {
        public override string Serialize(int value) => value.ToString();
        public override bool Deserialize(string value, out int deserialized) => int.TryParse(value, out deserialized);
    }

    internal sealed class UIntSerializer : ValueSerializer<uint>
    {
        public override string Serialize(uint value) => value.ToString();
        public override bool Deserialize(string value, out uint deserialized) => uint.TryParse(value, out deserialized);
    }

    internal sealed class LongSerializer : ValueSerializer<long>
    {
        public override string Serialize(long value) => value.ToString();
        public override bool Deserialize(string value, out long deserialized) => long.TryParse(value, out deserialized);
    }

    internal sealed class ULongSerializer : ValueSerializer<ulong>
    {
        public override string Serialize(ulong value) => value.ToString();
        public override bool Deserialize(string value, out ulong deserialized) => ulong.TryParse(value, out deserialized);
    }

    internal sealed class BooleanSerializer : ValueSerializer<bool>
    {
        public override string Serialize(bool value) => value.ToString();
        public override bool Deserialize(string value, out bool deserialized) => bool.TryParse(value, out deserialized);
    }

    internal sealed class StringSerializer : ValueSerializer<string>
    {
        public override string? Serialize(string? value) => value?.ToString();
        public override bool Deserialize(string value, out string deserialized)
        {
            deserialized = value.ToString();
            return true;
        }
    }

    internal sealed class SingleSerializer : ValueSerializer<float>
    {
        public override string Serialize(float value) => value.ToString(CultureInfo.InvariantCulture);
        public override bool Deserialize(string value, out float deserialized) => float.TryParse(value, CultureInfo.InvariantCulture, out deserialized);
    }

    internal sealed class DoubleSerializer : ValueSerializer<double>
    {
        public override string Serialize(double value) => value.ToString(CultureInfo.InvariantCulture);
        public override bool Deserialize(string value, out double deserialized) => double.TryParse(value, CultureInfo.InvariantCulture, out deserialized);
    }
}
