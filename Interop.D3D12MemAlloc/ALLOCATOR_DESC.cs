using TerraFX.Interop.DirectX;

namespace Interop.D3D12MemAlloc
{
    public unsafe partial struct ALLOCATOR_DESC
    {
        [NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")]
        public ALLOCATOR_FLAGS Flags;

        public ID3D12Device* pDevice;

        [NativeTypeName("UINT64")]
        public ulong PreferredBlockSize;

        [NativeTypeName("const ALLOCATION_CALLBACKS *")]
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;

        public IDXGIAdapter* pAdapter;
    }
}
