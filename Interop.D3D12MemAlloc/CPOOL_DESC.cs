using TerraFX.Interop.DirectX;
using static TerraFX.Interop.DirectX.D3D12_RESIDENCY_PRIORITY;
using static Interop.D3D12MemAlloc.POOL_FLAGS;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct CPOOL_DESC : D3D12MA::POOL_DESC")]
    public unsafe partial struct CPOOL_DESC
    {
        public POOL_DESC Base;

        public CPOOL_DESC(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS flags = (POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED), [NativeTypeName("UINT64")] ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = 0xffffffff, D3D12_RESIDENCY_PRIORITY residencyPriority = D3D12_RESIDENCY_PRIORITY_NORMAL)
        {
            Base.Flags = flags;
            Base.HeapProperties = new D3D12_HEAP_PROPERTIES
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

        public CPOOL_DESC([NativeTypeName("const D3D12_HEAP_PROPERTIES")] D3D12_HEAP_PROPERTIES heapProperties, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS flags = (POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED), [NativeTypeName("UINT64")] ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = 0xffffffff, D3D12_RESIDENCY_PRIORITY residencyPriority = D3D12_RESIDENCY_PRIORITY_NORMAL)
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
