using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Primary.Common
{
    public unsafe struct Ptr<T> : IEquatable<Ptr<T>> where T : unmanaged
    {
        private T* _pointer;

        public Ptr()
        {
            _pointer = null;
        }

        public Ptr(T* @ref)
        {
            _pointer = @ref;
        }

        public Ptr<T2> As<T2>() where T2 : unmanaged
        {
            return (T2*)_pointer;
        }

        public readonly Span<T> AsSpan(int start, int length) => new Span<T>(_pointer + start, length);

        public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is Ptr<T> ptr && Equals(ptr);
        public readonly bool Equals(Ptr<T> other) => _pointer == other._pointer;

        public readonly ref T Ref => ref Unsafe.AsRef<T>(_pointer);
        public T* Pointer { readonly get => _pointer; set => _pointer = value; }

        public readonly bool IsNull => _pointer == null;

        public static readonly Ptr<T> Null = new Ptr<T>();

        public static bool operator ==(Ptr<T> lhs, Ptr<T> rhs) => lhs.Equals(rhs);
        public static bool operator !=(Ptr<T> lhs, Ptr<T> rhs) => !(lhs == rhs);

        public static implicit operator Ptr<T>(T* ptr) => new Ptr<T>(ptr);
        public static implicit operator T*(Ptr<T> ptr) => ptr.Pointer;
    }
}
