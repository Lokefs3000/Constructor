namespace Primary.Interop
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
