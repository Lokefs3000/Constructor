using Primary.Components;
using Primary.Rendering.Data;
using Primary.Rendering2.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.Temporary
{
    [ComponentConnections(typeof(RenderBounds))]
    public struct MeshRenderer2 : IComponent
    {
        public RawRenderMesh? Mesh;
        public MaterialAsset2? Material;
    }
}
