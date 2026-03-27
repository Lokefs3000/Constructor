using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Memory.Native
{
    public unsafe struct ScopedPtr<T> where T : unmanaged
    {
        private T* _pointer;

        public ScopedPtr()
        {
            _pointer = null;
        }

        internal ScopedPtr(T* pointer)
        {
            _pointer = pointer;
        }

        public ScopedPtr<TTo> As<TTo>() where TTo : unmanaged
        {
            return new ScopedPtr<TTo>((TTo*)_pointer);
        }

        public Span<T> AsSpan(int count) => new Span<T>(_pointer, count);
        public Span<T> AsSpan(int start, int count) => new Span<T>(_pointer + start, count);

        #region Generic
        public void ClearBytes(nuint byteCount) => NativeMemory.Clear(_pointer, byteCount);
        public void FillBytes(nuint byteCount, byte value) => NativeMemory.Fill(_pointer, byteCount, value);

        public void CopyBytes(ScopedPtr<T> destination, nuint byteCount) => NativeMemory.Copy(_pointer, destination._pointer, byteCount);
        public void CopyBytes(T* destination, nuint byteCount) => NativeMemory.Copy(_pointer, destination, byteCount);
        public void CopyBytes(byte* destination, nuint byteCount) => NativeMemory.Copy(_pointer, destination, byteCount);
        #endregion
        #region Templated
        public void Clear(nuint elementCount = 1) => NativeMemory.Clear(_pointer, elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>());
        public void Fill(byte value, nuint elementCount = 1) => NativeMemory.Fill(_pointer, elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>(), value);

        public void Copy(ScopedPtr<T> destination, nuint elementCount = 1) => NativeMemory.Copy(_pointer, destination._pointer, elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>());
        public void Copy(T* destination, nuint elementCount = 1) => NativeMemory.Copy(_pointer, destination, elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>());
        public void Copy(byte* destination, nuint elementCount = 1) => NativeMemory.Copy(_pointer, destination, elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>());
        #endregion

        public ref T this[int elementOffset]
        {
            get => ref _pointer[elementOffset];
        }

        public ref T this[nuint elementOffset]
        {
            get => ref _pointer[elementOffset];
        }

        public T* Pointer => _pointer;
        public ref T Reference => ref Unsafe.AsRef<T>(_pointer);

        public bool IsNull => _pointer == null;

        public static readonly ScopedPtr<T> Null = new ScopedPtr<T>();
    }
}
