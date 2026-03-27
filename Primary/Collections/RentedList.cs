using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Collections
{
    public ref struct RentedList<T> : IDisposable, IList<T>, IReadOnlyList<T>, IEnumerable<T>
    {
        private readonly ArrayPool<T> _sourcePool;
        private bool _clearOnReturn;

        private int _version;

        private T[] _array;

        private int _count;
        private int _capacity;

        public RentedList(ArrayPool<T> sourcePool, int initialCapacity = 0)
        {
            Guard.IsGreaterThanOrEqualTo(initialCapacity, 0);

            _sourcePool = ArrayPool<T>.Shared;
            _clearOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

            _version = 0;

            _array = initialCapacity > 0 ? _sourcePool.Rent(initialCapacity) : Array.Empty<T>();

            _count = 0;
            _capacity = _array.Length;
        }

        public RentedList() : this(ArrayPool<T>.Shared, 0)
        {

        }

        public RentedList(int initialCapacity = 0) : this(ArrayPool<T>.Shared, initialCapacity)
        {

        }

        public void Dispose()
        {
            if (_array != Array.Empty<T>())
            {
                if (_clearOnReturn)
                    Array.Clear(_array);

                _sourcePool.Return(_array);
                _array = Array.Empty<T>();
            }

            _count = 0;
            _capacity = 0;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(_array, item, 0, _count);
        }

        public void Insert(int index, T item)
        {
            if (index == _count)
            {
                Add(item);
                return;
            }

            Guard.IsInRange(index, 0, _count);

            if (_count == _array.Length)
            {
                _capacity = Math.Max(_capacity, _capacity + 1) * 2;

                T[] newArray = _sourcePool.Rent(_capacity);
                Array.Copy(_array, newArray, _count);

                if (_array != Array.Empty<T>())
                    _sourcePool.Return(_array, _clearOnReturn);
                _array = newArray;
            }

            Array.Copy(_array, index, _array, index + 1, _count - index);
            _array[index] = item;

            ++_count;
        }

        public void RemoveAt(int index)
        {
            Guard.IsInRange(index, 0, _count);

            if (index != --_count)
            {
                Array.Copy(_array, index + 1, _array, index, _count - index);
                Array.Clear(_array, _count + 1, 1);
            }
            else
                Array.Clear(_array, index, 1);
        }

        public void Add(T item)
        {
            if (_count == _array.Length)
            {
                _capacity = Math.Max(Math.Max(_capacity, _count), 16) * 2;

                T[] newArray = _sourcePool.Rent(_capacity);
                Array.Copy(_array, newArray, _count);

                if (_array != Array.Empty<T>())
                    _sourcePool.Return(_array, _clearOnReturn);
                _array = newArray;
            }

            _array[_count++] = item;
        }

        public void Clear()
        {
            Array.Clear(_array);

            _count = 0;
        }

        public bool Contains(T item)
        {
            return _array.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Guard.IsInRange(arrayIndex, 0, _count);
            _array.AsSpan(arrayIndex, _count - arrayIndex).CopyTo(array);
        }

        public void CopyTo(Span<T> span, int spanIndex)
        {
            Guard.IsInRange(spanIndex, 0, _count);
            _array.AsSpan(spanIndex, _count - spanIndex).CopyTo(span);
        }

        public bool Remove(T item)
        {
            int index = Array.IndexOf(_array, item, 0, _count);
            if (index == -1)
                return false;

            if (index != --_count)
            {
                Array.Copy(_array, index + 1, _array, index, _count - index);
                Array.Clear(_array, _count + 1, 1);
            }
            else
                Array.Clear(_array, index, 1);

            return true;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Span<T> AsSpan() => _array.AsSpan(0, _count);
        public Span<T> AsSpan(int start, int length)
        {
            Guard.IsInRange(start, 0, _count);
            Guard.IsInRange(length, 0, _count - start);

            return _array.AsSpan(start, length);
        }

        public T[] ToArray() => _array.AsSpan(0, _count).ToArray();
        public T[] ToArray(int start, int length) => _array.AsSpan(start, length).ToArray();

        public ref T DangerousGetReference()
        {
            return ref _array.DangerousGetReference();
        }

        public ref T DangerousGetReferenceAt(int i)
        {
            return ref _array.DangerousGetReferenceAt(i);
        }

        T IList<T>.this[int index]
        {
            get
            {
                Guard.IsInRange(index, 0, _count);
                return _array[index];
            }
            set
            {
                Guard.IsInRange(index, 0, _count);
                _array[index] = value;
            }
        }

        T IReadOnlyList <T>.this[int index]
        {
            get
            {
                Guard.IsInRange(index, 0, _count);
                return _array[index];
            }
        }

        public ref T this[int index]
        {
            get
            {
                Guard.IsInRange(index, 0, _count);
                return ref _array[index];
            }
        }

        public int Count => _count;
        public int Capacity => _capacity;

        public bool IsReadOnly => false;

        public bool IsEmpty => _count == 0;

        public ArrayPool<T> SourcePool => _sourcePool;
        public bool ClearOnReturn { get => _clearOnReturn; set => _clearOnReturn = value; }

        public struct Enumerator : IEnumerator<T>
        {
            private T[] _array;
            private int _count;

            private int _version;

            private int _index;
            private T? _current;

            public Enumerator(ref RentedList<T> list)
            {
                _array = list._array;
                _count = list._count;

                _version = list._version;

                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                //Guard.Equals(_list._version, _version);

                if (_index < _count)
                {
                    _current = _array[_index];
                    ++_index;
                    return true;
                }

                _current = default;
                _index = -1;
                return false;
            }

            public void Reset()
            {
                //Guard.Equals(_list._version, _version);

                _index = 0;
                _current = default;
            }

            public T Current => _current!;
            object? IEnumerator.Current
            {
                get
                {
                    Guard.IsGreaterThan(_index, 0);
                    return _current;
                }
            }
        }
    }
}
