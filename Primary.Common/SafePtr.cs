using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Common
{
    public unsafe struct SafePtr<T> : IDisposable, IEquatable<SafePtr<T>> where T : unmanaged
    {
        private T* _value;
        private bool _owner;

        public SafePtr(T* pointer, bool giveOwnership = false)
        {
            _value = pointer;
            _owner = giveOwnership;
        }

        public SafePtr()
        {
            _value = null;
            _owner = true;
        }

        public void Dispose()
        {
            if (_owner && _value != null)
            {
                NativeMemory.Free(_value);
                _value = null;
            }
        }

        public bool Equals(SafePtr<T> other) => other._value == _value;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is SafePtr<T> ptr && Equals(ptr);
        public override int GetHashCode() => HashCode.Combine(OpaquePointer, _owner);
        public override string ToString() => OpaquePointer.ToString("x16");

        public SafePtr<T> CreateOffseted(int offset, bool giveOwnership = false) => IsNull ? Null : new SafePtr<T>(_value + offset, giveOwnership);

        public T* Pointer => _value;
        public nint OpaquePointer => (nint)_value;
        public ref T Reference => ref Unsafe.AsRef<T>(_value);

        public bool OwnsPointer => _owner;
        public bool IsNull => _value == null;

        public static SafePtr<T> Allocate(int size = 0) => new SafePtr<T>((T*)NativeMemory.Alloc((nuint)(size <= 0 ? sizeof(T) : size)), true);
        public static SafePtr<T> AllocateZeroed(int size = 0) => new SafePtr<T>((T*)NativeMemory.AllocZeroed((nuint)(size <= 0 ? sizeof(T) : size)), true);

        public static void Copy<TSrc, TDst>(SafePtr<TSrc> src, SafePtr<TDst> dst) where TSrc : unmanaged where TDst : unmanaged
        {
            if (sizeof(TSrc) != sizeof(TDst))
                throw new ArgumentException("Source and destination pointers must have same size for copy");
            if (src.IsNull || dst.IsNull)
                throw new NullReferenceException("Source or destination pointers are null");

            NativeMemory.Copy(src._value, dst._value, (nuint)sizeof(T));
        }

        public static void CopyPartial<TSrc, TDst>(SafePtr<TSrc> src, SafePtr<TDst> dst, int size = 0) where TSrc : unmanaged where TDst : unmanaged
        {
            if (size == 0)
                throw new ArgumentException("Partial copy size cannot be zero");
            if (size > sizeof(TSrc) || size > sizeof(TDst))
                throw new ArgumentException("Partial copy size cannot be bigger then source and destination sizes");
            if (src.IsNull || dst.IsNull)
                throw new NullReferenceException("Source or destination pointers are null");

            NativeMemory.Copy(src._value, dst._value, (nuint)size);
        }

        public static SafePtr<TTo> Reinterpret<TFrom, TTo>(SafePtr<TFrom> from) where TFrom : unmanaged where TTo : unmanaged
        {
            if (sizeof(TFrom) != sizeof(TTo))
                throw new ArgumentException("Reinterpret requires from and to be of same size");
            return new SafePtr<TTo>((TTo*)from._value, from._owner);
        }

        public static readonly SafePtr<T> Null = new SafePtr<T>(null);
    }
}
