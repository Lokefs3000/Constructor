using Editor.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo
{
    internal sealed record class SelectedFace(Brush Brush, BrushFaceIndex FaceIndex) : IEquatable<SelectedFace>
    {
        public override int GetHashCode() => HashCode.Combine(Brush, FaceIndex);
        public bool Equals(SelectedFace? other) => other != null && Brush == other.Brush && FaceIndex == other.FaceIndex;
    }
}
