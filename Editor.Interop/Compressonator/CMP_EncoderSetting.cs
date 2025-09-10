namespace Editor.Interop.Compressonator
{
    public partial struct CMP_EncoderSetting
    {
        [NativeTypeName("unsigned int")]
        public uint width;

        [NativeTypeName("unsigned int")]
        public uint height;

        [NativeTypeName("unsigned int")]
        public uint pitch;

        public float quality;

        [NativeTypeName("unsigned int")]
        public uint format;
    }
}
