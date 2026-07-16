using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI.Utility;
using Primary.Common.Memory;

namespace EditorUI.Memory
{
    public sealed class TemporaryAllocator : IDisposable
    {
        private LinearBlockAllocator _allocator;

        private bool _disposedValue;

        public TemporaryAllocator(int baseSize)
        {
            _allocator = new LinearBlockAllocator(baseSize);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void ResetAllocationOffset()
        {
            _allocator.Reset();
        }

        public unsafe SpanWriter GetWritableSpan(int size)
        {
            void* ptr = (void*)_allocator.Allocate(size);
            return new SpanWriter(new Span<byte>(ptr, size));
        }

        public unsafe void WriteInto<T>(in T value) where T : unmanaged
        {
            T* ptr = (T*)_allocator.Allocate(Unsafe.SizeOf<T>());
            *ptr = value;
        }

        public unsafe nint GetNativePointer(int size)
        {
            void* ptr = (void*)_allocator.Allocate(size);
            return (nint)ptr;
        }

        public unsafe Span<byte> AsSpan() => new Span<byte>(_allocator.Pointer.ToPointer(), _allocator.CurrentOffset);

        public int CurrentDataOffset => _allocator.CurrentOffset;
    }
}
