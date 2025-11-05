using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Pooling
{
    public sealed class DisposableObjectPool<T> : IDisposable, IObjectPool<T> where T : notnull, IDisposable
    {
        private readonly int _maxSize;
        private readonly IObjectPoolPolicy<T> _policy;

        private T[] _pool;
        private int _count;

        private bool _disposedValue;

        public DisposableObjectPool(IObjectPoolPolicy<T> policy, int maxSize = int.MaxValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxSize, 1);

            _pool = maxSize == int.MaxValue ? Array.Empty<T>() : new T[maxSize];
            _maxSize = maxSize;

            _policy = policy;

            _count = 0;
        }

        public T Get() //inline?
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

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
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            ArgumentNullException.ThrowIfNull(value);

            if (_count == _maxSize)
                throw new InvalidOperationException("Max size reached");

            if (_policy.Return(ref value))
            {
                if (_pool.Length <= _count)
                    Array.Resize(ref _pool, Math.Max(_pool.Length, 1) * 2);

                _pool[_count++] = value;
            }
            else
            {
                value.Dispose();
            }
        }

        public void Clear()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Array.Clear(_pool, 0, _count);

            _count = 0;
        }

        public void TrimExcess()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

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

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        _pool[i].Dispose();
                    }

                    _pool = Array.Empty<T>();
                    _count = 0;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
