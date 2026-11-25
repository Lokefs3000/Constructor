using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;

namespace Primary.Rendering.Utility
{
    public record struct PooledSwapAndPopList<T> : IDisposable
    {
        private readonly ArrayPool<T?> _arrayPool;

        private T?[] _array;
        private int _count;

        public PooledSwapAndPopList(ArrayPool<T?> arrayPool, int capacity)
        {
            Guard.IsGreaterThan(capacity, 0);

            _arrayPool = arrayPool;

            _array = capacity > 0 ? arrayPool.Rent(capacity) : [];
            _count = 0;
        }

        public PooledSwapAndPopList(int capacity) : this(ArrayPool<T?>.Shared, capacity)
        {
        }

        public PooledSwapAndPopList() : this(ArrayPool<T?>.Shared, 0)
        {
        }

        public void Dispose()
        {
            if (_array.Length > 0)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    Array.Clear(_array, 0, _count);

                _arrayPool.Return(_array);
            }

            _array = [];
            _count = 0;
        }

        public void EnsureCapacity(int capacity)
        {
            if (_array.Length < capacity)
            {
                int newCapacity = Math.Max(Math.Max(capacity, 16), _array.Length * 2);

                T?[] newArray = _arrayPool.Rent(newCapacity);

                if (_count > 0)
                    Array.Copy(_array, newArray, _count);
                
                if (_array.Length > 0)
                {
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        Array.Clear(_array, 0, _count);
                    _arrayPool.Return(_array);
                }

                _array = newArray;
            }
        }

        public int Add(T item)
        {
            if (_array.Length <= _count)
            {
                EnsureCapacity(_array.Length + 1);
            }

            int index = _count++;
            _array.DangerousGetReferenceAt(index) = item;

            return index;
        }

        public void RemoveAt(int index, out int movedIndex)
        {
            Guard.IsInRange(index, 0, _count);

            if (index < --_count)
            {
                movedIndex = index;

                _array.DangerousGetReferenceAt(index) = _array.DangerousGetReferenceAt(_count);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    _array.DangerousGetReferenceAt(_count) = default;
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _array.DangerousGetReferenceAt(_count) = default;

            movedIndex = -1;
        }

        public readonly ref T? this[int index]
        {
            get
            {
                Guard.IsInRange(index, 0, _count);
                return ref _array.DangerousGetReferenceAt(index);
            }
        }

        public readonly int Count => _count;
        public readonly int Capacity => _array.Length;

        public readonly bool IsEmpty => _count == 0;
    }
}
