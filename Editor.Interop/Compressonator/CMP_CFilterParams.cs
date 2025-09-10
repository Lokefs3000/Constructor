namespace Editor.Interop.Compressonator
{
    public partial struct CMP_CFilterParams
    {
        public int nFilterType;

        [NativeTypeName("unsigned long")]
        public uint dwMipFilterOptions;

        public int nMinSize;

        public float fGammaCorrection;

        public float fSharpness;

        public int destWidth;

        public int destHeight;

        [NativeTypeName("bool")]
        public byte useSRGB;
    }
}
