namespace Primary.Rendering.Resources
{
    public struct FrameGraphBufferDesc
    {
        public uint Width;
        public int Stride;

        public FGBufferUsage Usage;

        public FrameGraphBufferDesc()
        {
            Width = 0;
            Stride = 0;

            Usage = FGBufferUsage.Undefined;
        }
    }

    public enum FGBufferUsage : ushort
    {
        Undefined = 0,

        ConstantBuffer = 1 << 0,
        GenericShader = 1 << 1,
        PixelShader = 1 << 2,

        VertexBuffer = 1 << 3,
        IndexBuffer = 1 << 4,

        Structured = 1 << 5,
        Raw = 1 << 7,

        UnorderedAccess = 1 << 8,

        Global = 1 << 6
    }
}
