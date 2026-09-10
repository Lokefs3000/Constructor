using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections.Display;
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Collections
{
    [DebuggerTypeProxy(typeof(RentedStackDebugView<>))]
    public record struct RentedStack<T> : IDisposable, ICollection<T>, IReadOnlyCollection<T>, IEnumerable<T>
    {
        private readonly ArrayPool<T> _sourcePool;
        private bool _clearOnReturn;

        private int _version;

        private T[] _array;
        private int _head;

        public RentedStack(ArrayPool<T> sourcePool, int initialCapacity = 0)
        {
            Guard.IsGreaterThanOrEqualTo(initialCapacity, 0);

            _sourcePool = sourcePool;
            _clearOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

            _version = 0;

            _array = _sourcePool.Rent(initialCapacity);
            _head = -1;
        }

        public RentedStack() : this(ArrayPool<T>.Shared, 0)
        {

        }

        public RentedStack(int initialCapacity = 0) : this(ArrayPool<T>.Shared, initialCapacity)
        {

        }

        public void Dispose()
        {
            if (_array != Array.Empty<T>())
            {
                if (_clearOnReturn)
                    Array.Clear(_array, 0, _head + 1);

                _sourcePool.Return(_array);
                _array = Array.Empty<T>();
            }

            _head = -1;
            ++_version;
        }

        private void GrowToAtleast(int size)
        {
            int newCapacity = Math.Max(_array.Length * 2, 4);
            if ((uint)newCapacity > Array.MaxLength)
                newCapacity = Array.MaxLength;

            if (newCapacity < _array.Length)
                return;

            T[] newArray = _sourcePool.Rent(newCapacity);
            if (_head >= 0 && _array.Length > 0)
            {
                Array.Copy(_array, newArray, _head);
                Array.Clear(_array, 0, _head);
            }

            if (_array != Array.Empty<T>())
                _sourcePool.Return(_array);

            _array = newArray;
        }

        public void Clear()
        {
            if (_head >= 0)
            {
                Array.Clear(_array, 0, _head + 1);
            }

            _head = -1;
            ++_version;
        }

        public void Push(T item)
        {
            if (++_head == _array.Length)
                GrowToAtleast(_head);

            _array[_head] = item;
            ++_version;
        }

        public T Pop()
        {
            if (_head < 0)
                throw new InvalidOperationException("Popping an empty stack is not allowed");

            T item = _array[_head];
            if (_clearOnReturn)
                _array[_head] = default!;

            --_head;
            ++_version;
            return item;
        }

        public readonly T Peek()
        {
            if (_head < 0)
                throw new InvalidOperationException("Peeking an empty stack is not allowed");

            return _array[_head];
        }

        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            if (_head < 0)
            {
                result = default;
                return false;
            }
            else
            {
                result = _array[_head];
                if (_clearOnReturn)
                    _array[_head] = default!;

                --_head;
                ++_version;
                return true;
            }
        }

        public readonly bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            if (_head < 0)
            {
                result = default;
                return false;
            }
            else
            {
                result = _array[_head];
                return true;
            }
        }

        public void EnsureCapacity(int capacity)
        {
            if (_array.Length < capacity)
            {
                GrowToAtleast(capacity);
                ++_version;
            }
        }

        public void TrimExcess()
        {
            int count = _head + 1;
            if (count / (double)_array.Length < 0.9)
            {
                T[] newArray = _sourcePool.Rent(count);
                if (count > 0)
                {
                    Array.Copy(_array, newArray, count);
                    Array.Clear(_array, 0, count);
                }

                if (_array != Array.Empty<T>())
                    _sourcePool.Return(_array);

                _array = newArray;
                ++_version;
            }
        }

        public readonly bool Contains(T item) => _head < 0 ? false : _array.AsSpan(0, _head + 1).Contains(item);

        public readonly T[] ToArray() => _head < 0 ? [] : _array[..(_head + 1)];

        public readonly void CopyTo(Span<T> span)
        {
            if (_head >= 0)
            {
                int count = _head + 1;
                if (span.Length < count)
                    throw new ArgumentException("Not enough space available in destination array to copy");

                _array.AsSpan(0, count).CopyTo(span);
            }
        }

        [UnscopedRef]
        public Enumerator GetEnumerator() => new Enumerator(ref this);

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotSupportedException();
        readonly IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

        #region Overloads
        public readonly void CopyTo(T[] array, int arrayIndex = 0)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfLessThan(arrayIndex, 0);

            CopyTo(array.AsSpan(arrayIndex));
        }
        #endregion
        #region Hidden
        readonly void ICollection<T>.Add(T item) => throw new NotSupportedException();
        readonly bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        #endregion

        public readonly int Count => _head + 1;
        public readonly int Capacity => _array.Length;

        public readonly bool IsReadOnly => false;

        public ref struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ref RentedStack<T> _stack;
            private readonly int _version;

            private int _index;
            private T? _currentValue;

            public Enumerator(ref RentedStack<T> stack)
            {
                _stack = ref stack;
                _version = stack._version;

                _index = stack._head;
                _currentValue = default;
            }

            public void Dispose()
            {
                _index = -1;
                _currentValue = default;
            }

            public bool MoveNext()
            {
                if (_version != _stack._version)
                {
                    throw new InvalidOperationException("Enumeration target has been modified");
                }

                if (_index >= 0)
                {
                    _currentValue = _stack._array[_index--];
                    return true;
                }

                _currentValue = default;
                _index = -1;
                return false;
            }

            void IEnumerator.Reset()
            {
                if (_version != _stack._version)
                {
                    throw new InvalidOperationException("Enumeration target has been modified");
                }

                _index = -1;
                _currentValue = default;
            }

            public readonly T Current => _currentValue!;
            readonly object IEnumerator.Current => _currentValue!;
        }
    }
}
