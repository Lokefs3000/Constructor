namespace Interop.D3D12MemAlloc
{
    public partial struct Statistics
    {
        public uint BlockCount;

        public uint AllocationCount;

        [NativeTypeName("UINT64")]
        public ulong BlockBytes;

        [NativeTypeName("UINT64")]
        public ulong AllocationBytes;
    }
}
