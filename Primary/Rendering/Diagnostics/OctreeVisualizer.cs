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
            foreach (RegionOctree octree in manager.Regions.Values)
            {
                DrawOctantRecursive(octree.RootOctant, 0, in cullingFrustrum, in origin, FrustrumIntersection.Inside);
                Gizmos.DrawWireAABB(new AABB(octree.WorldBounds.Minimum, octree.WorldBounds.Maximum), new Color(1.0f, 1.0f, 0.0f, 1.0f));
            }

            void DrawOctantRecursive(RenderOctant octant, int depth, in Frustrum cullingFrustrum, in Vector3 origin, FrustrumIntersection octantIntersection)
            {
                foreach (RenderOctant subOctant in octant.Octants)
                {
                    FrustrumIntersection intersection = octantIntersection == FrustrumIntersection.Partial ?
                        cullingFrustrum.IntersectWhere(octant.Boundaries) :
                        FrustrumIntersection.Inside;

                    if (intersection == FrustrumIntersection.Outside)
                        continue;

                    DrawOctantRecursive(subOctant, depth + 1, in cullingFrustrum, in origin, intersection);
                }

                float perc = depth / (float)RegionOctree.MaxOctantTreeDepth;

                Color arrowColor = new Color(0.0f);
                arrowColor[2 - (int)octantIntersection] = 1.0f - perc;

                if (octant.Boundaries.Intersects(origin))
                {
                    Gizmos.DrawWireAABB(octant.Boundaries, arrowColor);
                    Gizmos.DrawText(octant.Boundaries.Center, $"Entities: {octant.Children.Count}");

                    Vector3 center = octant.Boundaries.Center;

                    RegionEntityList.Node? node = octant.Children.FirstNode;
                    do
                    {
                        foreach (SceneEntity entity in node.Entities)
                        {
                            ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
                            if (!Unsafe.IsNullRef(in bounds) && (octantIntersection == FrustrumIntersection.Inside || cullingFrustrum.Intersects(bounds.ComputedBounds)))
                            {
                                int frameDiff = Math.Min(Time.GetFrameDifference(Time.FrameIndex, bounds.UpdateIndex), 100);

                                Color boundsColor = arrowColor;
                                boundsColor.G = 1.0f - frameDiff * 0.005f;

                                Gizmos.DrawWireAABB(bounds.ComputedBounds, boundsColor);
                            }
                        }
                    } while ((node = node.Next) != null);
                }
                else
                {
                    RegionEntityList.Node? node = octant.Children.FirstNode;
                    do
                    {
                        foreach (SceneEntity entity in node.Entities)
                        {
                            ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
                            int frameDiff = Time.GetFrameDifference(Time.FrameIndex, bounds.UpdateIndex);
                            if (!Unsafe.IsNullRef(in bounds) && frameDiff < 100 && (octantIntersection == FrustrumIntersection.Inside || cullingFrustrum.Intersects(bounds.ComputedBounds)))
                            {
                                Gizmos.DrawWireAABB(bounds.ComputedBounds, Color.Blue.Darken(frameDiff * 0.005f));
                            }
                        }
                    } while ((node = node.Next) != null);
                }
            }
        }
    }
}
