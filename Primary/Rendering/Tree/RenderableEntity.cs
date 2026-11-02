using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Primary.Assets;
using Primary.Mathematics;

namespace Primary.Rendering.Tree
{
    // 128 bytes in size
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RenderableEntity
    {
        public int Id;
        public byte OctantIndex;
        public byte OctantBitshift;
        public RenderOctant? Octant;
        public RenderExtents Extents;
        public float SqrBoundingRadius;
        public MeshAsset? Mesh;
        public MaterialAsset? Material;
        public ulong Layers;
        public Matrix4x4 Transform;

        public static readonly RenderableEntity Null = new RenderableEntity { Id = -1 };
    }
}
