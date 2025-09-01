using Vortice.Direct3D12;
using static Primary.Interop.POOL_FLAGS;

namespace Primary.Interop
{
    [NativeTypeName("struct CPOOL_DESC : D3D12MA::POOL_DESC")]
    public unsafe partial struct CPOOL_DESC
    {
        public POOL_DESC Base;

        public CPOOL_DESC([NativeTypeName("const POOL_DESC &")] POOL_DESC* o)
        {
            this.Base = *o;
        }

        public CPOOL_DESC(HeapType heapType, HeapFlags heapFlags, [NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS flags = (POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED), [NativeTypeName("UINT64")] ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = 0xffffffff, ResidencyPriority residencyPriority = ResidencyPriority.Normal)
        {
            Base.Flags = flags;
            Base.HeapProperties = new HeapProperties
            {
            };
            Base.HeapProperties.Type = heapType;
            Base.HeapFlags = heapFlags;
            Base.BlockSize = blockSize;
            Base.MinBlockCount = minBlockCount;
            Base.MaxBlockCount = maxBlockCount;
            Base.MinAllocationAlignment = 0;
            Base.pProtectedSession = null;
            Base.ResidencyPriority = residencyPriority;
        }

        public CPOOL_DESC([NativeTypeName("const D3D12_HEAP_PROPERTIES")] HeapProperties heapProperties, HeapFlags heapFlags, [NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS flags = (POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED), [NativeTypeName("UINT64")] ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = 0xffffffff, ResidencyPriority residencyPriority = ResidencyPriority.Normal)
        {
            Base.Flags = flags;
            Base.HeapProperties = heapProperties;
            Base.HeapFlags = heapFlags;
            Base.BlockSize = blockSize;
            Base.MinBlockCount = minBlockCount;
            Base.MaxBlockCount = maxBlockCount;
            Base.MinAllocationAlignment = 0;
            Base.pProtectedSession = null;
            Base.ResidencyPriority = residencyPriority;
        }
    }
}
