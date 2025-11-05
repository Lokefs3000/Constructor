using TerraFX.Interop.DirectX;
using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct CALLOCATION_DESC : D3D12MA::ALLOCATION_DESC")]
    public unsafe partial struct CALLOCATION_DESC
    {
        public ALLOCATION_DESC Base;

        public CALLOCATION_DESC([NativeTypeName("D3D12MA::Pool *")] Pool* customPool, [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS flags = ALLOCATION_FLAG_NONE, void* privateData = null)
        {
            Base.Flags = flags;
            Base.HeapType = (D3D12_HEAP_TYPE)(0);
            Base.ExtraHeapFlags = D3D12_HEAP_FLAG_NONE;
            Base.CustomPool = customPool;
            Base.pPrivateData = privateData;
        }

        public CALLOCATION_DESC(D3D12_HEAP_TYPE heapType, [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS flags = ALLOCATION_FLAG_NONE, void* privateData = null, D3D12_HEAP_FLAGS extraHeapFlags = (D3D12_HEAP_FLAG_CREATE_NOT_ZEROED))
        {
            Base.Flags = flags;
            Base.HeapType = heapType;
            Base.ExtraHeapFlags = extraHeapFlags;
            Base.CustomPool = null;
            Base.pPrivateData = privateData;
        }
    }
}
