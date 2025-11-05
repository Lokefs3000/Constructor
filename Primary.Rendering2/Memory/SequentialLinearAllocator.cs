using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Memory
{
    internal unsafe sealed class SequentialLinearAllocator : IDisposable
    {
        private byte* _block;
        private int _blockSize;

        private int _offset;

        private bool _disposedValue;

        internal SequentialLinearAllocator(int baseBlockSize)
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

        ~SequentialLinearAllocator()
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
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            _offset = 0;
        }

        internal nint Allocate(int size)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            int nextOffset = _offset + size;
            if (nextOffset >= _blockSize)
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

            Debug.Assert(minimumSize > _blockSize);

            uint newSize = BitOperations.RoundUpToPowerOf2((uint)minimumSize);
            byte* newPointer = (byte*)NativeMemory.Alloc(newSize);

            if (_offset > 0)
                NativeMemory.Copy(_block, newPointer, (nuint)_offset);

            NativeMemory.Free(_block);
            _block = newPointer;
        }

        public nint Pointer => (nint)_block;
        public int CurrentOffset => _offset;
    }
}
