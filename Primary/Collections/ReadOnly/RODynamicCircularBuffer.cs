using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Primary.Collections.ReadOnly
{
    public readonly record struct RODynamicCircularBuffer<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private readonly DynamicCircularBuffer<T> _array;

        public RODynamicCircularBuffer(DynamicCircularBuffer<T> array)
        {
            _array = array;
        }

        public T Front() => _array.Front();
        public T Back() => _array.Back();

        public bool TryGetFront([NotNullWhen(true)] out T? item) => _array.TryGetFront(out item);
        public bool TryGetBack([NotNullWhen(true)] out T? item) => _array.TryGetBack(out item);

        public ReadOnlySpan<T> AsSpan() => _array.AsSpan();

        public IEnumerator<T> GetEnumerator() => _array.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _array.GetEnumerator();

        public T this[int index] => _array[index];

        public int Count => _array.Count;

        public int Tail => _array.Tail;
        public int Head => _array.Head;

        public bool IsEmpty => _array.IsEmpty;

        public static implicit operator RODynamicCircularBuffer<T>(DynamicCircularBuffer<T> array) => new RODynamicCircularBuffer<T>(array);
    }
}
