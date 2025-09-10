namespace Editor.Interop.Compressonator
{
    public partial struct CMP_MIPPROGRESSPARAM
    {
        [NativeTypeName("CMP_FLOAT")]
        public float mipProgress;

        [NativeTypeName("CMP_INT")]
        public int mipLevel;

        [NativeTypeName("CMP_INT")]
        public int cubeFace;
    }
}
