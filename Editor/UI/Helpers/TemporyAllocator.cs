using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Helpers
{
    internal unsafe sealed class TemporyAllocator : IDisposable
    {
        private nint _currentBlock;
        private int _currentBlockSize;

        private int _currentBlockOffset;

        private Lock _lock;

        private bool _disposedValue;

        internal TemporyAllocator(int blockSize)
        {
            _currentBlock = nint.Zero;
            _currentBlockSize = blockSize;

            _currentBlockOffset = 0;

            _lock = new Lock();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_currentBlock != nint.Zero)
                    NativeMemory.Free(_currentBlock.ToPointer());
                _currentBlock = nint.Zero;

                _disposedValue = true;
            }
        }

        ~TemporyAllocator()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Reset()
        {
            _currentBlockOffset = 0;
        }

        internal void* Allocate(int size)
        {
            lock (_lock)
            {
                int nextBlockOffset = _currentBlockOffset + size;
                if (_currentBlock == nint.Zero || nextBlockOffset >= _currentBlockSize)
                {
                    do
                    {
                        _currentBlockSize = (int)BitOperations.RoundUpToPowerOf2((uint)(_currentBlockSize + 1));
                    } while (_currentBlockSize < size);

                    nint newMemoryBlock = (nint)NativeMemory.Alloc((nuint)_currentBlockSize);
                    if (_currentBlock != nint.Zero)
                    {
                        NativeMemory.Copy(_currentBlock.ToPointer(), newMemoryBlock.ToPointer(), (nuint)_currentBlockOffset);
                        NativeMemory.Free(_currentBlock.ToPointer());
                    }

                    _currentBlock = newMemoryBlock;
                }

                nint dataPtr = _currentBlock + _currentBlockOffset;
                _currentBlockOffset = nextBlockOffset;

                return dataPtr.ToPointer();
            }
        }

        internal void CopyTo(TemporyAllocator allocator)
        {
            if (allocator._currentBlock == nint.Zero || allocator._currentBlockOffset + _currentBlockOffset > allocator._currentBlockSize)
            {
                allocator._currentBlockSize = (int)BitOperations.RoundUpToPowerOf2((uint)(allocator._currentBlockOffset + _currentBlockOffset));

                nint newMemoryBlock = (nint)NativeMemory.Alloc((nuint)allocator._currentBlockSize);
                if (allocator._currentBlock != nint.Zero)
                {
                    NativeMemory.Copy(allocator._currentBlock.ToPointer(), newMemoryBlock.ToPointer(), (nuint)allocator._currentBlockOffset);
                    NativeMemory.Free(allocator._currentBlock.ToPointer());
                }

                allocator._currentBlock = newMemoryBlock;
            }

            if (_currentBlockOffset > 0)
            {
                NativeMemory.Copy(_currentBlock.ToPointer(), (allocator._currentBlock + allocator._currentBlockOffset).ToPointer(), (nuint)_currentBlockOffset);
                allocator._currentBlockOffset += _currentBlockOffset;
            }
        }

        public Span<byte> AsSpan()
        {
            if (_currentBlockOffset == 0 || _currentBlock == nint.Zero)
                return Span<byte>.Empty;
            return new Span<byte>(_currentBlock.ToPointer(), _currentBlockOffset);
        }

        public int ByteOffset => _currentBlockOffset;
    }
}
