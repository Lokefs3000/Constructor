using Primary.Common;
using Primary.Profiling;
using Primary.Rendering2.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Batching
{
    public sealed class BatchingManager
    {
        private readonly OctreeRenderBatcher[] _subBatchers;

        private List<RenderList> _activeLists;

        internal BatchingManager()
        {
            _subBatchers = [
                new OctreeRenderBatcher(0),
                new OctreeRenderBatcher(1),
                new OctreeRenderBatcher(2),
                new OctreeRenderBatcher(3),
                ];

            _activeLists = new List<RenderList>();
        }

        public RenderList CreateRenderList()
        {
            RenderList list = new RenderList(this);
            _activeLists.Add(list);

            return list;
        }

        public void BatchWorld(OctreeManager octree, RenderList output)
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
                    foreach (var kvp in octree.Regions)
                    {
                        _subBatchers[0].Execute(output, kvp.Value);
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

                    for (int i = 0; i < keys.Length; i++)
                    {
                        ref readonly RenderKey key = ref keys[i];
                        if (key.ShaderId != lastShaderIdx)
                        {
                            OctreeRenderBatcher batcher = _subBatchers[key.Batcher];
                            if (lastShaderIdx != ushort.MaxValue)
                                output.AddRange(batcher.Flags[key.Index].Material.Shader!, new ShaderKeyRange(previousLastIndex, i));

                            previousLastIndex = i;
                            lastShaderIdx = key.ShaderId;
                        }
                    }

                    if (lastShaderIdx != ushort.MaxValue && previousLastIndex != keys.Length)
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
}
