using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace EditorUI.Utility
{
    internal ref struct SpanReader
    {
        private Span<byte> _span;
        private int _offset;

        public SpanReader(Span<byte> span)
        {
            _span = span;
            _offset = 0;
        }

        public readonly T Read<T>(int offset) where T : unmanaged
        {
            Debug.Assert(offset + Unsafe.SizeOf<T>() <= _span.Length);
            return Unsafe.ReadUnaligned<T>(in _span[offset]);
        }

        public readonly ReadOnlySpan<T> ReadSpan<T>(int offset, int length) where T : unmanaged
        {
            Debug.Assert(offset + Unsafe.SizeOf<T>() * length <= _span.Length);
            return MemoryMarshal.Cast<byte, T>(_span.Slice(offset, Unsafe.SizeOf<T>() * length));
        }

        public ref T Get<T>() where T : unmanaged
        {
            Debug.Assert(_offset + Unsafe.SizeOf<T>() <= _span.Length);
            ref T val = ref Unsafe.As<byte, T>(ref _span[_offset]);

            _offset += Unsafe.SizeOf<T>();
            return ref val;
        }

        public ref T Get<T>(int length) where T : unmanaged
        {
            int byteSize = length * Unsafe.SizeOf<T>();

            Debug.Assert(_offset + byteSize <= _span.Length);
            ref T val = ref Unsafe.As<byte, T>(ref _span[_offset]);

            _offset += byteSize;
            return ref val;
        }

        public readonly int Length => _span.Length;
        public readonly bool HasReadAllData => _offset >= _span.Length;
    }
}
