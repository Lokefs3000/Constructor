using System.Runtime.CompilerServices;

namespace Primary.Common
{
    public unsafe struct Ptr<T> where T : unmanaged
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

        public Span<T> AsSpan(int start, int length) => new Span<T>(_pointer + start, length);

        public ref T Ref => ref Unsafe.AsRef<T>(_pointer);
        public T* Pointer { get => _pointer; set => _pointer = value; }

        public bool IsNull => _pointer == null;

        public static readonly Ptr<T> Null = new Ptr<T>();

        public static implicit operator Ptr<T>(T* ptr) => new Ptr<T>(ptr);
    }
}
