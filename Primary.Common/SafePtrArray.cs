using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Common
{
    public unsafe struct SafePtrArray<T> : IDisposable, IEquatable<SafePtrArray<T>> where T : unmanaged
    {
        private T* _value;
        private int _length;
        private bool _owner;

        public SafePtrArray(T* array, int length, bool giveOwnership = false)
        {
            _value = array;
            _length = length;
            _owner = giveOwnership;
        }

        public SafePtrArray()
        {
            _value = null;
            _length = 0;
            _owner = true;
        }

        public void Dispose()
        {
            if (_owner && _value != null)
            {
                NativeMemory.Free(_value);
                _value = null;
            }

            _length = 0;
        }

        public ref T this[int index]
        {
            get
            {
                if (_value == null)
                    throw new NullReferenceException(nameof(_value));
                if ((uint)index >= _length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return ref _value[index];
            }
        }

        public bool Equals(SafePtrArray<T> other) => other._value == _value;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is SafePtrArray<T> ptr && Equals(ptr);
        public override int GetHashCode() => HashCode.Combine(OpaquePointer, _length, _owner);
        public override string ToString() => $"{OpaquePointer.ToString("x16")}[{_length}]";

        public T* Pointer => _value;
        public nint OpaquePointer => (nint)_value;
        public ref T Reference => ref Unsafe.AsRef<T>(_value);

        public int Length => _length;

        public bool OwnsPointer => _owner;
        public bool IsNull => _value == null;

        public static SafePtrArray<T> Allocate(int length) => length == 0 ? Null : new SafePtrArray<T>((T*)NativeMemory.Alloc((nuint)length, (nuint)sizeof(T)), length, true);
        public static SafePtrArray<T> AllocateZeroed(int length) => length == 0 ? Null : new SafePtrArray<T>((T*)NativeMemory.AllocZeroed((nuint)length, (nuint)sizeof(T)), length, true);

        public static void Copy(SafePtrArray<T> src, SafePtrArray<T> dst, int length = 0)
        {
            if (length > src._length || length > dst._length)
                throw new ArgumentException("Length is longer than source or copy");
            else if (length < 0)
                length = Math.Min(src._length, dst._length);
            if (src.IsNull || dst.IsNull)
                throw new NullReferenceException("Source or destination pointers are null");

            NativeMemory.Copy(src._value, dst._value, (nuint)sizeof(T) * (nuint)length);
        }

        public static void Copy(SafePtrArray<T> src, int srcOffset, SafePtrArray<T> dst, int dstOffset, int length = 0)
        {
            if (length > src._length || length > dst._length)
                throw new ArgumentException("Length is longer than source or copy");
            else if (length < 0)
                length = Math.Min(src._length, dst._length);
            if ((uint)srcOffset > src._length || srcOffset + length > src._length)
                throw new ArgumentException("Source offset is too large");
            if ((uint)dstOffset > dst._length || dstOffset + length > dst._length)
                throw new ArgumentException("Source offset is too large");
            if (src.IsNull || dst.IsNull)
                throw new NullReferenceException("Source or destination pointers are null");

            NativeMemory.Copy(src._value + srcOffset, dst._value + dstOffset, (nuint)sizeof(T) * (nuint)length);
        }

        public static void Fill(SafePtrArray<T> array, int length = -1, byte value = default)
        {
            if (length > array._length)
                throw new ArgumentException("Length is larger then array");
            else if (length < 0)
                length = array._length;

            NativeMemory.Fill(array._value, (nuint)(length * sizeof(T)), value);
        }

        public static SafePtrArray<TTo> Reinterpret<TFrom, TTo>(SafePtrArray<TFrom> from) where TFrom : unmanaged where TTo : unmanaged
        {
            if (sizeof(TFrom) != sizeof(TTo))
                throw new ArgumentException("Reinterpret requires from and to be of same size");
            return new SafePtrArray<TTo>((TTo*)from._value, from._length, from._owner);
        }

        public static readonly SafePtrArray<T> Null = new SafePtrArray<T>(null, 0);
    }
}
