using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;

namespace Primary.Collections
{
    // Based on 'https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1?view=net-10.0'
    public record struct RentedQueue<T> : IDisposable, ICollection<T>, IReadOnlyCollection<T>, IEnumerable<T>
    {
        private readonly ArrayPool<T> _sourcePool;

        private T[] _array;

        private int _size;

        // _head represents the item of which to dequeue
        // _tail represents the index that the next enqueued item will occupy

        private int _head;
        private int _tail;
        private int _version;

        public RentedQueue(ArrayPool<T> sourcePool, int initialCapacity = 0)
        {
            Guard.IsGreaterThanOrEqualTo(initialCapacity, 0);

            _sourcePool = sourcePool;

            // I'm assuming that it will return Array.Empty<T>() if the length is 0 and not allocate a new array
            _array = _sourcePool.Rent(initialCapacity);

            _size = 0;

            _head = 0;
            _tail = 0;
            _version = 0;

            Debug.WriteLineIf(initialCapacity == 0 && _array != Array.Empty<T>(), "ArrayPool<T> returned an allocated array with 0 length");
        }

        public RentedQueue() : this(ArrayPool<T>.Shared, 0)
        {
        }

        public RentedQueue(int initialCapacity) : this(ArrayPool<T>.Shared, initialCapacity)
        {
        }

        public void Dispose()
        {
            if (_array != Array.Empty<T>())
            {
                _sourcePool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                _array = Array.Empty<T>();
            }

            _size = 0;

            _head = 0;
            _tail = 0;
            _version = 0;
        }

        private void GrowCapacity(int capacity)
        {
            int newCapacity = Math.Max(_array.Length * 2, 4);
            if ((uint)newCapacity > Array.MaxLength)
                newCapacity = Array.MaxLength;

            if (newCapacity < capacity)
                newCapacity = capacity;

            T[] newArray = _sourcePool.Rent(newCapacity);
            if (_head != _tail)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, newArray, 0, _tail - _head);
                }
                else
                {
                    Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                }
            }

            if (_array != Array.Empty<T>())
                _sourcePool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

            _array = newArray;

            _head = 0;
            _tail = (_size == newArray.Length) ? 0 : _size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementIndex(ref int index)
        {
            index = (++index == _array.Length) ? 0 : index;
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && _head != _tail)
            {
                // The empty segment is in the middle
                if (_head < _tail)
                {
                    Array.Clear(_array, _head, _tail - _head);
                }
                else
                {
                    Array.Clear(_array, 0, _tail);
                    Array.Clear(_array, _head, _array.Length - _head);
                }
            }

            _size = 0;
            _head = 0;
            _tail = 0;

            ++_version;
        }

        public void Enqueue(T item)
        {
            if (_size == _array.Length)
            {
                GrowCapacity(_size + 1);
            }

            _array[_tail] = item;
            IncrementIndex(ref _tail);

            ++_size;
            ++_version;
        }

        public T Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException("Cannot dequeue on an empty queue");
            }

            T item = _array[_head];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _array[_head] = default!;

            IncrementIndex(ref _head);

            --_size;
            ++_version;

            return item;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            if (_size == 0)
            {
                result = default;
                return false;
            }
            else
            {
                result = _array[_head];
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    _array[_head] = default!;

                IncrementIndex(ref _head);

                --_size;
                ++_version;

                return true;
            }
        }

        public readonly T Peek()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException("Cannot peek on an empty queue");
            }

            return _array[_head];
        }

        public readonly bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            if (_size == 0)
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
                GrowCapacity(capacity);
            }
        }

        public void TrimExcess()
        {
            if (_size / (double)_array.Length < 0.9)
            {
                T[] newArray = _sourcePool.Rent(_size);
                if (_head != _tail)
                {
                    if (_head < _tail)
                    {
                        Array.Copy(_array, _head, newArray, 0, _tail - _head);
                    }
                    else
                    {
                        Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                        Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                    }
                }

                if (_array != Array.Empty<T>())
                    _sourcePool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

                _array = newArray;

                _head = 0;
                _tail = 0;

                ++_version;
            }
        }

        public readonly bool Contains(T item)
        {
            if (_head != _tail)
            {
                if (_head < _tail)
                {
                    return Array.IndexOf(_array, item, _head, _tail - _head) >= 0;
                }
                else
                {
                    return Array.IndexOf(_array, _head, _array.Length - _head) >= 0
                        && Array.IndexOf(_array, 0, _tail) >= 0;
                }
            }

            return false;
        }

        public readonly T[] ToArray()
        {
            if (_head != _tail)
            {
                T[] newArray = new T[_size];
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, newArray, 0, _tail - _head);
                }
                else
                {
                    Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                }

                return newArray;
            }

            return Array.Empty<T>();
        }

        public readonly void CopyTo(Span<T> array)
        {
            if (array.Length < _size)
                throw new ArgumentException("Not enough space available in destination array to copy");

            if (_head != _tail)
            {
                if (_head < _tail)
                {
                    _array.AsSpan(_head, _tail - _head).CopyTo(array);
                }
                else
                {
                    _array.AsSpan(_head, _array.Length - _head).CopyTo(array);
                    _array.AsSpan(0, _tail).CopyTo(array[(_array.Length - _head)..]);
                }
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

        public readonly int Count => _size;
        public readonly int Capacity => _array.Length;

        public readonly bool IsReadOnly => false;

        public ref struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ref RentedQueue<T> _queue;
            private readonly int _version;

            private int _index;
            private T? _currentValue;

            internal Enumerator(ref RentedQueue<T> queue)
            {
                _queue = ref queue;
                _version = queue._version;

                _index = -1;
                _currentValue = default;
            }

            public void Dispose()
            {
                _index = -2;
                _currentValue = default;
            }

            public bool MoveNext()
            {
                if (_version != _queue._version)
                {
                    throw new InvalidOperationException("Enumeration target has been modified");
                }

                int offset = _index + 1;
                while ((uint)offset < (uint)_queue._size)
                {
                    _index = offset;

                    int index = _queue._head + offset;
                    if ((uint)index < (uint)_queue._array.Length)
                    {
                        _currentValue = _queue._array[index];
                    }
                    else
                    {
                        index -= _queue._array.Length;
                        _currentValue = _queue._array[index];
                    }

                    return true;
                }

                _index = -2;
                _currentValue = default;
                return false;
            }

            void IEnumerator.Reset()
            {
                if (_version != _queue._version)
                {
                    throw new InvalidOperationException("Enumeration target has been modified");
                }

                _index = -1;
                _currentValue = default;
            }

            public readonly T Current => _currentValue!;
            readonly object IEnumerator.Current => Current!;
        }
    }
}
