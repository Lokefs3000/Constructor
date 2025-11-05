using System.Runtime.InteropServices;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct DefragmentationContext : D3D12MA::IUnknownImpl")]
    public unsafe partial struct DefragmentationContext
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::DefragmentationContextPimpl *")]
        private DefragmentationContextPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BeginPass@DefragmentationContext@D3D12MA@@QEAAJPEAUDEFRAGMENTATION_PASS_MOVE_INFO@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int BeginPass(DefragmentationContext* pThis, [NativeTypeName("D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO *")] DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?EndPass@DefragmentationContext@D3D12MA@@QEAAJPEAUDEFRAGMENTATION_PASS_MOVE_INFO@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int EndPass(DefragmentationContext* pThis, [NativeTypeName("D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO *")] DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetStats@DefragmentationContext@D3D12MA@@QEAAXPEAUDEFRAGMENTATION_STATS@2@@Z", ExactSpelling = true)]
        public static extern void GetStats(DefragmentationContext* pThis, [NativeTypeName("D3D12MA::DEFRAGMENTATION_STATS *")] DEFRAGMENTATION_STATS* pStats);
    }
}
