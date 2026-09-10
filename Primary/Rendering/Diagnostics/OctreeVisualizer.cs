using Primary.Common;
using Primary.Rendering.Tree;
using Primary.Mathematics;
using System.Numerics;
using Primary.Scenes;
using Primary.Components;
using System.Runtime.CompilerServices;
using Primary.Timing;

namespace Primary.Rendering.Diagnostics
{
    public static class OctreeVisualizer
    {
        public static void Visualize(OctreeManager manager, in Frustrum cullingFrustrum)
        {
            foreach (RegionOctree octree in manager.Regions.Values)
            {
                if (!cullingFrustrum.Intersects(octree.WorldBounds))
                    continue;

                DrawOctantRecursive(octree.RootOctant, 0, in cullingFrustrum);
                Gizmos.DrawWireAABB(new AABB(octree.WorldBounds.Minimum, octree.WorldBounds.Maximum), new Color(1.0f, 1.0f, 0.0f, 1.0f));
            }

            void DrawOctantRecursive(RenderOctant octant, int depth, in Frustrum cullingFrustrum)
            {
                foreach (RenderOctant subOctant in octant.Octants)
                {
                    if (!cullingFrustrum.Intersects(subOctant.Boundaries))
                        continue;

                    DrawOctantRecursive(subOctant, depth + 1, in cullingFrustrum);
                }

                float perc = depth / (float)RegionOctree.MaxOctantTreeDepth;
                Gizmos.DrawWireAABB(octant.Boundaries, new Color(1.0f, 1.0f - perc, 0.0f, 1.0f));

                foreach (SceneEntity entity in octant.Children)
                {
                    ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
                    if (!Unsafe.IsNullRef(in bounds) && cullingFrustrum.Intersects(bounds.ComputedBounds))
                    {
                        int color = Math.Min(Time.GetFrameDifference(Time.FrameIndex, bounds.UpdateIndex), 100);
                        Gizmos.DrawWireAABB(bounds.ComputedBounds, Color.Blue.Darken(color * 0.005f));
                    }
                }
            }
        }
    }
}
