namespace Primary.Interop
{
    public partial struct DEFRAGMENTATION_DESC
    {
        [NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")]
        public DEFRAGMENTATION_FLAGS Flags;

        [NativeTypeName("UINT64")]
        public ulong MaxBytesPerPass;

        [NativeTypeName("UINT32")]
        public uint MaxAllocationsPerPass;
    }
}
