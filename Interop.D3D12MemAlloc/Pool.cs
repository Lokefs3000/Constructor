using System.Runtime.InteropServices;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct Pool : D3D12MA::IUnknownImpl")]
    public unsafe partial struct Pool
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::PoolPimpl *")]
        private PoolPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetDesc@Pool@D3D12MA@@QEBA?AUPOOL_DESC@2@XZ", ExactSpelling = true)]
        [return: NativeTypeName("D3D12MA::POOL_DESC")]
        public static extern POOL_DESC GetDesc(Pool* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetStatistics@Pool@D3D12MA@@QEAAXPEAUStatistics@2@@Z", ExactSpelling = true)]
        public static extern void GetStatistics(Pool* pThis, [NativeTypeName("D3D12MA::Statistics *")] Statistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CalculateStatistics@Pool@D3D12MA@@QEAAXPEAUDetailedStatistics@2@@Z", ExactSpelling = true)]
        public static extern void CalculateStatistics(Pool* pThis, [NativeTypeName("D3D12MA::DetailedStatistics *")] DetailedStatistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetName@Pool@D3D12MA@@QEAAXPEB_W@Z", ExactSpelling = true)]
        public static extern void SetName(Pool* pThis, [NativeTypeName("LPCWSTR")] ushort* Name);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetName@Pool@D3D12MA@@QEBAPEB_WXZ", ExactSpelling = true)]
        [return: NativeTypeName("LPCWSTR")]
        public static extern ushort* GetName(Pool* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BeginDefragmentation@Pool@D3D12MA@@QEAAJPEBUDEFRAGMENTATION_DESC@2@PEAPEAVDefragmentationContext@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int BeginDefragmentation(Pool* pThis, [NativeTypeName("const DEFRAGMENTATION_DESC *")] DEFRAGMENTATION_DESC* pDesc, DefragmentationContext** ppContext);
    }
}
