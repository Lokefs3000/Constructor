using static Interop.D3D12MemAlloc.VIRTUAL_BLOCK_FLAGS;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct CVIRTUAL_BLOCK_DESC : D3D12MA::VIRTUAL_BLOCK_DESC")]
    public unsafe partial struct CVIRTUAL_BLOCK_DESC
    {
        public VIRTUAL_BLOCK_DESC Base;

        public CVIRTUAL_BLOCK_DESC([NativeTypeName("UINT64")] ulong size, [NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS flags = VIRTUAL_BLOCK_FLAG_NONE, [NativeTypeName("const ALLOCATION_CALLBACKS *")] ALLOCATION_CALLBACKS* allocationCallbacks = null)
        {
            Base.Flags = flags;
            Base.Size = size;
            Base.pAllocationCallbacks = allocationCallbacks;
        }
    }
}
