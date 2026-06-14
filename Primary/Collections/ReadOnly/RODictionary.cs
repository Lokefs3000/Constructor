using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;
using static Primary.Scenes.Components.ComponentRegistryEntry;

namespace Primary.Collections.ReadOnly
{
    public readonly record struct RODictionary<TKey, TValue> : IEquatable<RODictionary<TKey, TValue>>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IImmutableDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _dictionary;

        public RODictionary(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        #region Dictionary<TKey, TValue>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException();
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException();

        public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);

        bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException();
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => throw new NotSupportedException("Cannot set keyed value on a readonly dictionary");
        }
        #endregion
        #region IReadOnlyDictionary<TKey, TValue>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        #endregion
        #region IImmutableDictionary<TKey, TValue>
        public IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            Dictionary<TKey, TValue> copy = new Dictionary<TKey, TValue>(_dictionary)
            {
                { key, value }
            };

            return new RODictionary<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            Dictionary<TKey, TValue> copy = new Dictionary<TKey, TValue>(_dictionary);
            foreach (var (key, value) in pairs)
            {
                copy.Add(key, value);
            }

            return new RODictionary<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> Clear()
        {
            return new RODictionary<TKey, TValue>(new Dictionary<TKey, TValue>());
        }

        public IImmutableDictionary<TKey, TValue> Remove(TKey key)
        {
            Dictionary<TKey, TValue> copy = new Dictionary<TKey, TValue>(_dictionary);
            copy.Remove(key);

            return new RODictionary<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            Dictionary<TKey, TValue> copy = new Dictionary<TKey, TValue>(_dictionary);
            foreach (TKey key in keys)
            {
                copy.Remove(key);
            }

            return new RODictionary<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            Dictionary<TKey, TValue> copy = new Dictionary<TKey, TValue>(_dictionary)
            {
                [key] = value
            };

            return new RODictionary<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            Dictionary<TKey, TValue> copy = new Dictionary<TKey, TValue>(_dictionary);
            foreach (var (key, value) in items)
            {
                copy[key] = value;
            }

            return new RODictionary<TKey, TValue>(copy);
        }

        bool IImmutableDictionary<TKey, TValue>.TryGetKey(TKey equalKey, out TKey actualKey) => throw new NotSupportedException();
        #endregion
        #region High-Performance
        public ref readonly TValue GetValueRefOrNullRef(TKey key)
        {
            return ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, key);
        }

        public ref readonly TValue? GetValueRefOrAddDefault(TKey key, out bool exists)
        {
            return ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, key, out exists);
        }
        #endregion

        public override int GetHashCode() => _dictionary.GetHashCode();
        public override string ToString() => $"RODictionary<{typeof(TKey).Name},{typeof(TValue).Name}>({_dictionary.Count})";

        public int Count => _dictionary.Count;
        public bool IsReadOnly => true;

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        public static RODictionary<TKey, TValue> Empty => new RODictionary<TKey, TValue>([]);

        public static implicit operator RODictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary) => new RODictionary<TKey, TValue>(dictionary);
    }
}
