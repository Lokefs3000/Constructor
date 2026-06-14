using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public readonly record struct MeshVertexUpdate(int SourceIndex, int DestinationIndex, int Count);
}
