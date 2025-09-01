namespace Primary.Interop
{
    public unsafe partial struct DEFRAGMENTATION_MOVE
    {
        [NativeTypeName("D3D12MA::DEFRAGMENTATION_MOVE_OPERATION")]
        public DEFRAGMENTATION_MOVE_OPERATION Operation;

        [NativeTypeName("D3D12MA::Allocation *")]
        public Allocation* pSrcAllocation;

        [NativeTypeName("D3D12MA::Allocation *")]
        public Allocation* pDstTmpAllocation;
    }
}
