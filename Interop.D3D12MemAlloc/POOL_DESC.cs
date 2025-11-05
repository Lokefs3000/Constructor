using TerraFX.Interop.DirectX;

namespace Interop.D3D12MemAlloc
{
    public unsafe partial struct POOL_DESC
    {
        [NativeTypeName("D3D12MA::POOL_FLAGS")]
        public POOL_FLAGS Flags;

        public D3D12_HEAP_PROPERTIES HeapProperties;

        public D3D12_HEAP_FLAGS HeapFlags;

        [NativeTypeName("UINT64")]
        public ulong BlockSize;

        public uint MinBlockCount;

        public uint MaxBlockCount;

        [NativeTypeName("UINT64")]
        public ulong MinAllocationAlignment;

        public ID3D12ProtectedResourceSession* pProtectedSession;

        public D3D12_RESIDENCY_PRIORITY ResidencyPriority;
    }
}
