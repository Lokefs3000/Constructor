using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Memory.Native
{
    public unsafe static class ScopedMemory
    {
        private static ConcurrentDictionary<int, ThreadScope> _threads;

        static ScopedMemory()
        {
            _threads = new ConcurrentDictionary<int, ThreadScope>();
        }

        private static ThreadScope GetThreadScope() => _threads.GetOrAdd(Thread.CurrentThread.ManagedThreadId, (_) => new ThreadScope());

        public static MemoryScope PushScope()
        {
            ThreadScope scope = GetThreadScope();
            scope.PushScope();

            return new MemoryScope(scope);
        }

        #region Generic
        public static ScopedPtr<byte> Allocate(nuint byteCount)
        {
            byte* ptr = (byte*)NativeMemory.Alloc(byteCount);
            GetThreadScope().AddMemoryToScope((nint)ptr, false);
            return new ScopedPtr<byte>(ptr);
        }

        public static ScopedPtr<byte> Reallocate(ScopedPtr<byte> pointer, nuint byteCount)
        {
            byte* ptr = (byte*)NativeMemory.Realloc(pointer.Pointer, byteCount);
            GetThreadScope().AddMemoryToScope((nint)ptr, false);
            return new ScopedPtr<byte>(ptr);
        }

        public static ScopedPtr<byte> Allocate(nuint byteCount, nuint alignment)
        {
            byte* ptr = (byte*)NativeMemory.AlignedAlloc(byteCount, alignment);
            GetThreadScope().AddMemoryToScope((nint)ptr, true);
            return new ScopedPtr<byte>(ptr);
        }

        public static ScopedPtr<byte> Reallocate(ScopedPtr<byte> pointer, nuint byteCount, nuint alignment)
        {
            byte* ptr = (byte*)NativeMemory.AlignedRealloc(pointer.Pointer, byteCount, alignment);
            GetThreadScope().AddMemoryToScope((nint)ptr, true);
            return new ScopedPtr<byte>(ptr);
        }
        #endregion
        #region Templated
        public static ScopedPtr<T> Allocate<T>(nuint elementCount = 1) where T : unmanaged
            => Allocate(elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>()).As<T>();
        public static ScopedPtr<T> Reallocate<T>(ScopedPtr<T> pointer, nuint elementCount = 1) where T : unmanaged
            => Reallocate(pointer.As<byte>(), elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>()).As<T>();
        public static ScopedPtr<T> Allocate<T>(nuint alignment, nuint elementCount = 1) where T : unmanaged
            => Allocate(elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>(), alignment).As<T>();
        public static ScopedPtr<T> Reallocate<T>(ScopedPtr<byte> pointer, nuint alignment, nuint elementCount = 1) where T : unmanaged
            => Reallocate(pointer.As<byte>(), elementCount == 1 ? (nuint)Unsafe.SizeOf<T>() : elementCount * (nuint)Unsafe.SizeOf<T>(), alignment).As<T>();
        #endregion

        internal sealed class ThreadScope
        {
            private List<MemoryData> _pointers;
            private Stack<ScopeData> _scopes;

            internal ThreadScope()
            {
                _pointers = new List<MemoryData>();
                _scopes = new Stack<ScopeData>();
            }

            internal void PushScope() => _scopes.Push(new ScopeData(_pointers.Count));
            internal bool PopScope()
            {
                if (_scopes.TryPop(out ScopeData data))
                {
                    while (_pointers.Count > data.PointerStart)
                    {
                        int idx = _pointers.Count - 1;
                        MemoryData memory = _pointers[idx];

                        if (memory.IsAligned)
                            NativeMemory.AlignedFree(memory.Pointer.ToPointer());
                        else
                            NativeMemory.Free(memory.Pointer.ToPointer());

                        _pointers.RemoveAt(idx);
                    }

                    return true;
                }

                return false;
            }

            internal void AddMemoryToScope(nint pointer, bool isAligned) => _pointers.Add(new MemoryData(pointer, isAligned));

            internal bool IsEmpty => _scopes.Count == 0;

            private readonly record struct MemoryData(nint Pointer, bool IsAligned);
            private readonly record struct ScopeData(int PointerStart);
        }
    }

    public ref struct MemoryScope : IDisposable
    {
        private readonly ScopedMemory.ThreadScope _scope;
        private bool _isDisposed;

        internal MemoryScope(ScopedMemory.ThreadScope scope)
        {
            _scope = scope;
            _isDisposed = false;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _scope.PopScope();
                _isDisposed = false;
            }
        }
    }
}
