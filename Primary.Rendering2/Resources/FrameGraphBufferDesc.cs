using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Resources
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

    public enum FGBufferUsage : byte
    {
        Undefined = 0,

        ConstantBuffer = 1 << 0,
        GenericShader = 1 << 1,
        PixelShader = 1 << 2,

        VertexBuffer = 1 << 3,
        IndexBuffer = 1 << 4,

        Structured = 1 << 5
    }
}
