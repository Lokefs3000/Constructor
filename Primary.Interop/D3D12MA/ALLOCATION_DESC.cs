using Vortice.Direct3D12;

namespace Primary.Interop
{
    public unsafe partial struct ALLOCATION_DESC
    {
        [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")]
        public ALLOCATION_FLAGS Flags;

        public HeapType HeapType;

        public HeapFlags ExtraHeapFlags;

        [NativeTypeName("D3D12MA::Pool *")]
        public Pool* CustomPool;

        public void* pPrivateData;
    }
}
