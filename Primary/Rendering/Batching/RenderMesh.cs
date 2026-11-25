using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Batching
{
    public readonly record struct RenderMesh(ulong PassId, int ModelIndex, ushort MeshIndex) : IEquatable<RenderMesh>
    {
        public override int GetHashCode() => HashCode.Combine(PassId, ModelIndex, MeshIndex);
    }
}
