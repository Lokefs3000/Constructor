using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Collections;

namespace Primary.Common
{
    public readonly ref struct RentedArray<T> : IDisposable, IArrayIterator<T>, IEnumerable<T>
    {
        private readonly ArrayPool<T> _pool;
        private readonly int _count;
        private readonly T[] _array;
        private readonly bool _returnClear;

        internal RentedArray(int count, ArrayPool<T> pool, bool clearOnReturn = false)
        {
            _pool = pool;
            _count = count;
            _array = pool.Rent(count);
        }

        internal RentedArray(int count, bool clearOnReturn = false) : this(count, ArrayPool<T>.Shared, clearOnReturn) { }

        #region Interface
        public void Dispose()
        {
            if (_array != null)
                _pool.Return(_array, _returnClear);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ArraySegment<T>(_array, 0, _count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ArraySegment<T>(_array, 0, _count).GetEnumerator();
        }

        public T[] ToArray() => _array.ToArray();
        //TODO: cleanup and dont rely on external error testing
        public T[] ToArray(int start, int length) => _array.AsSpan().Slice(start, length).ToArray();
        #endregion

        #region Indexers
        T IArrayIterator<T>.this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public ref T this[int index]
        {
            get => ref _array[index];
        }

        public ref T DangerousGetReference()
        {
            return ref _array.DangerousGetReference();
        }

        public ref T DangerousGetReferenceAt(int i)
        {
            return ref _array.DangerousGetReferenceAt(i);
        }
        #endregion

        public ArrayPool<T> Pool => _pool;
        public int Count => _count;
        public Span<T> Span => _array.AsSpan(0, _count);
        public bool ClearOnReturn => _returnClear;

        public static RentedArray<T> Rent(int count, bool clearOnReturn = false) => new RentedArray<T>(count, clearOnReturn);
        public static RentedArray<T> Rent(int count, ArrayPool<T> pool, bool clearOnReturn = false) => new RentedArray<T>(count, pool, clearOnReturn);
    }
}
