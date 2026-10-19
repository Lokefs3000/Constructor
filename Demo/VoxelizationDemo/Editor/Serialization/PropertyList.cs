using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.HighPerformance;

namespace VoxelizationDemo.Editor.Serialization
{
    internal readonly record struct PropertyList
    {
        private readonly PropertyProvider _provider;
        private readonly FrozenDictionary<Utf8TableKey, ValueSource> _sources;

        private readonly FrozenDictionary<Utf8TableKey, ValueSource>.AlternateLookup<ReadOnlySpan<byte>> _lookup;

        internal PropertyList(PropertyProvider provider, FrozenDictionary<Utf8TableKey, ValueSource> sources)
        {
            _provider = provider;
            _sources = sources;

            //_lookup = sources.GetAlternateLookup<ReadOnlySpan<byte>>();
        }

        public bool TryGetSource(ReadOnlySpan<byte> name, [NotNullWhen(true)] out ValueSource source)
        {
            return _sources.TryGetValue(new Utf8TableKey([.. name]), out source); //_lookup.TryGetValue(name, out source);
        }

        public IEnumerable<KeyValuePair<Utf8TableKey, ValueSource>> GetSourceEnumerator() => _sources;
    }

    internal readonly record struct Utf8TableKey(byte[] Bytes) : IEquatable<Utf8TableKey>
    {
        public readonly bool Equals(Utf8TableKey other) => Bytes.AsSpan().SequenceEqual(other.Bytes.AsSpan());
        public readonly override int GetHashCode() => Bytes.AsSpan().GetDjb2HashCode();

        public static implicit operator Utf8TableKey(byte[] bytes) => new Utf8TableKey(bytes);
        public static implicit operator byte[](Utf8TableKey key) => key.Bytes;
    }
}
