using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Data
{
    public interface IRenderMeshSource
    {
        public RHI.Buffer? VertexBuffer { get; }
        public RHI.Buffer? IndexBuffer { get; }
    }
}
