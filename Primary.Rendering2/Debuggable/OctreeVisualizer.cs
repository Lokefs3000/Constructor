using Primary.Common;
using Primary.Rendering2.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Debuggable
{
    internal static class OctreeVisualizer
    {
        public static void Visualize(OctreeManager manager, IDebugRenderer renderer)
        {
            foreach (RegionOctree octree in manager.Regions.Values)
            {
                DrawOctantRecursive(octree.RootOctant, 0);   
                renderer.DrawWireAABB(new AABB(octree.WorldBounds.Minimum, octree.WorldBounds.Maximum), new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
            }

            void DrawOctantRecursive(RenderOctant octant, int depth)
            {
                foreach (RenderOctant subOctant in octant.Octants)
                {
                    DrawOctantRecursive(subOctant, depth + 1);
                }

                float perc = depth / (float)RegionOctree.MaxOctantTreeDepth;
                renderer.DrawWireAABB(octant.Boundaries, new Vector4(1.0f, 1.0f - perc, 0.0f, 1.0f));
            }
        }
    }
}
