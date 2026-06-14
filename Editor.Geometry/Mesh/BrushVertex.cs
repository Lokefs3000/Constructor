using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public readonly record struct BrushVertex(Vector3 Position, Vector3 Normal, Vector3 Tangent, Vector3 Bitangent, Vector2 UV);
}
