using System.Buffers;
using System.Collections;
using System.Diagnostics;

namespace Primary.Common
{
    public struct PoolArray<T> : IEnumerable<T>, IArrayIterator<T>, IDisposable
    {
        private T[]? _array;
        private bool _clearOnReturn;

        public PoolArray(T[] array, bool clearOnReturn)
        {
            _array = array;
            _clearOnReturn = clearOnReturn;
        }

        public void Dispose()
        {
            if (_array != null)
                ArrayPool<T>.Shared.Return(_array, _clearOnReturn);
            _array = null;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)_array!.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _array!.GetEnumerator();
        }

        public Span<T> AsSpan()
        {
            return _array!.AsSpan();
        }

        public Span<T> AsSpan(int start)
        {
            return _array!.AsSpan(start);
        }

        public Span<T> AsSpan(int start, int length)
        {
            return _array!.AsSpan(start, length);
        }

        T IArrayIterator<T>.this[int index]
        {
            get => _array![index];
            set => _array![index] = value;
        }

        public ref T this[int index]
        {
            get => ref _array![index];
        }

        public T[] Array => _array!;
        public bool ClearOnReturn { get => _clearOnReturn; set => _clearOnReturn = value; }

        public static explicit operator T[](PoolArray<T> array) => array._array!;
        public static implicit operator PoolArray<T>(T[] array) => new PoolArray<T>(array, false);

        public static readonly PoolArray<T> Empty = new PoolArray<T>(null, false);
    }

    internal interface IArrayIterator<T>
    {
        public T this[int index] { get; set; }
    }

    internal class PoolArrayDebugView<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PoolArray<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => (T[])_entity;

        public PoolArrayDebugView(PoolArray<T> entity) => _entity = entity;
    }
}
