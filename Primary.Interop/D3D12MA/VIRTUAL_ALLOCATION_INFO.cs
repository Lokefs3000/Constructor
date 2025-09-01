namespace Primary.Interop
{
    public unsafe partial struct VIRTUAL_ALLOCATION_INFO
    {
        [NativeTypeName("UINT64")]
        public ulong Offset;

        [NativeTypeName("UINT64")]
        public ulong Size;

        public void* pPrivateData;
    }
}
