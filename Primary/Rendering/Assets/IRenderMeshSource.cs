using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Assets
{
    public interface IRenderMeshSource
    {
        public RHIBuffer? VertexBuffer { get; }
        public RHIBuffer? IndexBuffer { get; }
    }
}
