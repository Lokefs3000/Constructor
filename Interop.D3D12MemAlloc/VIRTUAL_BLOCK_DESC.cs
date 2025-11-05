namespace Interop.D3D12MemAlloc
{
    public unsafe partial struct VIRTUAL_BLOCK_DESC
    {
        [NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")]
        public VIRTUAL_BLOCK_FLAGS Flags;

        [NativeTypeName("UINT64")]
        public ulong Size;

        [NativeTypeName("const ALLOCATION_CALLBACKS *")]
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;
    }
}
