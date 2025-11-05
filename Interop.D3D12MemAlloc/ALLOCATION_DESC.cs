using TerraFX.Interop.DirectX;

namespace Interop.D3D12MemAlloc
{
    public unsafe partial struct ALLOCATION_DESC
    {
        [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")]
        public ALLOCATION_FLAGS Flags;

        public D3D12_HEAP_TYPE HeapType;

        public D3D12_HEAP_FLAGS ExtraHeapFlags;

        [NativeTypeName("D3D12MA::Pool *")]
        public Pool* CustomPool;

        public void* pPrivateData;
    }
}
