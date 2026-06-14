using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public readonly record struct MeshFaceUpdate(BrushMeshKey Key, int SliceIndex, int VertexOffset);
}
