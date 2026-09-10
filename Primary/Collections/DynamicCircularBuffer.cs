using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Collections.Display;
using Primary.Common;

namespace Primary.Collections
{
    [DebuggerTypeProxy(typeof(DynamicCircularBufferDebugView<>))]
    public sealed class DynamicCircularBuffer<T> : IEnumerable<T>, IReadOnlyCollection<T>, IArrayIterator<T>
    {
        private T[] _array;
        private int _size;

        private int _version;

        // <Front> -------- <Back>
        private int _end;
        private int _start;

        private bool _isEndAtStart;

        public DynamicCircularBuffer()
        {
            _array = Array.Empty<T>();
            _size = 0;

            _version = 0;

            _start = 0;
            _end = 0;

            _isEndAtStart = false;
        }

        public DynamicCircularBuffer(int initialSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(initialSize);

            _array = initialSize == 0 ? Array.Empty<T>() : new T[initialSize];

            _start = initialSize / 2;
            _end = _start;
        }

        private void ResizeArray(bool isResizingForBack)
        {
            int newSize = Math.Max(_size * 2, 4);
            T[] newArray = new T[newSize];

            if (_end < _start)
            {
                if (_isEndAtStart)
                {
                    // copy     <Front> ----- |ArrayEnd|
                    Array.Copy(_array, _start, newArray, _size, _array.Length - _start);

                    // copy     |ArrayStart| ----- <Back>
                    Array.Copy(_array, 0, newArray, _size + (_array.Length - _start), _end);
                }
                else
                {
                    // copy     <Back> ----- |ArrayEnd|
                    Array.Copy(_array, _end, newArray, _size, _array.Length - _end);

                    // copy     |ArrayStart| ----- <Back>
                    Array.Copy(_array, 0, newArray, _size + (_array.Length - _end), _end);
                }
            }
            else
            {
                Array.Copy(_array, _start, newArray, _size, _end - _start);
            }

            _array = newArray;

            if (!isResizingForBack)
            {
                _start += _size;
                _end += _size;
            }

            _isEndAtStart = false;
        }

        public void Clear()
        {
            _start = _array.Length / 2;
            _end = _start;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Array.Clear(_array);
        }

        public void PushFront(T item)
        {
            if (_size == _array.Length)
                ResizeArray(false);

            _array[_start--] = item;

            ++_size;
            ++_version;

            if (_start < 0)
                _start = _array.Length - 1;
        }

        public void PushBack(T item)
        {
            if (_size == _array.Length)
                ResizeArray(true);

            _array[_end++] = item;

            ++_size;
            ++_version;

            if (_end == _array.Length)
            {
                _end = 0;
                _isEndAtStart = true;
            }
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

        public bool TryGetFront([MaybeNullWhen(false)] out T item)
        {
            if (_size > 0)
            {
                item = _array[_start];
                return true;
            }

            item = default;
            return false;
        }

        public bool TryGetBack([MaybeNullWhen(false)] out T item)
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
            return _end < _start ? (_start + index) % _array.Length : _start + index;
        }

        public Span<T> AsSpan() => _array.AsSpan();

        // TODO: consider using ArraySegment<T> and yield return instead of a custom enumerator?
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        T IArrayIterator<T>.this[int index]
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
            }
        }

        public ref T this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_size);
                return ref _array[GetInternalIndex(index)];
            }
        }

        public int Count => _size;

        public int Tail => _start;
        public int Head => _end;

        public bool IsEmpty => _size == 0;

        public struct Enumerator : IEnumerator<T>
        {
            private readonly DynamicCircularBuffer<T> _buffer;

            private readonly int _version;
            private int _index;

            private T? _current;

            public Enumerator(DynamicCircularBuffer<T> buffer)
            {
                _buffer = buffer;

                _version = buffer._version;
                _index = buffer._end == 0 ? buffer._array.Length - 1 : buffer._end - 1;

                if (buffer._end == buffer._start)
                    _index = -1;

                _current = default;
            }

            public void Reset()
            {
                if (_version != _buffer._version)
                    throw new InvalidOperationException("Collection was modified outside of the enumerator");

                _index = _buffer._end == 0 ? _buffer._array.Length - 1 : _buffer._end - 1;
                _current = default;

                if (_buffer._end == _buffer._start)
                    _index = -1;
            }

            void IDisposable.Dispose()
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

                _current = _buffer._array[_index];

                if (_index == _buffer._start)
                {
                    _index = -1;
                    return true;
                }

                if (--_index < 0)
                    _index = _buffer._array.Length - 1;
                return true;
            }

            public readonly T Current => _current!;
            readonly object IEnumerator.Current => _current!;
        }
    }
}
