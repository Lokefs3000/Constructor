using Vortice.Direct3D12;

namespace Primary.Interop
{
    public unsafe partial struct POOL_DESC
    {
        [NativeTypeName("D3D12MA::POOL_FLAGS")]
        public POOL_FLAGS Flags;

        public HeapProperties HeapProperties;

        public HeapFlags HeapFlags;

        [NativeTypeName("UINT64")]
        public ulong BlockSize;

        public uint MinBlockCount;

        public uint MaxBlockCount;

        [NativeTypeName("UINT64")]
        public ulong MinAllocationAlignment;

        public ID3D12ProtectedResourceSession* pProtectedSession;

        public ResidencyPriority ResidencyPriority;
    }
}
