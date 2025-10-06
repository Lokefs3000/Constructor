using Primary.Common;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Memory
{
    public unsafe sealed class LinearAllocator : IDisposable
    {
        private readonly uint _blockSize = 0;
        private readonly uint _blockAlignment = 0;

        private byte** _blocks;
        private byte* _currentBlock;

        private uint* _blockSizes;

        private uint _blockIndex;
        private uint _blockArrayLength;
        private bool disposedValue;

        public LinearAllocator(uint blockSize, uint blockAlignment = 16u)
        {
            ExceptionUtility.Assert(BitOperations.IsPow2(blockSize));
            ExceptionUtility.Assert(BitOperations.IsPow2(blockAlignment));

            _blockSize = blockSize;
            _blockAlignment = blockAlignment;

            _blocks = (byte**)NativeMemory.Alloc(8u, (nuint)sizeof(byte*));
            _blockSizes = (uint*)NativeMemory.Alloc(8u, (nuint)sizeof(uint));
            _blockIndex = 0;
            _blockArrayLength = 8;

            _blocks[0] = (byte*)NativeMemory.AlignedAlloc(_blockSize, _blockAlignment);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                for (uint i = 0; i < _blockArrayLength; i++)
                    NativeMemory.Free(_blocks[i]);
                NativeMemory.Free(_blocks);

                disposedValue = true;
            }
        }

        ~LinearAllocator()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _blockIndex = 0;
            _currentBlock = _blocks[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* Allocate(uint width)
        {
            if (width > _blockSizes[_blockIndex])
                return InternalAllocateSlow(width);

            _currentBlock += width;
            return _currentBlock;
        }

        private byte* InternalAllocateSlow(uint width)
        {
            if (++_blockIndex == _blockArrayLength)
            {
                uint oldLength = _blockArrayLength;
                uint newLength = _blockArrayLength << 2;

                {
                    byte** newBlocks = (byte**)NativeMemory.Alloc(newLength, (nuint)sizeof(byte*));

                    NativeMemory.Copy(_blocks, newBlocks, oldLength);
                    NativeMemory.Free(_blocks);

                    _blocks = newBlocks;
                }

                {
                    uint* newSizes = (uint*)NativeMemory.Alloc(newLength, sizeof(uint));

                    NativeMemory.Copy(_blockSizes, newSizes, oldLength);
                    NativeMemory.Free(_blockSizes);

                    _blockSizes = newSizes;
                }

                _blockArrayLength = newLength;
            }

            uint blockSize = width > _blockSize ? BitOperations.RoundUpToPowerOf2(width) : _blockSize;
            byte* block = (byte*)NativeMemory.AlignedAlloc(blockSize, _blockAlignment);

            _blockSizes[_blockIndex] = blockSize;
            _blocks[++_blockIndex] = block;
            _currentBlock = block + width;

            return _currentBlock;
        }
    }
}
