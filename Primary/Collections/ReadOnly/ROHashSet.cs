using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Collections.ReadOnly.Display;

namespace Primary.Collections.ReadOnly
{
    [DebuggerTypeProxy(typeof(ROHashSetDisplay<>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly record struct ROHashSet<T> : IEquatable<ROHashSet<T>>, ISet<T>, IReadOnlySet<T>, IImmutableSet<T>
    {
        private readonly HashSet<T> _set;

        public ROHashSet(HashSet<T> set)
        {
            _set = set;
        }

        #region Set<T>
        bool ISet<T>.Add(T item) => throw new NotImplementedException();

        void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotImplementedException();
        void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotImplementedException();

        public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotImplementedException();
        void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotImplementedException();

        void ICollection<T>.Add(T item) => throw new NotImplementedException();
        void ICollection<T>.Clear() => throw new NotImplementedException();

        public bool Contains(T item) => _set.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

        bool ICollection<T>.Remove(T item) => throw new NotImplementedException();

        public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
        #endregion
        #region ImmutableSet<T>
        public IImmutableSet<T> Add(T value)
        {
            HashSet<T> values = [.. _set, value];
            return new ROHashSet<T>(values);
        }

        public IImmutableSet<T> Clear()
        {
            return new ROHashSet<T>([]);
        }

        public IImmutableSet<T> Except(IEnumerable<T> other)
        {
            HashSet<T> values = [.. _set];
            values.ExceptWith(other);
            return new ROHashSet<T>(values);
        }

        public IImmutableSet<T> Intersect(IEnumerable<T> other)
        {
            HashSet<T> values = [.. _set];
            values.IntersectWith(other);
            return new ROHashSet<T>(values);
        }

        public IImmutableSet<T> Remove(T value)
        {
            HashSet<T> values = [.. _set];
            values.Remove(value);
            return new ROHashSet<T>(values);
        }

        public IImmutableSet<T> SymmetricExcept(IEnumerable<T> other)
        {
            HashSet<T> values = [.. _set];
            values.SymmetricExceptWith(other);
            return new ROHashSet<T>(values);
        }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            return _set.TryGetValue(equalValue, out actualValue);
        }
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

        public IImmutableSet<T> Union(IEnumerable<T> other)
        {
            HashSet<T> values = [.. _set];
            values.UnionWith(other);
            return new ROHashSet<T>(values);
        }
        #endregion

        public override int GetHashCode() => _set.GetHashCode();
        public override string ToString() => $"ROHashSet<{typeof(T).Name}>({_set.Count})";

        public int Count => _set.Count;
        public int Capacity => _set.Capacity;

        public IEqualityComparer<T> Comparer => _set.Comparer;

        public bool IsReadOnly => true;

        public static readonly ROHashSet<T> Empty = new ROHashSet<T>([]);

        public static implicit operator ROHashSet<T>(HashSet<T> set) => new ROHashSet<T>(set);
    }
}
