using Primary.Common;
using Primary.Mathematics;
using System.Numerics;
using Primary.Rendering.Partitioning;

namespace Primary.Rendering.Diagnostics
{
    public static class OctreeVisualizer
    {
        public static void Visualize(PartitioningManager manager, in Frustrum cullingFrustrum, in Vector3 origin)
        {
            foreach (var (_, chunk) in manager.Regions)
            {
                Color color = new Color(
                    (chunk.GridPoint.X % 8) * 0.05f + 0.6f,
                    (chunk.GridPoint.Y % 8) * 0.05f + 0.6f,
                    (chunk.GridPoint.Z % 8) * 0.05f + 0.6f);

                if (chunk.CurrentExtents.AsAABB().Intersects(origin))
                {
                    foreach (ref readonly RenderableChunk renderableChunk in chunk.Chunks)
                    {
                        foreach (ref readonly Renderable renderable in renderableChunk.Renderables)
                        {
                            FrustrumIntersection intersection = renderable.Extents.Intersect(in cullingFrustrum);
                            if (intersection == FrustrumIntersection.Outside)
                                continue;

                            Gizmos.DrawWireAABB(renderable.Extents.AsAABB(), intersection == FrustrumIntersection.Inside ? Color.NavyBlue : Color.Cyan);
                        }
                    }

                    Gizmos.DrawText(chunk.SourceExtents.Center, $"Entities: {chunk.EntityCount}");
                }

                Gizmos.DrawWireAABB(chunk.CurrentExtents.AsAABB(), color);
            }
        }
    }
}
