namespace Primary.Interop
{
    public unsafe partial struct DEFRAGMENTATION_PASS_MOVE_INFO
    {
        [NativeTypeName("UINT32")]
        public uint MoveCount;

        [NativeTypeName("D3D12MA::DEFRAGMENTATION_MOVE *")]
        public DEFRAGMENTATION_MOVE* pMoves;
    }
}
