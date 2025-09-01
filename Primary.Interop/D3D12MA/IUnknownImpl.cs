using System.Runtime.CompilerServices;

namespace Primary.Interop
{
    [NativeTypeName("struct IUnknownImpl : IUnknown")]
    public unsafe partial struct IUnknownImpl
    {
        public void** lpVtbl;

        [NativeTypeName("atomic<UINT>")]
        private volatile uint m_RefCount;

        [return: NativeTypeName("HRESULT")]
        public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<IUnknownImpl*, Guid*, void**, int>)(lpVtbl[0]))((IUnknownImpl*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        [return: NativeTypeName("ULONG")]
        public uint AddRef()
        {
            return ((delegate* unmanaged[Stdcall]<IUnknownImpl*, uint>)(lpVtbl[1]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("ULONG")]
        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<IUnknownImpl*, uint>)(lpVtbl[2]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        public void Dispose()
        {
            ((delegate* unmanaged[Thiscall]<IUnknownImpl*, void>)(lpVtbl[3]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        public void ReleaseThis()
        {
            ((delegate* unmanaged[Thiscall]<IUnknownImpl*, void>)(lpVtbl[4]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }
    }
}
