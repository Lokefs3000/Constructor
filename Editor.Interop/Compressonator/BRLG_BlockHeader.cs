namespace Editor.Interop.Compressonator
{
    public partial struct BRLG_BlockHeader
    {
        [NativeTypeName("CMP_DWORD")]
        public uint originalWidth;

        [NativeTypeName("CMP_UINT")]
        public uint originalHeight;

        public CMP_FORMAT originalFormat;

        public CMP_TextureType originalTextureType;

        public CMP_TextureDataType originalTextureDataType;

        [NativeTypeName("CMP_UINT")]
        public uint extraDataSize;

        [NativeTypeName("CMP_UINT")]
        public uint compressedBlockSize;
    }
}
