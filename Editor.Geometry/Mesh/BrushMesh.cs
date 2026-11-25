using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public readonly record struct BrushMesh(int UpdateIndex, int FaceRemap, BrushVertex[] Vertices, MeshFace[] Faces)
    {
        public int GetFaceIndex(int index) => index == 0 ? (FaceRemap & 0b111) : ((FaceRemap >> (index * 3)) & 0b111);
    }
}
