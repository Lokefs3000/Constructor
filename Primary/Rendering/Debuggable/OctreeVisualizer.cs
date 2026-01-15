using Primary.Common;
using Primary.Rendering.Tree;
using System.Numerics;

namespace Primary.Rendering.Debuggable
{
    internal static class OctreeVisualizer
    {
        public static void Visualize(OctreeManager manager, IDebugRenderer renderer)
        {
            foreach (RegionOctree octree in manager.Regions.Values)
            {
                DrawOctantRecursive(octree.RootOctant, 0);
                renderer.DrawWireAABB(new AABB(octree.WorldBounds.Minimum, octree.WorldBounds.Maximum), new Color(1.0f, 1.0f, 0.0f, 1.0f));
            }

            void DrawOctantRecursive(RenderOctant octant, int depth)
            {
                foreach (RenderOctant subOctant in octant.Octants)
                {
                    DrawOctantRecursive(subOctant, depth + 1);
                }

                float perc = depth / (float)RegionOctree.MaxOctantTreeDepth;
                renderer.DrawWireAABB(octant.Boundaries, new Color(1.0f, 1.0f - perc, 0.0f, 1.0f));
            }
        }
    }
}
