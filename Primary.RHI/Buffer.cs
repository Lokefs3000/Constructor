namespace Primary.RHI
{
    public abstract class Buffer : Resource
    {
        public abstract Descriptor AllocateDescriptor(BufferCBDescriptorDescription description);
        public abstract Descriptor AllocateDescriptor(BufferSRDescriptorDescription description);

        public abstract ref readonly BufferDescription Description { get; }
        public abstract string Name { set; }
    }

    public struct BufferDescription
    {
        public uint ByteWidth;
        public uint Stride;

        public MemoryUsage Memory;
        public BufferUsage Usage;
        public BufferMode Mode;
        public CPUAccessFlags CpuAccessFlags;
    }

    [Flags]
    public enum BufferUsage : byte
    {
        None = 0,
        VertexBuffer = 1 << 0,
        IndexBuffer = 1 << 1,
        ConstantBuffer = 1 << 2,
        ShaderResource = 1 << 3,
    }

    [Flags]
    public enum BufferMode : byte
    {
        None = 0,
        Structured = 1 << 0,
        Unordered = 1 << 1,
    }
}
