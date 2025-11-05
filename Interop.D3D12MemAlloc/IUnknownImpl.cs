using System;
using System.Runtime.CompilerServices;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct IUnknownImpl : IUnknown")]
    public unsafe partial struct IUnknownImpl
    {
        public Vtbl* lpVtbl;

        [NativeTypeName("atomic<UINT>")]
        private volatile uint m_RefCount;

        [return: NativeTypeName("HRESULT")]
        public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
        {
            return lpVtbl->QueryInterface((IUnknownImpl*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        [return: NativeTypeName("ULONG")]
        public uint AddRef()
        {
            return lpVtbl->AddRef((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("ULONG")]
        public uint Release()
        {
            return lpVtbl->Release((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        public void Dispose()
        {
            lpVtbl->Dispose((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        public void ReleaseThis()
        {
            lpVtbl->ReleaseThis((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        public partial struct Vtbl
        {
            [NativeTypeName("HRESULT (const IID &, void **) __attribute__((stdcall))")]
            public delegate* unmanaged[Stdcall]<IUnknownImpl*, Guid*, void**, int> QueryInterface;

            [NativeTypeName("ULONG () __attribute__((stdcall))")]
            public delegate* unmanaged[Stdcall]<IUnknownImpl*, uint> AddRef;

            [NativeTypeName("ULONG () __attribute__((stdcall))")]
            public delegate* unmanaged[Stdcall]<IUnknownImpl*, uint> Release;

            [NativeTypeName("void () noexcept")]
            public delegate* unmanaged[Thiscall]<IUnknownImpl*, void> Dispose;

            [NativeTypeName("void ()")]
            public delegate* unmanaged[Thiscall]<IUnknownImpl*, void> ReleaseThis;
        }
    }
}
