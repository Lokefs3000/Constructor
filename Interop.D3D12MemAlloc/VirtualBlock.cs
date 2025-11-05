using System.Runtime.InteropServices;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct VirtualBlock : D3D12MA::IUnknownImpl")]
    public unsafe partial struct VirtualBlock
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::VirtualBlockPimpl *")]
        private VirtualBlockPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsEmpty@VirtualBlock@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern  int IsEmpty(VirtualBlock* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetAllocationInfo@VirtualBlock@D3D12MA@@QEBAXUVirtualAllocation@2@PEAUVIRTUAL_ALLOCATION_INFO@2@@Z", ExactSpelling = true)]
        public static extern  void GetAllocationInfo(VirtualBlock* pThis, [NativeTypeName("D3D12MA::VirtualAllocation")] VirtualAllocation allocation, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO *")] VIRTUAL_ALLOCATION_INFO* pInfo);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?Allocate@VirtualBlock@D3D12MA@@QEAAJPEBUVIRTUAL_ALLOCATION_DESC@2@PEAUVirtualAllocation@2@PEA_K@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int Allocate(VirtualBlock* pThis, [NativeTypeName("const VIRTUAL_ALLOCATION_DESC *")] VIRTUAL_ALLOCATION_DESC* pDesc, [NativeTypeName("D3D12MA::VirtualAllocation *")] VirtualAllocation* pAllocation, [NativeTypeName("UINT64 *")] ulong* pOffset);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeAllocation@VirtualBlock@D3D12MA@@QEAAXUVirtualAllocation@2@@Z", ExactSpelling = true)]
        public static extern void FreeAllocation(VirtualBlock* pThis, [NativeTypeName("D3D12MA::VirtualAllocation")] VirtualAllocation allocation);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?Clear@VirtualBlock@D3D12MA@@QEAAXXZ", ExactSpelling = true)]
        public static extern void Clear(VirtualBlock* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetAllocationPrivateData@VirtualBlock@D3D12MA@@QEAAXUVirtualAllocation@2@PEAX@Z", ExactSpelling = true)]
        public static extern void SetAllocationPrivateData(VirtualBlock* pThis, [NativeTypeName("D3D12MA::VirtualAllocation")] VirtualAllocation allocation, void* pPrivateData);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetStatistics@VirtualBlock@D3D12MA@@QEBAXPEAUStatistics@2@@Z", ExactSpelling = true)]
        public static extern  void GetStatistics(VirtualBlock* pThis, [NativeTypeName("D3D12MA::Statistics *")] Statistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CalculateStatistics@VirtualBlock@D3D12MA@@QEBAXPEAUDetailedStatistics@2@@Z", ExactSpelling = true)]
        public static extern  void CalculateStatistics(VirtualBlock* pThis, [NativeTypeName("D3D12MA::DetailedStatistics *")] DetailedStatistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BuildStatsString@VirtualBlock@D3D12MA@@QEBAXPEAPEA_W@Z", ExactSpelling = true)]
        public static extern  void BuildStatsString(VirtualBlock* pThis, [NativeTypeName("WCHAR **")] ushort** ppStatsString);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeStatsString@VirtualBlock@D3D12MA@@QEBAXPEA_W@Z", ExactSpelling = true)]
        public static extern  void FreeStatsString(VirtualBlock* pThis, [NativeTypeName("WCHAR *")] ushort* pStatsString);
    }
}
