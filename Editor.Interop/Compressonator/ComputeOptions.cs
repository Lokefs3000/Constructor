namespace Editor.Interop.Compressonator
{
    public unsafe partial struct ComputeOptions
    {
        [NativeTypeName("bool")]
        public byte force_rebuild;

        public void* plugin_compute;
    }
}
