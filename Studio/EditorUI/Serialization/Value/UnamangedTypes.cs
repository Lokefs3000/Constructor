using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Serialization.Value
{
    [ValueConverter]
    internal sealed class SByteValueConverter : ValueConverter<sbyte>
    {
        public override sbyte TryDeserialize(ReadOnlySpan<char> source) => sbyte.Parse(source);
        public override string TrySerialize(sbyte value) => value.ToString();
    }

    [ValueConverter]
    internal sealed class ByteValueConverter : ValueConverter<byte>
    {
        public override byte TryDeserialize(ReadOnlySpan<char> source) => byte.Parse(source);
        public override string TrySerialize(byte value) => value.ToString();
    }

    [ValueConverter]
    internal sealed class ShortValueConverter : ValueConverter<short>
    {
        public override short TryDeserialize(ReadOnlySpan<char> source) => short.Parse(source);
        public override string TrySerialize(short value) => value.ToString();
    }

    [ValueConverter]
    internal sealed class UShortValueConverter : ValueConverter<ushort>
    {
        public override ushort TryDeserialize(ReadOnlySpan<char> source) => ushort.Parse(source);
        public override string TrySerialize(ushort value) => value.ToString();
    }

    [ValueConverter]
    internal sealed class IntValueConverter : ValueConverter<int>
    {
        public override int TryDeserialize(ReadOnlySpan<char> source) => int.Parse(source);
        public override string TrySerialize(int value) => value.ToString();
    }

    [ValueConverter]
    internal sealed class UIntValueConverter : ValueConverter<uint>
    {
        public override uint TryDeserialize(ReadOnlySpan<char> source) => uint.Parse(source);
        public override string TrySerialize(uint value) => value.ToString();
    }

    [ValueConverter]
    internal sealed class LongValueConverter : ValueConverter<long>
    {
        public override long TryDeserialize(ReadOnlySpan<char> source) => long.Parse(source);
        public override string TrySerialize(long value) => value.ToString();
    }

    [ValueConverter]
    internal sealed class ULongValueConverter : ValueConverter<ulong>
    {
        public override ulong TryDeserialize(ReadOnlySpan<char> source) => ulong.Parse(source);
        public override string TrySerialize(ulong value) => value.ToString();
    }
}
