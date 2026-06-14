using Arch.Core;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering.Tree;
using System.Numerics;

namespace Editor.Rendering.Debugging
{
    internal sealed class EntityDebugRenderer
    {
        internal void Render()
        {
            using (new ProfilingScope("DebugRender"))
            {
                using (new ProfilingScope("RenderBounds"))
                {
                    World world = EditorRuntime.GlobalSingleton.SceneManager.World;
                    if (RenderDebug.DrawEntityBounds)
                    {
                        world.InlineQuery<DrawRenderBoundsJob, RenderBounds>(DrawRenderBoundsJob.Query);
                    }
                }

                if (false)
                {
                    using (new ProfilingScope("Octree"))
                    {
                        OctreeManager octree = EditorRuntime.GlobalSingleton.RenderingManager.OctreeManager;
                        foreach (var (point, tree) in octree.Regions)
                        {
                            DrawOctantRecursive(point, tree.RootOctant);
                            Gizmos.DrawWireAABB(tree.WorldBounds, new Color(0.0f, 1.0f, 1.0f));
                        }
                    }
                }
            }
        }

        private void DrawOctantRecursive(OctreePoint point, RenderOctant octant)
        {
            if (octant.Children.Count > 0)
            {
                foreach (RenderOctant subOctant in octant.Octants)
                {
                    DrawOctantRecursive(octant.Point, subOctant);
                }
            }
            else
            {
                Gizmos.DrawWireAABB(octant.Boundaries, Color.Red);
            }
        }

        private struct DrawRenderBoundsJob : IForEach<RenderBounds>
        {
            public void Update(ref RenderBounds bounds)
            {
                Gizmos.DrawWireAABB(bounds.ComputedBounds, Color.Yellow);
            }

            public static readonly QueryDescription Query = new QueryDescription().WithAll<RenderBounds>();
        }
    }
}
