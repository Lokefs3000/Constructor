namespace Editor.Interop.Compressonator
{
    public partial struct CMP_AnalysisData
    {
        [NativeTypeName("unsigned long")]
        public uint analysisMode;

        [NativeTypeName("unsigned int")]
        public uint channelBitMap;

        public float fInputDefog;

        public float fInputExposure;

        public float fInputKneeLow;

        public float fInputKneeHigh;

        public float fInputGamma;

        public float mse;

        public float mseR;

        public float mseG;

        public float mseB;

        public float mseA;

        public float psnr;

        public float psnrR;

        public float psnrG;

        public float psnrB;

        public float psnrA;
    }
}
