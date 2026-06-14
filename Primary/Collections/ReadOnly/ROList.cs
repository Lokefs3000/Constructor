using CommunityToolkit.HighPerformance;
using System.Collections;
using System.Collections.Immutable;
using System.Xml.Linq;
using TerraFX.Interop.Windows;

namespace Primary.Collections.ReadOnly
{
    public readonly record struct ROList<T> : IEquatable<ROList<T>>, IList<T>, IReadOnlyList<T>, IImmutableList<T>
    {
        private readonly List<T> _list;

        public ROList(List<T> list)
        {
            _list = list;
        }

        #region List<T>
        public int IndexOf(T item) => _list.IndexOf(item);

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        void ICollection<T>.Clear() => throw new NotSupportedException();

        public bool Contains(T item) => _list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        T IList<T>.this[int index]
        {
            get => _list[index];
            set => throw new NotSupportedException("Cannot set indexed value on a readonly list");
        }
        #endregion
        #region IReadOnlyList<T>
        public T this[int index]
        {
            get => _list[index];
        }
        #endregion
        #region IImmutableList<T>
        public IImmutableList<T> Add(T value)
        {
            List<T> copy = [.. _list];
            copy.Add(value);

            return new ROList<T>(copy);
        }

        public IImmutableList<T> AddRange(IEnumerable<T> items)
        {
            List<T> copy = [.. _list];
            copy.AddRange(items);

            return new ROList<T>(copy);
        }

        IImmutableList<T> IImmutableList<T>.Clear()
        {
            return new ROList<T>(new List<T>());
        }

        public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
        {
            Span<T> span = _list.AsSpan();
            return System.MemoryExtensions.IndexOf(span, item, equalityComparer);
        }

        IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
        {
            List<T> copy = [.. _list];
            copy.Insert(index, element);

            return new ROList<T>(copy);
        }

        public IImmutableList<T> InsertRange(int index, IEnumerable<T> items)
        {
            List<T> copy = [.. _list];
            copy.InsertRange(index, items);

            return new ROList<T>(copy);
        }

        public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
        {
            Span<T> span = _list.AsSpan();
            return System.MemoryExtensions.LastIndexOf(span, item, equalityComparer);
        }

        public IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
        {
            List<T> copy = [.. _list];

            equalityComparer ??= EqualityComparer<T>.Default;
            for (int i = 0; i < copy.Count; ++i)
            {
                if (equalityComparer.Equals(copy[i], value))
                {
                    copy.RemoveAt(i);
                    break;
                }
            }

            return new ROList<T>(copy);
        }

        public IImmutableList<T> RemoveAll(Predicate<T> match)
        {
            List<T> copy = [.. _list];

            for (int i = 0; i < copy.Count; ++i)
            {
                if (match(copy[i]))
                {
                    copy.RemoveAt(i);
                    break;
                }
            }

            return new ROList<T>(copy);
        }

        IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
        {
            List<T> copy = [.. _list];
            copy.RemoveAt(index);

            return new ROList<T>(copy);
        }

        public IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
        {
            List<T> copy = [.. _list];

            equalityComparer ??= EqualityComparer<T>.Default;
            foreach (T item in items)
            {
                for (int i = 0; i < copy.Count; ++i)
                {
                    if (equalityComparer.Equals(copy[i], item))
                    {
                        copy.RemoveAt(i);
                        break;
                    }
                }

                if (copy.Count == 0)
                    break;
            }

            return new ROList<T>(copy);
        }

        public IImmutableList<T> RemoveRange(int index, int count)
        {
            List<T> copy = [.. _list];
            copy.RemoveRange(index, count);

            return new ROList<T>(copy);
        }

        public IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
        {
            List<T> copy = [.. _list];

            equalityComparer ??= EqualityComparer<T>.Default;
            for (int i = 0; i < copy.Count; ++i)
            {
                if (equalityComparer.Equals(copy[i], oldValue))
                {
                    copy[i] = newValue;
                    break;
                }
            }

            return new ROList<T>(copy);
        }

        public IImmutableList<T> SetItem(int index, T value)
        {
            List<T> copy = [.. _list];
            copy[index] = value;

            return new ROList<T>(copy);
        }
        #endregion

        public ReadOnlySpan<T> AsSpan() => _list.AsSpan();

        public override int GetHashCode() => _list.GetHashCode();
        public override string ToString() => $"ROList<{typeof(T).Name}>({_list.Count})";

        public int Count => _list.Count;
        public int Capacity => _list.Capacity;

        public bool IsReadOnly => true;

        public static readonly ROList<T> Empty = new ROList<T>([]);

        public static implicit operator ROList<T>(List<T> list) => new ROList<T>(list);
    }
}
