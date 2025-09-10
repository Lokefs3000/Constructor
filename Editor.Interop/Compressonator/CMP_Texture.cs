namespace Editor.Interop.Compressonator
{
    public unsafe partial struct CMP_Texture
    {
        [NativeTypeName("CMP_DWORD")]
        public uint dwSize;

        [NativeTypeName("CMP_DWORD")]
        public uint dwWidth;

        [NativeTypeName("CMP_DWORD")]
        public uint dwHeight;

        [NativeTypeName("CMP_DWORD")]
        public uint dwPitch;

        public CMP_FORMAT format;

        public CMP_FORMAT transcodeFormat;

        [NativeTypeName("CMP_BYTE")]
        public byte nBlockHeight;

        [NativeTypeName("CMP_BYTE")]
        public byte nBlockWidth;

        [NativeTypeName("CMP_BYTE")]
        public byte nBlockDepth;

        [NativeTypeName("CMP_DWORD")]
        public uint dwDataSize;

        [NativeTypeName("CMP_BYTE *")]
        public byte* pData;

        [NativeTypeName("CMP_VOID *")]
        public void* pMipSet;
    }
}
