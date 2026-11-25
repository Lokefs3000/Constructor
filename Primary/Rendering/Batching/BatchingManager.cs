using System.Collections.Concurrent;
using System.Numerics;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Rendering.Configuration;
using Primary.Rendering.Partitioning;
using Primary.Services;
using Primary.Threading;

namespace Primary.Rendering.Batching
{
    public sealed class BatchingManager : IService
    {
        private readonly BatchingGlobalOptions _options;

        private readonly RenderingManager _manager;
        private readonly ConcurrentBag<GridChunkBatcher> _idleGridBatchers;

        private List<RenderList> _activeLists;
        private List<JobHandle> _activeJobs;

        internal BatchingManager(RenderingManager manager)
        {
            _options = new BatchingGlobalOptions();

            _manager = manager;
            _idleGridBatchers = new ConcurrentBag<GridChunkBatcher>();

            _activeLists = new List<RenderList>();
            _activeJobs = new List<JobHandle>();
        }

        internal void ClearAllRenderLists()
        {
            if (_activeJobs.Count > 0)
            {
                JobScheduler.CombineAll(_activeJobs.AsSpan()).WaitForCompletion();
                _activeJobs.Clear();
            }

            foreach (RenderList renderList in _activeLists)
            {
                renderList.Clear();
            }
        }

        internal GridChunkBatcher GetGridChunkBatcher()
        {
            return _idleGridBatchers.TryTake(out GridChunkBatcher? result) ? result : new GridChunkBatcher();
        }

        internal void ReturnGridChunkBatcher(GridChunkBatcher gridChunkBatcher)
        {
            gridChunkBatcher.ClearBatchData();
            _idleGridBatchers.Add(gridChunkBatcher);
        }

        internal void RemoveRenderList(RenderList renderList)
        {
            _activeLists.Remove(renderList);
        }

        public RenderList CreateRenderList()
        {
            RenderList list = new RenderList(this);
            _activeLists.Add(list);

            return list;
        }

        internal JobHandle BatchWorld(RenderList output, BatchingOptions options, JobHandle parentJob)
        {
            JobHandle prepareChunks = JobScheduler.Schedule(new PrepareChunks(_manager, options, output), parentJob, JobPriority.Realtime, true);
            JobHandle combineGridBatches = JobScheduler.Schedule(new RenderList.CombineGridBatches(output), prepareChunks, JobPriority.Realtime, true);

            _activeJobs.Add(combineGridBatches);
            return combineGridBatches;
        }

        public BatchingGlobalOptions Options => _options;

        private readonly record struct PrepareChunks(RenderingManager Manager, BatchingOptions Options, RenderList RenderList) : IJob
        {
            public void Execute()
            {
                using (new ProfilingScope("PrepareChunks"))
                {
                    BatchingManager batchingManager = Manager.Services.GetService<BatchingManager>();

                    float viewDistanceSqr = Options.MaxViewingDistance * Options.MaxViewingDistance;

                    using RentedList<JobHandle> scheduledJobs = new RentedList<JobHandle>();
                    foreach (var (_, chunk) in Manager.PartitioningManager.Regions)
                    {
                        if (chunk.IsEmpty)
                            continue;
                        if (Vector3.DistanceSquared(Options.Origin, chunk.CurrentExtents.Center) - chunk.CurrentExtents.SqrRadius > viewDistanceSqr)
                            continue;

                        FrustrumIntersection intersection = batchingManager._options.FrustrumCulling ? chunk.CurrentExtents.Intersect(Options.ViewFrustrum) : FrustrumIntersection.Inside;
                        if (intersection != FrustrumIntersection.Outside)
                        {
                            GridChunkBatcher chunkBatcher = batchingManager.GetGridChunkBatcher();
                            chunkBatcher.Manager = batchingManager;
                            chunkBatcher.Options = Options;
                            chunkBatcher.RenderList = RenderList;
                            chunkBatcher.GridChunk = chunk;
                            chunkBatcher.DoFineFrustrumCulling = batchingManager._options.ForceFrustrumCull || intersection == FrustrumIntersection.Partial;

                            if (batchingManager._options.ThreadedCulling)
                                scheduledJobs.Add(JobScheduler.Schedule(chunkBatcher, JobPriority.Realtime, true));
                            else
                                ((IJob)chunkBatcher).Execute();
                        }
                    }

                    JobScheduler.CombineAll(scheduledJobs.AsSpan()).WaitForCompletion();
                }
            }
        }
    }

    public readonly record struct BatchWorldSetup(PartitioningManager Octree, BatchingOptions Options);
}
