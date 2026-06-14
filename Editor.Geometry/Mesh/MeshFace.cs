using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public readonly record struct MeshFace(BrushFaceIndex FaceIndex, MaterialAsset? Material, byte I0, byte I1, byte I2, byte I3, byte I4, byte I5);
}
