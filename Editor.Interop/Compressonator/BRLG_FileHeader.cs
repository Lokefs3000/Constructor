using System.Runtime.CompilerServices;

namespace Editor.Interop.Compressonator
{
    public partial struct BRLG_FileHeader
    {
        [NativeTypeName("CMP_BYTE[4]")]
        public _fileType_e__FixedBuffer fileType;

        [NativeTypeName("CMP_BYTE")]
        public byte majorVersion;

        [NativeTypeName("CMP_UINT")]
        public uint headerSize;

        [NativeTypeName("CMP_DWORD")]
        public uint compressedDataSize;

        [InlineArray(4)]
        public partial struct _fileType_e__FixedBuffer
        {
            public byte e0;
        }
    }
}
