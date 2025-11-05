using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Tree;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering
{
    internal sealed class RenderTreeCollector
    {
        private RenderTreeSubCollector[] _subCollectors;
        private Task?[] _temporaryTasks;
        private int _activeCollectors;

        private ConcurrentQueue<RenderTree> _queuedTrees;

        internal RenderTreeCollector()
        {
            _subCollectors = new RenderTreeSubCollector[4];
            _temporaryTasks = new Task?[_subCollectors.Length - 1];
            _activeCollectors = 0;

            _queuedTrees = new ConcurrentQueue<RenderTree>();

            for (int i = 0; i < _subCollectors.Length; i++)
            {
                _subCollectors[i] = new RenderTreeSubCollector();
            }
        }

        internal void CollectTrees(RenderBatcher batcher, RenderTreeManager treeManager)
        {
            using (new ProfilingScope("CollectTrees"))
            {
                int totalEntities = 0;

                _queuedTrees.Clear();
                foreach (RenderTree tree in treeManager.Trees.Values)
                {
                    _queuedTrees.Enqueue(tree);
                    totalEntities += tree.ContainedEntityCount;
                }

                if (_queuedTrees.IsEmpty)
                    return;

                int averageTreeEntityCount = (int)Math.Ceiling(totalEntities / (double)_queuedTrees.Count);
                int workerCount = _queuedTrees.Count == 0 ? 0 : Math.Min(averageTreeEntityCount / 900, TreeCVars.MaxWorkers);

                _activeCollectors = Math.Max(workerCount, 1);

                for (int i = 1; i < workerCount; i++)
                {
                    _temporaryTasks[i - 1] = _subCollectors[i].ExecuteAsTask(this, batcher, (byte)i);
                }

                _subCollectors[0].Execute(this, batcher, 0);

                if (workerCount > 0)
                {
                    using (new ProfilingScope("WaitTasks"))
                    {
                        Task.WaitAll(_temporaryTasks.AsSpan(0, workerCount - 1)!);
                        Array.Fill(_temporaryTasks, null);
                    }
                }
            }
        }

        internal bool TryPopPendingTree([NotNullWhen(true)] out RenderTree? tree) => _queuedTrees.TryDequeue(out tree);

        internal ReadOnlySpan<RenderTreeSubCollector> SubCollectors => _subCollectors.AsSpan(0, _activeCollectors);
    }
}
