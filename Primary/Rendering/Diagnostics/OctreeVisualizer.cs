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
            foreach (RenderOctree octree in manager.Regions.Values)
            {
                if (octree.Octants != null)
                {
                    foreach (RenderOctant octant in octree.Octants)
                    {
                        DrawOctantRecursive(octant, 0, in cullingFrustrum, in origin, FrustrumIntersection.Inside);
                    }
                }

                Gizmos.DrawWireAABB(octree.CurrentExtents.AsAABB(), new Color(1.0f, 1.0f, 0.0f, 1.0f));
            }

            void DrawOctantRecursive(RenderOctant octant, int depth, in Frustrum cullingFrustrum, in Vector3 origin, FrustrumIntersection octantIntersection)
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

                        DrawOctantRecursive(subOctant, depth + 1, in cullingFrustrum, in origin, intersection);
                    }
                }

                float perc = depth / (float)RenderOctree.MaxTreeDepth;

                Color arrowColor = new Color(0.0f);
                arrowColor[2 - (int)octantIntersection] = 1.0f - perc;

                if (octant.CurrentExtents.AsAABB().Intersects(origin))
                {
                    Gizmos.DrawWireAABB(octant.CurrentExtents.AsAABB(), arrowColor);
                    Gizmos.DrawText(octant.CurrentExtents.Center, $"Entities: {octant.EntityCount}");
                }
            }
        }
    }
}
