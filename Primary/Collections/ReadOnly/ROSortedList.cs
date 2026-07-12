using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Primary.Collections.ReadOnly.Display;

namespace Primary.Collections.ReadOnly
{
    [DebuggerTypeProxy(typeof(ROSortedListDisplay<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly record struct ROSortedList<TKey, TValue> : IEquatable<ROSortedList<TKey, TValue>>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IImmutableDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly SortedList<TKey, TValue> _list;

        public ROSortedList(SortedList<TKey, TValue> list)
        {
            _list = list;
        }

        #region SortedList<TKey, TValue>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException();
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException();

        public bool Contains(KeyValuePair<TKey, TValue> item) => _list.Contains(item);
        public bool ContainsKey(TKey key) => _list.ContainsKey(key);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_list).CopyTo(array, arrayIndex);

        bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException();
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _list.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public TValue this[TKey key]
        {
            get => _list[key];
            set => throw new NotSupportedException("Cannot set keyed value on a readonly SortedList");
        }
        #endregion
        #region IReadOnlyDictionary<TKey, TValue>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        #endregion
        #region IImmutableDictionary<TKey, TValue>
        public IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            SortedList<TKey, TValue> copy = new SortedList<TKey, TValue>(_list)
            {
                { key, value }
            };

            return new ROSortedList<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            SortedList<TKey, TValue> copy = new SortedList<TKey, TValue>(_list);
            foreach (var (key, value) in pairs)
            {
                copy.Add(key, value);
            }

            return new ROSortedList<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> Clear()
        {
            return new ROSortedList<TKey, TValue>(new SortedList<TKey, TValue>());
        }

        public IImmutableDictionary<TKey, TValue> Remove(TKey key)
        {
            SortedList<TKey, TValue> copy = new SortedList<TKey, TValue>(_list);
            copy.Remove(key);

            return new ROSortedList<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            SortedList<TKey, TValue> copy = new SortedList<TKey, TValue>(_list);
            foreach (TKey key in keys)
            {
                copy.Remove(key);
            }

            return new ROSortedList<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            SortedList<TKey, TValue> copy = new SortedList<TKey, TValue>(_list)
            {
                [key] = value
            };

            return new ROSortedList<TKey, TValue>(copy);
        }

        public IImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            SortedList<TKey, TValue> copy = new SortedList<TKey, TValue>(_list);
            foreach (var (key, value) in items)
            {
                copy[key] = value;
            }

            return new ROSortedList<TKey, TValue>(copy);
        }

        bool IImmutableDictionary<TKey, TValue>.TryGetKey(TKey equalKey, out TKey actualKey) => throw new NotSupportedException();
        #endregion

        public override int GetHashCode() => _list.GetHashCode();
        public override string ToString() => $"ROList<{typeof(TKey).Name},{typeof(TValue).Name}>({_list.Count})";

        public int Count => _list.Count;
        public bool IsReadOnly => true;

        public IList<TKey> Keys => _list.Keys;
        public IList<TValue> Values => _list.Values;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        public static ROSortedList<TKey, TValue> Empty => new ROSortedList<TKey, TValue>([]);

        public static implicit operator ROSortedList<TKey, TValue>(SortedList<TKey, TValue> list) => new ROSortedList<TKey, TValue>(list);
    }
}
