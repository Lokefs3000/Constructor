using Arch.Core;
using Primary.Common;
using Primary.Mathematics;
using Primary.Components;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Scenes.Components;
using Primary.Timing;
using System.Numerics;
using System.Runtime.Intrinsics;

using SceneEntityManager = Primary.Scenes.Components.SceneEntityManager;
using System.Buffers;
using Primary.Rendering.Tree.Jobs;
using Primary.Threading;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Tree
{
    public sealed class OctreeManager
    {
        private readonly ArrayPool<RenderableEntity> _entityArrayPool;
        private readonly Dictionary<Int3, RenderOctree> _regions;

        private readonly UpdateRenderables _updateRenderables;
        private readonly UpdateMeshRenderers _updateMeshRenderers;
        private readonly ScheduleOctreeUpdates _scheduleOctreeUpdates;

        internal OctreeManager()
        {
            _entityArrayPool = ArrayPool<RenderableEntity>.Create();
            _regions = new Dictionary<Int3, RenderOctree>();

            _updateRenderables = new UpdateRenderables(this);
            _updateMeshRenderers = new UpdateMeshRenderers(this);
            _scheduleOctreeUpdates = new ScheduleOctreeUpdates(this);
        }

        internal JobHandle SchedulePending(JobHandle parentJob)
        {
            using (new ProfilingScope("OctreeUpdate"))
            {
                JobHandle updateRenderables = _updateRenderables.ScheduleAll(parentJob);
                updateRenderables.WaitForCompletion();
                JobHandle updateMeshRenderers = _updateMeshRenderers.ScheduleAll(updateRenderables);
                updateMeshRenderers.WaitForCompletion();

                JobHandle octreeUpdates = _scheduleOctreeUpdates.ScheduleAll(updateMeshRenderers);

                return octreeUpdates;
            }
        }

        internal RenderOctree GetOrCreateOctree(Int3 gridPoint)
        {
            ref RenderOctree? octree = ref CollectionsMarshal.GetValueRefOrAddDefault(_regions, gridPoint, out bool exists);
            if (!exists || octree == null)
            {
                octree = new RenderOctree(_entityArrayPool, gridPoint);
            }

            return octree;
        }

        internal RenderOctree GetOctree(Int3 gridPoint)
        {
            return _regions[gridPoint];
        }

        public Dictionary<Int3, RenderOctree> Regions => _regions;
    }
}
