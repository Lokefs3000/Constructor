namespace Interop.D3D12MemAlloc
{
    public partial struct DEFRAGMENTATION_STATS
    {
        [NativeTypeName("UINT64")]
        public ulong BytesMoved;

        [NativeTypeName("UINT64")]
        public ulong BytesFreed;

        [NativeTypeName("UINT32")]
        public uint AllocationsMoved;

        [NativeTypeName("UINT32")]
        public uint HeapsFreed;
    }
}
