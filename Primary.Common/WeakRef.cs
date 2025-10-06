using System.Runtime.CompilerServices;

namespace Primary.Common
{
    public unsafe struct WeakRef<T> where T : unmanaged
    {
        private T* _ref;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeakRef()
        {
            _ref = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeakRef(ref T @ref)
        {
            _ref = (T*)Unsafe.AsPointer(ref @ref);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeakRef(T* @ref)
        {
            _ref = @ref;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WeakRef(Ptr<T> ptr)
        {
            _ref = ptr.Pointer;
        }

        public ref T Ref => ref Unsafe.AsRef<T>(_ref);
        public T* PointerRef => _ref;

        public bool IsNull => _ref == null;

        public static readonly WeakRef<T> Null = new WeakRef<T>();

        public static implicit operator WeakRef<T>(Ptr<T> ptr) => new WeakRef<T>(ptr);
    }
}
