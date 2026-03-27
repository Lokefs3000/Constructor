using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Helpers
{
    internal unsafe sealed class StringAllocator : IDisposable
    {
        private char* _currentBlock;

        private int _currentBlockOffset;
        private int _currentBlockLength;

        private int _allocatedBytes;

        private Queue<(nint, int)> _activeBlocks;

        private bool _disposedValue;

        internal StringAllocator(int startBlockSize)
        {
            _currentBlock = (char*)(startBlockSize > 0 ? NativeMemory.Alloc((nuint)startBlockSize, sizeof(char)) : null);

            _currentBlockOffset = 0;
            _currentBlockLength = 0;

            _allocatedBytes = 0;

            _activeBlocks = new Queue<(nint, int)>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                while (_activeBlocks.TryDequeue(out ValueTuple<nint, int> result))
                    NativeMemory.Free(result.Item1.ToPointer());

                if (_currentBlock != null)
                    NativeMemory.Free(_currentBlock);
                _currentBlock = null;

                _disposedValue = true;
            }
        }

        ~StringAllocator()
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
            while (_activeBlocks.TryDequeue(out ValueTuple<nint, int> result))
                    NativeMemory.Free(result.Item1.ToPointer());

            _currentBlockOffset = 0;
            _allocatedBytes = 0;
        }

        internal void CopyTo(StringAllocator strings)
        {
            if (@strings._currentBlockOffset + _allocatedBytes > strings._currentBlockLength)
            {
                int oldLength = strings._currentBlockOffset;

                strings._currentBlockLength = (int)BitOperations.RoundUpToPowerOf2((uint)(strings._currentBlockOffset + _allocatedBytes));
                strings._currentBlockOffset = 0;

                strings._activeBlocks.Enqueue(((nint)strings._currentBlock, oldLength));
                strings._currentBlock = (char*)NativeMemory.Alloc((nuint)strings._currentBlockLength, sizeof(char));
            }

            int offset = 0;
            if (_activeBlocks.Count > 0)
            {
                foreach ((nint ptr, int length) in _activeBlocks)
                {
                    if (length > 0)
                    {
                        NativeMemory.Copy(ptr.ToPointer(), strings._currentBlock + offset, (uint)length * 2);
                        offset += length;
                    }
                }
            }

            if (_currentBlockOffset > 0)
            {
                NativeMemory.Copy(_currentBlock, strings._currentBlock + offset, (uint)_currentBlockOffset * 2);
            }

            strings._currentBlockOffset += _allocatedBytes;
        }

        private void* AllocateBytes(int length)
        {
            if (_currentBlockOffset + length > _currentBlockLength)
            {
                int oldLength = _currentBlockOffset;

                _currentBlockLength = (int)BitOperations.RoundUpToPowerOf2((uint)(_currentBlockOffset + length));
                _currentBlockOffset = 0;

                _activeBlocks.Enqueue(((nint)_currentBlock, oldLength));
                _currentBlock = (char*)NativeMemory.Alloc((nuint)_currentBlockLength, sizeof(char));
            }

            char* ptr = _currentBlock + _currentBlockOffset;

            _currentBlockOffset += length;
            _allocatedBytes += length;

            return ptr;
        }

        internal StringHandle Allocate(ReadOnlySpan<char> @string, int hash)
        {
            char* ptr = (char*)AllocateBytes(@string.Length);
            StringHandle handle = new StringHandle(ptr, @string.Length, hash);

            @string.CopyTo(handle.String);
            return handle;
        }
    }

    public unsafe readonly struct StringHandle
    {
        private readonly char* _handle;
        private readonly int _length;

        private readonly int _hash;

        public StringHandle(char* handle, int length, int hash)
        {
            _handle = handle;
            _length = length;

            _hash = hash;
        }

        public Span<char> String => new Span<char>(_handle, _length);

        public int Hash => _hash;
        public nint Pointer => (nint)_handle;

        public const int InvalidHashCode = int.MinValue;
    }
}
