using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace VoxelizationDemo.Editor.Serialization
{
    internal readonly record struct PropertyList
    {
        private readonly PropertyProvider _provider;
        private readonly FrozenDictionary<byte[], ValueSource> _sources;

        private readonly FrozenDictionary<byte[], ValueSource>.AlternateLookup<ReadOnlySpan<byte>> _lookup;

        internal PropertyList(PropertyProvider provider, FrozenDictionary<byte[], ValueSource> sources)
        {
            _provider = provider;
            _sources = sources;

            //_lookup = sources.GetAlternateLookup<ReadOnlySpan<byte>>();
        }

        public bool TryGetSource(ReadOnlySpan<byte> name, [NotNullWhen(true)] out ValueSource source)
        {
            return _sources.TryGetValue([.. name], out source); //_lookup.TryGetValue(name, out source);
        }

        public IEnumerable<KeyValuePair<byte[], ValueSource>> GetSourceEnumerator() => _sources;
    }
}
