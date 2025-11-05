using Primary.Rendering.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components.Rendering
{
    [IncompatibleComponents(typeof(MeshSourceFilter))]
    public struct RenderMeshFilter : IComponent
    {
        public RawRenderMesh? Source;
    }
}
