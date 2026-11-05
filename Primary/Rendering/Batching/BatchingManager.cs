using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Rendering.Tree;
using Primary.Threading;

namespace Primary.Rendering.Batching
{
    public sealed class BatchingManager
    {
        private readonly RenderingManager _manager;
        private readonly List<OctreeRenderBatcher> _subBatchers;
        private int _subBatchersIndex;

        private List<RenderList> _activeLists;

        internal BatchingManager(RenderingManager manager)
        {
            _manager = manager;
            _subBatchers = new List<OctreeRenderBatcher>();
            _subBatchersIndex = 0;

            _activeLists = new List<RenderList>();
        }

        public RenderList CreateRenderList()
        {
            RenderList list = new RenderList(this);
            _activeLists.Add(list);

            return list;
        }

        public void BatchWorld(RenderList output, in BatchWorldSetup setup)
        {
            using (new ProfilingScope("BatchWorld"))
            {
                using (new ProfilingScope("Dispatch"))
                {
                    output.ClearFrameData();
                    foreach (OctreeRenderBatcher batcher in _subBatchers.AsSpan()[.._subBatchersIndex])
                    {
                        batcher.ClearFrameData();
                    }

                    BatchingOptions options = setup.Options;

                    using RentedList<JobHandle> jobs = new RentedList<JobHandle>();

                    _subBatchersIndex = 0;
                    foreach (var (_, tree) in setup.Octree.Regions)
                    {
                        OctreeRenderBatcher renderBatcher;
                        if (_subBatchersIndex >= _subBatchers.Count)
                        {
                            renderBatcher = new OctreeRenderBatcher((byte)_subBatchersIndex, _manager);
                            _subBatchers.Add(renderBatcher);
                        }
                        else
                        {
                            renderBatcher = _subBatchers[_subBatchersIndex];
                        }

                        renderBatcher.AddToQueue(output, tree, options);

                        JobHandle job = JobScheduler.Schedule(renderBatcher, JobPriority.Realtime);
                        JobScheduler.Flush(job);

                        jobs.Add(job);

                        ++_subBatchersIndex;
                    }

                    if (!jobs.IsEmpty)
                    {
                        if (jobs.Count == 1)
                            jobs[0].WaitForCompletion();
                        else
                            JobScheduler.CombineAll(jobs.AsSpan()).WaitForCompletion();
                    }

                    output.FinishDispatch(options.ShaderType);
                }

                int totalKeys = 0;
                Span<RenderKey> keys = default;

                using (new ProfilingScope("Group"))
                {
                    foreach (OctreeRenderBatcher batcher in _subBatchers.AsSpan()[.._subBatchersIndex])
                    {
                        totalKeys += batcher.Keys.Length;
                    }

                    keys = output.RentKeys(totalKeys);

                    Span<RenderKey> tempKeys = keys;
                    foreach (OctreeRenderBatcher batcher in _subBatchers.AsSpan()[.._subBatchersIndex])
                    {
                        batcher.Keys.CopyTo(tempKeys);
                        tempKeys = tempKeys.Slice(batcher.Keys.Length);
                    }
                }

                using (new ProfilingScope("Sort"))
                {
                    keys.Sort();
                }

                using (new ProfilingScope("FindShaders"))
                {
                    int previousLastIndex = 0;
                    ushort lastShaderIdx = ushort.MaxValue;

                    for (int i = 0; i < keys.Length; ++i)
                    {
                        ref readonly RenderKey key = ref keys[i];
                        if (key.ShaderId != lastShaderIdx)
                        {
                            OctreeRenderBatcher batcher = _subBatchers[key.Batcher];
                            if (lastShaderIdx != ushort.MaxValue)
                                output.AddRange(batcher.Flags[keys[previousLastIndex].Index].Material.GetShaderOrNull(setup.Options.ShaderType)!, new ShaderKeyRange(previousLastIndex, i));

                            previousLastIndex = i;
                            lastShaderIdx = key.ShaderId;
                        }
                    }

                    if (lastShaderIdx != ushort.MaxValue && previousLastIndex < keys.Length)
                    {
                        ref readonly RenderKey key = ref keys[previousLastIndex];
                        OctreeRenderBatcher batcher = _subBatchers[key.Batcher];
                        output.AddRange(batcher.Flags[key.Index].Material.GetShaderOrNull(setup.Options.ShaderType)!, new ShaderKeyRange(previousLastIndex, keys.Length));
                    }
                }

                using (new ProfilingScope("Batch"))
                {
                    output.SetupShaderBatchers();
                    output.ExecuteActiveBatchers(_subBatchers.AsSpan()[.._subBatchersIndex]);
                }

                output.ReturnKeys();
            }
        }
    }

    public readonly record struct BatchWorldSetup(OctreeManager Octree, BatchingOptions Options);
}
