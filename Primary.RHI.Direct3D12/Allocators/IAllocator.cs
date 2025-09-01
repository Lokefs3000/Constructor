namespace Primary.RHI.Direct3D12.Allocators
{
    internal interface IAllocator : IDisposable
    {
        public Allocation Allocate(int size);
        public void Free(ref Allocation allocation);

        public interface Allocation
        {
            public uint Value { get; }

            public bool IsNull { get; }
            public bool IsNotNull { get; }
        }
    }
}
