namespace Interop.D3D12MemAlloc
{
    public unsafe partial struct ALLOCATION_CALLBACKS
    {
        [NativeTypeName("D3D12MA::ALLOCATE_FUNC_PTR")]
        public delegate* unmanaged[Cdecl]<nuint, nuint, void*, void*> pAllocate;

        [NativeTypeName("D3D12MA::FREE_FUNC_PTR")]
        public delegate* unmanaged[Cdecl]<void*, void*, void> pFree;

        public void* pPrivateData;
    }
}
