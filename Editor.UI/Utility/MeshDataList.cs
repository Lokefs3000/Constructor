using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.UI.Utility
{
    internal struct MeshDataList<T> where T : unmanaged
    {
        private T[] _array;
        private int _count;

        public MeshDataList() : this(0)
        {
        }

        public MeshDataList(int initialCapacity)
        {
            _array = initialCapacity > 0 ? new T[initialCapacity] : Array.Empty<T>();
            _count = 0;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void SetUnchecked(int index, in T item)
        {
            Debug.Assert(index >= 0 && index < _array.Length);
            _array.DangerousGetReferenceAt(index) = item;
        }

        public void EnsureSizeFor(int count)
        {
            int nextCount = _count + count;
            if (nextCount >= _array.Length)
            {
                int capacity = _array.Length;
                do { capacity *= 2; } while (capacity <= nextCount);

                T[] newArray = new T[capacity];
                Array.Copy(_array, newArray, _count);

                _array = newArray;
            }
        }

        public Span<T> Span => _array.AsSpan(0, _count);

        public int Count { get => _count; set => _count = value; }
        public int Capacity => _array.Length;

        public bool IsEmpty => _count == 0;
    }
}
