namespace Interop.D3D12MemAlloc
{
    public partial struct DetailedStatistics
    {
        [NativeTypeName("D3D12MA::Statistics")]
        public Statistics Stats;

        public uint UnusedRangeCount;

        [NativeTypeName("UINT64")]
        public ulong AllocationSizeMin;

        [NativeTypeName("UINT64")]
        public ulong AllocationSizeMax;

        [NativeTypeName("UINT64")]
        public ulong UnusedRangeSizeMin;

        [NativeTypeName("UINT64")]
        public ulong UnusedRangeSizeMax;
    }
}
