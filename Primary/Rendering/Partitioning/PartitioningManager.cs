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
using Primary.Rendering.Partitioning.Jobs;
using Primary.Threading;
using System.Runtime.InteropServices;
using Primary.Services;
using System.Collections.Concurrent;
using Primary.Collections;

namespace Primary.Rendering.Partitioning
{
    public sealed class PartitioningManager : IService
    {
        private readonly RenderingManager _manager;

        private readonly ArrayPool<Renderable> _entityArrayPool;
        private readonly Dictionary<Int3, RenderGridChunk> _regions;

        private readonly Lock _regionsLock;

        private readonly ConcurrentBag<RenderableUpdateData> _createUpdates;
        private readonly ConcurrentBag<RenderableUpdateData> _destroyUpdates;
        private readonly ConcurrentBag<RenderableUpdateData> _updateUpdates;

        private readonly UpdateMeshRenderers _updateMeshRenderers;

        internal PartitioningManager(RenderingManager manager)
        {
            _manager = manager;

            _entityArrayPool = ArrayPool<Renderable>.Create();
            _regions = new Dictionary<Int3, RenderGridChunk>();

            _regionsLock = new Lock();

            _createUpdates = new ConcurrentBag<RenderableUpdateData>();
            _destroyUpdates = new ConcurrentBag<RenderableUpdateData>();
            _updateUpdates = new ConcurrentBag<RenderableUpdateData>();

            _updateMeshRenderers = new UpdateMeshRenderers(this);
        }

        internal JobHandle SchedulePending(JobHandle parentJob)
        {
            using (new ProfilingScope("UpdatePartitions"))
            {
                JobHandle updateMeshRenderers = _updateMeshRenderers.ScheduleAll(parentJob);

                JobHandle distributeUpdates = JobScheduler.Schedule(new FlushUpdatesJob(this), updateMeshRenderers, JobPriority.Realtime, true);
                JobHandle updateGridChunks = JobScheduler.Schedule(ScheduleChunkUpdates, distributeUpdates, JobPriority.Realtime, true);

                return updateGridChunks;
            }
        }

        private void ScheduleChunkUpdates()
        {
            using (new ProfilingScope("SchduleChunkUpdates"))
            {
                using RentedList<JobHandle> jobs = new RentedList<JobHandle>();
                foreach (var (_, chunk) in _regions)
                {
                    if (chunk.HasUpdates || chunk.WantsExtentsRefit)
                    {
                        jobs.Add(JobScheduler.Schedule(new UpdateGridChunk(chunk), JobPriority.Realtime, true));
                    }
                }

                JobScheduler.CombineAll(jobs.AsSpan()).WaitForCompletion();
            }
        }

        internal void RequestCreate(SceneEntity entity, RenderGridChunk gridChunk)
        {
            _createUpdates.Add(new RenderableUpdateData(RenderableUpdateData.Type.Create, entity, gridChunk, -1));
        }

        internal void RequestDestroy(RenderGridChunk gridChunk, int renderableId)
        {
            _destroyUpdates.Add(new RenderableUpdateData(RenderableUpdateData.Type.Destroy, SceneEntity.Null, gridChunk, renderableId));
        }

        internal void RequestUpdate(SceneEntity entity, RenderGridChunk gridChunk, int renderableId)
        {
            _updateUpdates.Add(new RenderableUpdateData(RenderableUpdateData.Type.Create, entity, gridChunk, renderableId));
        }

        internal RenderGridChunk GetOrCreateOctree(Int3 gridPoint)
        {
            using (_regionsLock.EnterScope())
            {
                ref RenderGridChunk? octree = ref CollectionsMarshal.GetValueRefOrAddDefault(_regions, gridPoint, out bool exists);
                if (!exists || octree == null)
                {
                    octree = new RenderGridChunk(_manager, _entityArrayPool, gridPoint);
                }

                return octree;
            }
        }

        internal RenderGridChunk GetOctree(Int3 gridPoint)
        {
            using (_regionsLock.EnterScope())
            {
                return _regions[gridPoint];
            }
        }

        public Dictionary<Int3, RenderGridChunk> Regions => _regions;

        private readonly record struct FlushUpdatesJob(PartitioningManager Manager) : IJob
        {
            public void Execute()
            {
                using (new ProfilingScope("DistributeUpdates"))
                {
                    while (Manager._destroyUpdates.TryTake(out RenderableUpdateData updateData))
                    {
                        updateData.Chunk.AddUpdate(updateData);
                    }

                    while (Manager._createUpdates.TryTake(out RenderableUpdateData updateData))
                    {
                        updateData.Chunk.AddUpdate(updateData);
                    }

                    while (Manager._updateUpdates.TryTake(out RenderableUpdateData updateData))
                    {
                        updateData.Chunk.AddUpdate(updateData);
                    }
                }
            }
        }

        private readonly record struct UpdateGridChunk(RenderGridChunk Chunk) : IJob
        {
            public void Execute()
            {
                using (new ProfilingScope("UpdateChunk"))
                {
                    if (Chunk.HasUpdates)
                    {
                        Chunk.FlushUpdates();
                    }

                    if (Chunk.WantsExtentsRefit)
                    {
                        Chunk.RefitExtents();
                    }
                }
            }
        }
    }

    internal readonly record struct RenderableUpdateData(RenderableUpdateData.Type UpdateType, SceneEntity Entity, RenderGridChunk Chunk, int RenderableId)
    {
        internal enum Type : byte
        {
            Create = 0,
            Destroy,
            Update
        }
    }
}
