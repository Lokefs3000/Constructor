using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct Allocator : D3D12MA::IUnknownImpl")]
    public unsafe partial struct Allocator
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::AllocatorPimpl *")]
        private AllocatorPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetD3D12Options@Allocator@D3D12MA@@QEBAAEBUD3D12_FEATURE_DATA_D3D12_OPTIONS@@XZ", ExactSpelling = true)]
        [return: NativeTypeName("const D3D12_FEATURE_DATA_D3D12_OPTIONS &")]
        public static extern D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsUMA@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsUMA(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsCacheCoherentUMA@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsCacheCoherentUMA(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsGPUUploadHeapSupported@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsGPUUploadHeapSupported(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsTightAlignmentSupported@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsTightAlignmentSupported(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetMemoryCapacity@Allocator@D3D12MA@@QEBA_KI@Z", ExactSpelling = true)]
        [return: NativeTypeName("UINT64")]
        public static extern ulong GetMemoryCapacity(Allocator* pThis, uint memorySegmentGroup);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateResource@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_DESC@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@PEAPEAVAllocation@2@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateResource(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateResource2@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_DESC1@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@PEAPEAVAllocation@2@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateResource2(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateResource3@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_DESC1@@W4D3D12_BARRIER_LAYOUT@@PEBUD3D12_CLEAR_VALUE@@IPEBW4DXGI_FORMAT@@PEAPEAVAllocation@2@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateResource3(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, Allocation** ppAllocation, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?AllocateMemory@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_ALLOCATION_INFO@@PEAPEAVAllocation@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int AllocateMemory(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO *")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, Allocation** ppAllocation);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateAliasingResource@Allocator@D3D12MA@@QEAAJPEAVAllocation@2@_KPEBUD3D12_RESOURCE_DESC@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAliasingResource(Allocator* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateAliasingResource1@Allocator@D3D12MA@@QEAAJPEAVAllocation@2@_KPEBUD3D12_RESOURCE_DESC1@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAliasingResource1(Allocator* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateAliasingResource2@Allocator@D3D12MA@@QEAAJPEAVAllocation@2@_KPEBUD3D12_RESOURCE_DESC1@@W4D3D12_BARRIER_LAYOUT@@PEBUD3D12_CLEAR_VALUE@@IPEBW4DXGI_FORMAT@@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAliasingResource2(Allocator* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreatePool@Allocator@D3D12MA@@QEAAJPEBUPOOL_DESC@2@PEAPEAVPool@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreatePool(Allocator* pThis, [NativeTypeName("const POOL_DESC *")] POOL_DESC* pPoolDesc, Pool** ppPool);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetCurrentFrameIndex@Allocator@D3D12MA@@QEAAXI@Z", ExactSpelling = true)]
        public static extern void SetCurrentFrameIndex(Allocator* pThis, uint frameIndex);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetBudget@Allocator@D3D12MA@@QEAAXPEAUBudget@2@0@Z", ExactSpelling = true)]
        public static extern void GetBudget(Allocator* pThis, [NativeTypeName("D3D12MA::Budget *")] Budget* pLocalBudget, [NativeTypeName("D3D12MA::Budget *")] Budget* pNonLocalBudget);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CalculateStatistics@Allocator@D3D12MA@@QEAAXPEAUTotalStatistics@2@@Z", ExactSpelling = true)]
        public static extern void CalculateStatistics(Allocator* pThis, [NativeTypeName("D3D12MA::TotalStatistics *")] TotalStatistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BuildStatsString@Allocator@D3D12MA@@QEBAXPEAPEA_WH@Z", ExactSpelling = true)]
        public static extern void BuildStatsString(Allocator* pThis, [NativeTypeName("WCHAR **")] ushort** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeStatsString@Allocator@D3D12MA@@QEBAXPEA_W@Z", ExactSpelling = true)]
        public static extern void FreeStatsString(Allocator* pThis, [NativeTypeName("WCHAR *")] ushort* pStatsString);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BeginDefragmentation@Allocator@D3D12MA@@QEAAXPEBUDEFRAGMENTATION_DESC@2@PEAPEAVDefragmentationContext@2@@Z", ExactSpelling = true)]
        public static extern void BeginDefragmentation(Allocator* pThis, [NativeTypeName("const DEFRAGMENTATION_DESC *")] DEFRAGMENTATION_DESC* pDesc, DefragmentationContext** ppContext);
    }
}
