using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Assets
{
    public interface IRenderMeshSource
    {
        public RHIBuffer? VertexBuffer { get; }
        public RHIBuffer? IndexBuffer { get; }

        public bool IsLoaded { get; }
    }
}
