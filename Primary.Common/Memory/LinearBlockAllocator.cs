using Primary.Common;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Primary.Common.Memory
{
    public unsafe sealed class LinearBlockAllocator : IDisposable
    {
        private byte* _block;
        private int _blockSize;

        private int _offset;

        private Queue<Ptr<byte>> _oldBlocks;

        private bool _disposedValue;

        public LinearBlockAllocator(int baseBlockSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(baseBlockSize, 1);

            _block = (byte*)NativeMemory.Alloc((nuint)baseBlockSize);
            _blockSize = baseBlockSize;

            _offset = 0;

            _oldBlocks = new Queue<Ptr<byte>>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_block != null)
                    NativeMemory.Free(_block);
                while (_oldBlocks.TryDequeue(out Ptr<byte> block))
                    NativeMemory.Free(block.Pointer);

                _block = null;
                _blockSize = 0;

                _offset = 0;

                _disposedValue = true;
            }
        }

        ~LinearBlockAllocator()
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

            while (_oldBlocks.TryDequeue(out Ptr<byte> block))
                NativeMemory.Free(block.Pointer);

            _offset = 0;
        }

        public nint Allocate(int size)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            int nextOffset = _offset + size;
            if (nextOffset > _blockSize)
            {
                ResizeInternalBlock(nextOffset);
                nextOffset = size;
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

            _oldBlocks.Enqueue(_block);

            _block = newPointer;
            _blockSize = (int)newSize;

            _offset = 0;
        }

        public nint Pointer => (nint)_block;
        public int CurrentOffset => _offset;
    }
}
