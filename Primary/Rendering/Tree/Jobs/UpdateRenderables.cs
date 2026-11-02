using System.Numerics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Components;
using Primary.Mathematics;
using Primary.Threading;
using Primary.Timing;

namespace Primary.Rendering.Tree.Jobs
{
    internal sealed class UpdateRenderables : IJob
    {
        private readonly OctreeManager _manager;

        private readonly QueryDescription _queryDesc;

        private Query? _query;

        internal UpdateRenderables(OctreeManager octreeManager)
        {
            _manager = octreeManager;

            _queryDesc = new QueryDescription().WithAll<RenderBounds, RenderOctantInfo>();

            _query = null;
        }

        internal JobHandle ScheduleAll(JobHandle parentJob)
        {
            World world = Engine.GlobalSingleton.SceneManager.World;
            _query = world.Query(in _queryDesc);

            return JobScheduler.Schedule(this, parentJob, JobPriority.Realtime);
        }

        void IJob.Execute()
        {
            foreach (ref Chunk currentChunk in _query!)
            {
                ref RenderBounds renderBoundsFirst = ref currentChunk.GetFirst<RenderBounds>();
                ref RenderOctantInfo octantInfoFirst = ref currentChunk.GetFirst<RenderOctantInfo>();

                foreach (int entityIndex in currentChunk)
                {
                    ref RenderOctantInfo octantInfo = ref Unsafe.Add(ref octantInfoFirst, entityIndex);
                    ref RenderBounds renderBounds = ref Unsafe.Add(ref renderBoundsFirst, entityIndex);

                    if (octantInfo.LastUpdateFrame < renderBounds.UpdateIndex)
                    {
                        Vector3 center = (renderBounds.ComputedBounds.Minimum + renderBounds.ComputedBounds.Maximum) * 0.5f;
                        Vector3 halfExtents = (renderBounds.ComputedBounds.Maximum - renderBounds.ComputedBounds.Minimum) * 0.5f;
                        RenderExtents extents = new RenderExtents(center, halfExtents);

                        Int3 gridPoint = RenderOctree.GetGridPointFor(extents.Center);
                        if (octantInfo.Octree == null || octantInfo.Octree.GridPoint != gridPoint)
                        {
                            if (octantInfo.RenderableId != -1 && octantInfo.Octree != null)
                            {
                                octantInfo.Octree.DestroyRenderable(octantInfo.RenderableId);
                            }

                            octantInfo.Octree = _manager.GetOrCreateOctree(gridPoint);

                            octantInfo.RenderableId = -1;
                            octantInfo.LastUpdateFrame = -1;
                        }

                        if (octantInfo.RenderableId == -1)
                        {
                            RenderOctree octree = octantInfo.Octree!;
                            ref RenderableEntity renderable = ref octree.CreateRenderable(out octantInfo.RenderableId);

                            RenderOctant? octant = octree.FitExtentsInTree(extents);
                            octree.SetOctant(ref renderable, octant);

                            octantInfo.LastUpdateFrame = -1;
                        }
                    }
                }
            }

            _query = null;
        }
    }
}
