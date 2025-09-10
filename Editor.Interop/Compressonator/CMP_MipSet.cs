namespace Editor.Interop.Compressonator
{
    public unsafe partial struct CMP_MipSet
    {
        [NativeTypeName("CMP_INT")]
        public int m_nWidth;

        [NativeTypeName("CMP_INT")]
        public int m_nHeight;

        [NativeTypeName("CMP_INT")]
        public int m_nDepth;

        public CMP_FORMAT m_format;

        [NativeTypeName("ChannelFormat")]
        public CMP_ChannelFormat m_ChannelFormat;

        [NativeTypeName("TextureDataType")]
        public CMP_TextureDataType m_TextureDataType;

        [NativeTypeName("TextureType")]
        public CMP_TextureType m_TextureType;

        [NativeTypeName("CMP_UINT")]
        public uint m_Flags;

        [NativeTypeName("CMP_BYTE")]
        public byte m_CubeFaceMask;

        [NativeTypeName("CMP_DWORD")]
        public uint m_dwFourCC;

        [NativeTypeName("CMP_DWORD")]
        public uint m_dwFourCC2;

        [NativeTypeName("CMP_INT")]
        public int m_nMaxMipLevels;

        [NativeTypeName("CMP_INT")]
        public int m_nMipLevels;

        public CMP_FORMAT m_transcodeFormat;

        [NativeTypeName("CMP_BOOL")]
        public byte m_compressed;

        public CMP_FORMAT m_isDeCompressed;

        [NativeTypeName("CMP_BOOL")]
        public byte m_swizzle;

        [NativeTypeName("CMP_BYTE")]
        public byte m_nBlockWidth;

        [NativeTypeName("CMP_BYTE")]
        public byte m_nBlockHeight;

        [NativeTypeName("CMP_BYTE")]
        public byte m_nBlockDepth;

        [NativeTypeName("CMP_BYTE")]
        public byte m_nChannels;

        [NativeTypeName("CMP_BYTE")]
        public byte m_isSigned;

        [NativeTypeName("CMP_DWORD")]
        public uint dwWidth;

        [NativeTypeName("CMP_DWORD")]
        public uint dwHeight;

        [NativeTypeName("CMP_DWORD")]
        public uint dwDataSize;

        [NativeTypeName("CMP_BYTE *")]
        public byte* pData;

        [NativeTypeName("CMP_MipLevelTable *")]
        public CMP_MipLevel** m_pMipLevelTable;

        public void* m_pReservedData;

        [NativeTypeName("CMP_INT")]
        public int m_nIterations;

        [NativeTypeName("CMP_INT")]
        public int m_atmiplevel;

        [NativeTypeName("CMP_INT")]
        public int m_atfaceorslice;
    }
}
