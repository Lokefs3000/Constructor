namespace Editor.Interop.Compressonator
{
    public partial struct CMP_CVisionProcessOptions
    {
        public CMP_VISION_PROCESS nProcessType;

        [NativeTypeName("CMP_BOOL")]
        public byte Auto;

        [NativeTypeName("CMP_BOOL")]
        public byte AlignImages;

        [NativeTypeName("CMP_BOOL")]
        public byte ShowImages;

        [NativeTypeName("CMP_BOOL")]
        public byte SaveMatch;

        [NativeTypeName("CMP_BOOL")]
        public byte SaveImages;

        [NativeTypeName("CMP_BOOL")]
        public byte SSIM;

        [NativeTypeName("CMP_BOOL")]
        public byte PSNR;

        [NativeTypeName("CMP_BOOL")]
        public byte ImageDiff;

        [NativeTypeName("CMP_BOOL")]
        public byte CropImages;

        [NativeTypeName("CMP_INT")]
        public int Crop;
    }
}
