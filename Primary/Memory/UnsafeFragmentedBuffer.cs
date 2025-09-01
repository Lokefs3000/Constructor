using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Memory
{
    public unsafe sealed class UnsafeFragmentedBuffer<T> : IDisposable where T : unmanaged
    {
        private T* _buffer;

        private uint _capacity;
        private uint _used;

        private uint _startIndex;
        private uint _endIndex;

        private SortedSet<uint> _availablePositions;
        private HashSet<uint> _usedPositions;

        private bool _disposedValue;

        internal UnsafeFragmentedBuffer(uint startSize = 8)
        {
            ExceptionUtility.Assert(startSize > 0);

            _buffer = (T*)NativeMemory.Alloc((nuint)(startSize * sizeof(T)));

            _capacity = startSize;
            _used = 0;

            _startIndex = 0;
            _endIndex = 0;

            _availablePositions = new SortedSet<uint>();
            _usedPositions = new HashSet<uint>();

            for (uint i = 0; i < startSize; i++)
            {
                _availablePositions.Add(i);
            }
        }

        internal void Resize(uint newSize, bool preserveContents)
        {
            if (newSize == _capacity)
                return;

            ExceptionUtility.Assert(newSize > 0);

            if (preserveContents)
            {
                T* newPointer = _buffer = (T*)NativeMemory.Alloc((nuint)(newSize * (ulong)sizeof(T)));

                NativeMemory.Copy(_buffer + _startIndex, newPointer + _startIndex, (nuint)(_used * (ulong)sizeof(T)));
                NativeMemory.Free(_buffer);

                _buffer = newPointer;
            }
            else
            {
                NativeMemory.Free(_buffer);
                _buffer = (T*)NativeMemory.Alloc((nuint)(newSize * (ulong)sizeof(T)));
            }

            if (newSize > _capacity)
            {
                for (uint i = _capacity; i < newSize; i++)
                {
                    _availablePositions.Add(i);
                }
            }
            else
            {
                //verify it works
                for (uint i = _capacity - 1; i > newSize; i--)
                {
                    _availablePositions.Remove(i);
                }
            }

                _capacity = newSize;
        }

        internal void Clear()
        {
            _startIndex = 0;
            _endIndex = 0;

            _used = 0;

            _availablePositions.Clear();
            _usedPositions.Clear();

            for (uint i = 0; i < _capacity; i++)
            {
                _availablePositions.Add(i);
            }
        }

        internal uint Add(T value)
        {
            if (_used >= _capacity)
                return uint.MaxValue;

            uint position = GetAvailablePosition();

            if (_used == 0)
            {
                _startIndex = position;
                _endIndex = position + 1;
            }
            else
            {
                _startIndex = Math.Min(_startIndex, position);
                _endIndex = Math.Max(_endIndex, position + 1);
            }

            _used++;
            return uint.MaxValue;
        }

        internal void Remove(uint position)
        {
            Debug.Assert(position < _capacity);
            Debug.Assert(_used > 0);

            if (position < _startIndex || position > _endIndex || _used == 0)
                return;

            _availablePositions.Add(position);
            _usedPositions.Remove(position);

            if (position == _startIndex)
            {
                do
                {
                    position++;
                } while (position < _endIndex && !_usedPositions.Contains(position));
            }
            else if (position == _endIndex)
            {
                do
                {
                    position--;
                } while (position > _startIndex && !_usedPositions.Contains(position));
            }

            _used--;
        }

        private uint GetAvailablePosition()
        {
            if (_availablePositions.Count == 0)
                return uint.MaxValue;

            uint position = _availablePositions.First();

            _availablePositions.Remove(position);
            _usedPositions.Add(position);

            return position;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                NativeMemory.Free(_buffer);

                _disposedValue = true;
            }
        }

        ~UnsafeFragmentedBuffer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal bool IsEmpty => _used == 0;

        internal uint Count => _used;
        internal uint Capacity => _capacity;
    }
}
