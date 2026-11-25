using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace EditorUI.Utility
{
    public ref struct SpanWriter
    {
        private Span<byte> _span;
        private int _offset;

        public SpanWriter(Span<byte> span)
        {
            _span = span;
            _offset = 0;
        }

        public void Write<T>(T value) where T : unmanaged
        {
            Debug.Assert(_offset + Unsafe.SizeOf<T>() <= _span.Length);
            Unsafe.WriteUnaligned(ref _span[_offset], value);

            _offset += Unsafe.SizeOf<T>();
        }

        public void WriteSpan<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            int byteSize = values.Length * Unsafe.SizeOf<T>();

            Debug.Assert(_offset + Unsafe.SizeOf<T>() * values.Length <= _span.Length);
            values.CopyTo(MemoryMarshal.Cast<byte, T>(_span.Slice(_offset, byteSize)));

            _offset += byteSize;
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
        public readonly unsafe nint SourceData => (nint)Unsafe.AsPointer(ref _span[0]);
    }
}
