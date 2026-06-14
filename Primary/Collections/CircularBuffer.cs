using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Collections
{
    public sealed class CircularBuffer<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private T[] _array;
        private int _size;

        private int _version;

        // <Front> -------- <Back>
        private int _end;
        private int _start;

        public CircularBuffer(int initialSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(initialSize);

            _array = initialSize == 0 ? Array.Empty<T>() : new T[initialSize];
            _size = 0;

            _version = 0;

            _start = 0;
            _end = 0;
        }

        public void ResizeCapacity(int newCapacity)
        {
            if (_array.Length == newCapacity)
            {
                // clear even if nothing happens
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    Array.Clear(_array);
            }    
            else
            {
                _array = newCapacity == 0 ? Array.Empty<T>() : new T[newCapacity];
            }

            _size = 0;

            _start = 0;
            _end = 0;

            ++_version;
        }

        public void Clear()
        {
            _start = 0;
            _end = 0;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Array.Clear(_array);
        }

        public void PushFront(T item)
        {
            _array[_start--] = item;
            if (_start < 0)
                _start = _array.Length - 1;

            ++_version;

            if (_size == _array.Length)
                _end = _start;
            else
                ++_size;
        }

        public void PushBack(T item)
        {
            _array[_end++] = item;
            if (_end == _array.Length)
                _end = 0;

            ++_version;

            if (_size == _array.Length)
                _start = _end;
            else
                ++_size;
        }

        public T PopFront()
        {
            ArgumentOutOfRangeException.ThrowIfZero(_size);

            ++_version;
            --_size;
            
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                (T item, _array[_start]) = (_array[_start], default!);
                if (++_start == _array.Length)
                    _start = 0;

                return item;
            }
            else
            {
                T item = _array[_start++];
                if (_start == _array.Length)
                    _start = 0;

                return item;
            }
        }

        public T PopBack()
        {
            ArgumentOutOfRangeException.ThrowIfZero(_size);

            ++_version;
            --_size;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                (T item, _array[_end]) = (_array[_end], default!);
                if (--_end == 0)
                    _end = _array.Length - 1;

                return item;
            }
            else
            {
                T item = _array[_end--];
                if (_end == 0)
                    _end = _array.Length - 1;

                return item;
            }
        }

        public T Front()
        {
            ArgumentOutOfRangeException.ThrowIfZero(_size);
            return _array[_start];
        }

        public T Back()
        {
            ArgumentOutOfRangeException.ThrowIfZero(_size);
            return _array[_end];
        }

        public bool TryGetFront([NotNullWhen(true)] out T? item)
        {
            if (_size > 0)
            {
                item = _array[_start];
                return true;
            }

            item = default;
            return false;
        }

        public bool TryGetBack([NotNullWhen(true)] out T? item)
        {
            if (_size > 0)
            {
                item = _array[_end];
                return true;
            }

            item = default;
            return false;
        }

        private int GetInternalIndex(int index)
        {
            return _end <= _start ? (_start + index) % _array.Length : _start + index;
        }

        public Span<T> AsSpan() => _array.AsSpan();

        // TODO: consider using ArraySegment<T> and yield return instead of a custom enumerator?
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_size);
                return _array[GetInternalIndex(index)];
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_size);

                _array[GetInternalIndex(index)] = value;
                ++_version;
            }
        }

        public int Count => _size;
        public int Capacity => _array.Length;

        public int Tail => _start;
        public int Head => _end;

        public bool IsEmpty => _size == 0;
        public bool IsFull => _size == _array.Length;

        public struct Enumerator : IEnumerator<T>
        {
            private readonly CircularBuffer<T> _buffer;

            private readonly int _version;
            private int _index;

            private T? _current;

            public Enumerator(CircularBuffer<T> buffer)
            {
                _buffer = buffer;

                _version = buffer._version;
                _index = buffer._end;

                _current = default;
            }

            public void Reset()
            {
                if (_version != _buffer._version)
                    throw new InvalidOperationException("Collection was modified outside of the enumerator");

                _index = _buffer._start;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _buffer._version)
                    throw new InvalidOperationException("Collection was modified outside of the enumerator");
                if (_index == -1)
                {
                    _current = default;
                    return false;
                }

                _current = _buffer._array[_index++];

                if ((_index = (_index % _buffer._array.Length)) == _buffer._end)
                {
                    _index = -1;
                }

                return true;
            }

            public T Current => _current!;
            object IEnumerator.Current => _current!;
        }
    }
}
