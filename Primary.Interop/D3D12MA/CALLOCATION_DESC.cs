using Vortice.Direct3D12;
using static Primary.Interop.ALLOCATION_FLAGS;

namespace Primary.Interop
{
    [NativeTypeName("struct CALLOCATION_DESC : D3D12MA::ALLOCATION_DESC")]
    public unsafe partial struct CALLOCATION_DESC
    {
        public ALLOCATION_DESC Base;

        public CALLOCATION_DESC([NativeTypeName("const ALLOCATION_DESC &")] ALLOCATION_DESC* o)
        {
            this.Base = *o;
        }

        public CALLOCATION_DESC([NativeTypeName("D3D12MA::Pool *")] Pool* customPool, [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS flags = ALLOCATION_FLAG_NONE, void* privateData = null)
        {
            Base.Flags = flags;
            Base.HeapType = (HeapType)(0);
            Base.ExtraHeapFlags = HeapFlags.None;
            Base.CustomPool = customPool;
            Base.pPrivateData = privateData;
        }

        public CALLOCATION_DESC(HeapType heapType, [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS flags = ALLOCATION_FLAG_NONE, void* privateData = null, HeapFlags extraHeapFlags = HeapFlags.CreateNotZeroed)
        {
            Base.Flags = flags;
            Base.HeapType = heapType;
            Base.ExtraHeapFlags = extraHeapFlags;
            Base.CustomPool = null;
            Base.pPrivateData = privateData;
        }
    }
}
