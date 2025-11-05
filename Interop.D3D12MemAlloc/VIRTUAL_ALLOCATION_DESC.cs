namespace Interop.D3D12MemAlloc
{
    public unsafe partial struct VIRTUAL_ALLOCATION_DESC
    {
        [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")]
        public VIRTUAL_ALLOCATION_FLAGS Flags;

        [NativeTypeName("UINT64")]
        public ulong Size;

        [NativeTypeName("UINT64")]
        public ulong Alignment;

        public void* pPrivateData;
    }
}
