using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering.Tree;

namespace Primary.Rendering.Batching
{
    public sealed class BatchingManager
    {
        private readonly RenderingManager _manager;
        private readonly OctreeRenderBatcher[] _subBatchers;

        private List<RenderList> _activeLists;

        internal BatchingManager(RenderingManager manager)
        {
            _manager = manager;
            _subBatchers = [
                new OctreeRenderBatcher(0, manager),
                new OctreeRenderBatcher(1, manager),
                new OctreeRenderBatcher(2, manager),
                new OctreeRenderBatcher(3, manager),
                ];

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
                    foreach (OctreeRenderBatcher batcher in _subBatchers)
                    {
                        batcher.ClearFrameData();
                    }

                    //TODO: multithreading
                    foreach (var (_, tree) in setup.Octree.Regions)
                    {
                        _subBatchers[0].Execute(output, tree, setup.CullingFrustrum);
                    }
                }

                int totalKeys = 0;
                Span<RenderKey> keys = default;

                using (new ProfilingScope("Group"))
                {
                    foreach (OctreeRenderBatcher batcher in _subBatchers)
                    {
                        totalKeys += batcher.Keys.Length;
                    }

                    keys = output.RentKeys(totalKeys);

                    Span<RenderKey> tempKeys = keys;
                    foreach (OctreeRenderBatcher batcher in _subBatchers)
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
                                output.AddRange(batcher.Flags[keys[previousLastIndex].Index].Material.Shader!, new ShaderKeyRange(previousLastIndex, i));

                            previousLastIndex = i;
                            lastShaderIdx = key.ShaderId;
                        }
                    }

                    if (lastShaderIdx != ushort.MaxValue && previousLastIndex < keys.Length)
                    {
                        ref readonly RenderKey key = ref keys[previousLastIndex];
                        OctreeRenderBatcher batcher = _subBatchers[key.Batcher];
                        output.AddRange(batcher.Flags[key.Index].Material.Shader!, new ShaderKeyRange(previousLastIndex, keys.Length));
                    }
                }

                using (new ProfilingScope("Batch"))
                {
                    output.SetupShaderBatchers();
                    output.ExecuteActiveBatchers(_subBatchers);
                }

                output.ReturnKeys();
            }
        }
    }

    public readonly record struct BatchWorldSetup(OctreeManager Octree, Frustrum CullingFrustrum);
}
