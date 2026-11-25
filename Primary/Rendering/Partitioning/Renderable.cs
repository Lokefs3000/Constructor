using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering.Batching;

namespace Primary.Rendering.Partitioning
{
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    public record struct Renderable
    {
        public int Id;
        public RenderExtents Extents;
        public int ModelIndex;
        public ushort MeshIndex;
        public RentedArray<RenderablePassIndex> MaterialPasses;
        public ulong RenderLayers;
        public Matrix4x4 Transform;
    }
}
