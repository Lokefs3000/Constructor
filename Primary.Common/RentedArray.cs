using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return new Enumerator(_array, _count, 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
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
        public Span<T> Span => _array;
        public bool ClearOnReturn => _returnClear;

        public static RentedArray<T> Rent(int count, bool clearOnReturn = false) => new RentedArray<T>(count, clearOnReturn);
        public static RentedArray<T> Rent(int count, ArrayPool<T> pool, bool clearOnReturn = false) => new RentedArray<T>(count, pool, clearOnReturn);

        private struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private int _count;
            private int _index;

            public Enumerator(T[] array, int count, int index)
            {
                _array = array;
                _count = count;
                _index = index;
            }

            public void Dispose()
            {
                _count = 0;
            }

            public bool MoveNext()
            {
                _index++;
                return _index >= _count;
            }

            public void Reset()
            {
                _index = 0;
            }

            public T Current => _array[_index];
            object IEnumerator.Current => Current;
        }
    }
}
