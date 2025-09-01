using static Primary.Interop.VIRTUAL_ALLOCATION_FLAGS;

namespace Primary.Interop
{
    [NativeTypeName("struct CVIRTUAL_ALLOCATION_DESC : D3D12MA::VIRTUAL_ALLOCATION_DESC")]
    public unsafe partial struct CVIRTUAL_ALLOCATION_DESC
    {
        public VIRTUAL_ALLOCATION_DESC Base;

        public CVIRTUAL_ALLOCATION_DESC([NativeTypeName("const VIRTUAL_ALLOCATION_DESC &")] VIRTUAL_ALLOCATION_DESC* o)
        {
            this.Base = *o;
        }

        public CVIRTUAL_ALLOCATION_DESC([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS flags = VIRTUAL_ALLOCATION_FLAG_NONE, void* privateData = null)
        {
            Base.Flags = flags;
            Base.Size = size;
            Base.Alignment = alignment;
            Base.pPrivateData = privateData;
        }
    }
}
