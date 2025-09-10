namespace Editor.Interop.Compressonator
{
    public partial struct CMP_BC6H_BLOCK_PARAMETERS
    {
        [NativeTypeName("CMP_WORD")]
        public ushort dwMask;

        public float fExposure;

        [NativeTypeName("bool")]
        public byte bIsSigned;

        public float fQuality;

        [NativeTypeName("bool")]
        public byte bUsePatternRec;
    }
}
