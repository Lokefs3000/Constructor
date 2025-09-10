namespace Editor.Interop.Compressonator
{
    public unsafe partial struct BRLG_ExtraInfo
    {
        [NativeTypeName("char *")]
        public sbyte* fileName;

        [NativeTypeName("CMP_DWORD")]
        public uint numChars;
    }
}
