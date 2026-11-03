using Primary.Common;
using Primary.Mathematics;
using System.Numerics;
using Primary.Scenes;
using Primary.Components;
using System.Runtime.CompilerServices;
using Primary.Timing;
using Primary.Rendering.Tree;

namespace Primary.Rendering.Diagnostics
{
    public static class OctreeVisualizer
    {
        public static void Visualize(OctreeManager manager, in Frustrum cullingFrustrum, in Vector3 origin)
        {
            using RentedArray<ushort> octantMask = new RentedArray<ushort>(255);

            int isInsideAnyOctant = 0;
            foreach (RenderOctree octree in manager.Regions.Values)
            {
                bool isWithinOctree = false;
                if (octree.CurrentExtents.AsAABB().Intersects(origin))
                {
                    if (isInsideAnyOctant > 0)
                        octantMask.Span[..isInsideAnyOctant].Fill(0);
                    if (octree.Octants != null)
                    {
                        foreach (RenderOctant octant in octree.Octants)
                        {
                            DrawOctantRecursive(octant, 0, in cullingFrustrum, in origin, FrustrumIntersection.Inside, octantMask.Span, ref isInsideAnyOctant);
                        }
                    }

                    octantMask[0] = ushort.MaxValue;
                    isInsideAnyOctant = Math.Max(isInsideAnyOctant, 1);

                    isWithinOctree = true;
                }

                Gizmos.DrawWireAABB(octree.CurrentExtents.AsAABB(), new Color(1.0f, 1.0f, 0.0f, isWithinOctree ? 1.0f : 0.2f));
                if (isWithinOctree)
                    Gizmos.DrawText(octree.CurrentExtents.Center, $"Entities: {octree.EntityCount}");

                if (isInsideAnyOctant > 0)
                {
                    foreach (ref readonly EntityChunk chunk in octree.Chunks)
                    {
                        foreach (ref RenderableEntity entity in chunk.Entities)
                        {
                            if (((octantMask[entity.OctantIndex] >> entity.OctantBitshift) & 0x3) > 0)
                            {
                                Gizmos.DrawWireAABB(entity.Extents.AsAABB(), Color.NavyBlue);
                            }
                        }
                    }
                }
            }

            void DrawOctantRecursive(RenderOctant octant, int depth, in Frustrum cullingFrustrum, in Vector3 origin, FrustrumIntersection octantIntersection, Span<ushort> visibilityMask, ref int isInAnyOctant)
            {

                if (octant.Octants != null)
                {
                    foreach (RenderOctant subOctant in octant.Octants)
                    {
                        FrustrumIntersection intersection = octantIntersection == FrustrumIntersection.Partial ?
                            cullingFrustrum.IntersectWhere(octant.CurrentExtents.AsAABB()) :
                            FrustrumIntersection.Inside;

                        if (intersection == FrustrumIntersection.Outside)
                            continue;

                        DrawOctantRecursive(subOctant, depth + 1, in cullingFrustrum, in origin, intersection, visibilityMask, ref isInAnyOctant);
                    }
                }

                float perc = depth / (float)RenderOctree.MaxTreeDepth;

                Color arrowColor = new Color(0.0f);
                arrowColor[2 - (int)octantIntersection] = 1.0f - perc;

                if (octant.CurrentExtents.AsAABB().Intersects(origin))
                {
                    Gizmos.DrawWireAABB(octant.CurrentExtents.AsAABB(), arrowColor);
                    Gizmos.DrawText(octant.CurrentExtents.Center, $"Entities: {octant.EntityCount}");

                    visibilityMask[octant.EntityIndex] |= (ushort)(0x3 << octant.EntityBitShift);
                    isInAnyOctant = Math.Max(isInAnyOctant, octant.EntityIndex + 1);
                }
            }
        }
    }
}
