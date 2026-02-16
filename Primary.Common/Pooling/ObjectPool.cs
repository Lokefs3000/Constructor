using System.Runtime.CompilerServices;

namespace Primary.Pooling
{
    public sealed class ObjectPool<T> : IObjectPool<T> where T : notnull
    {
        private readonly int _maxSize;
        private readonly IObjectPoolPolicy<T> _policy;

        private T[] _pool;
        private int _count;

        public ObjectPool(IObjectPoolPolicy<T> policy, int maxSize = int.MaxValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxSize, 1);

            _pool = maxSize == int.MaxValue ? Array.Empty<T>() : new T[maxSize];
            _maxSize = maxSize;

            _policy = policy;

            _count = 0;
        }

        public T Get() //inline?
        {
            if (_count > 0)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    int idx = --_count;
                    T val = _pool[idx];

                    Array.Clear(_pool, idx, 1);
                    return val;
                }
                else
                    return _pool[--_count];
            }
            else
                return _policy.Create();
        }

        public void Return(T value) //inline?
        {
            ArgumentNullException.ThrowIfNull(value);

            if (_count == _maxSize)
                throw new InvalidOperationException("Max size reached");

            if (_policy.Return(ref value))
            {
                if (_pool.Length <= _count)
                    Array.Resize(ref _pool, Math.Max(_pool.Length, 1) * 2);

                _pool[_count++] = value;
            }
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Array.Clear(_pool, 0, _count);

            _count = 0;
        }

        public void TrimExcess()
        {
            //mimicks Stack<T>.TrimExcess()
            int threshold = (int)(_pool.Length * 0.9);
            if (_count < threshold)
            {
                if (_count == 0)
                    _pool = Array.Empty<T>();
                else
                    Array.Resize(ref _pool, _count);
            }
        }

        public int Count => _count;
        public int Capacity => _pool.Length;
    }
}
