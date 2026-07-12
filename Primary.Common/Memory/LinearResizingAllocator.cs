using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Common.Memory
{
    public unsafe sealed class LinearResizingAllocator : IDisposable
    {
        private byte* _block;
        private int _blockSize;

        private int _offset;

        private bool _disposedValue;

        public LinearResizingAllocator(int baseBlockSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(baseBlockSize, 1);

            _block = (byte*)NativeMemory.Alloc((nuint)baseBlockSize);
            _blockSize = baseBlockSize;

            _offset = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_block != null)
                    NativeMemory.Free(_block);

                _block = null;
                _blockSize = 0;

                _offset = 0;

                _disposedValue = true;
            }
        }

        ~LinearResizingAllocator()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            _offset = 0;
        }

        public nint Allocate(int size)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            int nextOffset = _offset + size;
            if (nextOffset > _blockSize)
            {
                ResizeInternalBlock(nextOffset);
            }

            nint ptr = (nint)(_block + _offset);

            _offset = nextOffset;
            return ptr;
        }

        private void ResizeInternalBlock(int minimumSize)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            uint newSize = BitOperations.RoundUpToPowerOf2((uint)minimumSize);
            byte* newPointer = (byte*)NativeMemory.Alloc(newSize);

            NativeMemory.Copy(_block, newPointer, (nuint)_offset);
            NativeMemory.Free(_block);

            _block = newPointer;
            _blockSize = (int)newSize;
        }

        public nint Pointer => (nint)_block;
        public int CurrentOffset { get => _offset; set => _offset = value; }
    }
}
