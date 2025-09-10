namespace Editor.Interop.Compressonator
{
    public partial struct CMP_CVisionProcessResults
    {
        [NativeTypeName("CMP_INT")]
        public int result;

        [NativeTypeName("CMP_INT")]
        public int imageSize;

        [NativeTypeName("CMP_FLOAT")]
        public float srcLSTD;

        [NativeTypeName("CMP_FLOAT")]
        public float tstLSTD;

        [NativeTypeName("CMP_FLOAT")]
        public float normLSTD;

        [NativeTypeName("CMP_FLOAT")]
        public float SSIM;

        [NativeTypeName("CMP_FLOAT")]
        public float PSNR;
    }
}
