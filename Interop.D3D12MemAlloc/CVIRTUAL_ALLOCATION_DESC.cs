using static Interop.D3D12MemAlloc.VIRTUAL_ALLOCATION_FLAGS;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct CVIRTUAL_ALLOCATION_DESC : D3D12MA::VIRTUAL_ALLOCATION_DESC")]
    public unsafe partial struct CVIRTUAL_ALLOCATION_DESC
    {
        public VIRTUAL_ALLOCATION_DESC Base;

        public CVIRTUAL_ALLOCATION_DESC([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS flags = VIRTUAL_ALLOCATION_FLAG_NONE, void* privateData = null)
        {
            Base.Flags = flags;
            Base.Size = size;
            Base.Alignment = alignment;
            Base.pPrivateData = privateData;
        }
    }
}
