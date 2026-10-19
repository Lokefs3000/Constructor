using System.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Profiling;
using Primary.Rendering.Configuration;
using Primary.Rendering.Passes;

namespace Primary.Rendering
{
    public sealed class GraphCompiler
    {
        private readonly FrameGraph _frameGraph;

        private readonly HashSet<RenderPassIndex> _seenPasses;
        private readonly Queue<RenderPassIndex> _topoQueue;
        private readonly List<int> _incomingEdges;
        private readonly List<RenderPassIndex> _passOrder;

        private readonly List<RenderPassIndex> _rasterPasses;
        private readonly List<RenderPassIndex> _computePasses;

        internal GraphCompiler(FrameGraph frameGraph)
        {
            _frameGraph = frameGraph;

            _seenPasses = new HashSet<RenderPassIndex>();
            _topoQueue = new Queue<RenderPassIndex>();
            _incomingEdges = new List<int>();
            _passOrder = new List<RenderPassIndex>();

            _rasterPasses = new List<RenderPassIndex>();
            _computePasses = new List<RenderPassIndex>();
        }

        internal void ClearDataAfterFrame()
        {
            _seenPasses.Clear();
            _topoQueue.Clear();
            _incomingEdges.Clear();
            _passOrder.Clear();

            _rasterPasses.Clear();
            _computePasses.Clear();
        }

        internal void CompileGraphForExecution(CompiledRenderPlan plan)
        {
            using (new ProfilingScope("GraphCompile"))
            {
                BuildEdges();
                TopologicalSort();
                CullPasses();
                GroupPasses();
                FlushEvents(plan);
            }
        }

        private void BuildEdges()
        {
            using (new ProfilingScope("BuildEdges"))
            {
                ROList<RenderPassIndex> renderPasses = _frameGraph.RenderPassManager.ActivePasses;

                foreach (RenderPassIndex renderPass in renderPasses)
                {
                    _seenPasses.Clear();
                    foreach (RenderPassIndex dependency in renderPass.Dependencies)
                    {
                        if (_seenPasses.Add(dependency))
                        {
                            dependency.AddSuccessor(renderPass);
                            renderPass.IncrementIncomingEdges();
                        }
                    }
                }
            }
        }

        private void TopologicalSort()
        {
            using (new ProfilingScope("TopoSort"))
            {
                ROList<RenderPassIndex> renderPasses = _frameGraph.RenderPassManager.ActivePasses;

                foreach (RenderPassIndex renderPass in renderPasses)
                {
                    _incomingEdges.Add(renderPass.IncomingEdgeCount);

                    if (renderPass.IncomingEdgeCount == 0)
                    {
                        _topoQueue.Enqueue(renderPass);
                    }
                }

                while (_topoQueue.TryDequeue(out RenderPassIndex? renderPass))
                {
                    _passOrder.Add(renderPass);

                    Span<int> incomingEdgeSpan = _incomingEdges.AsSpan();
                    foreach (RenderPassIndex successor in renderPass.Successors)
                    {
                        if (--incomingEdgeSpan[successor.Index] == 0)
                        {
                            _topoQueue.Enqueue(successor);
                        }
                    }
                }

                _incomingEdges.Clear();
                _topoQueue.Clear();

                Debug.Assert(_passOrder.Count == renderPasses.Count);
            }
        }

        private void CullPasses()
        {
            using (new ProfilingScope("Cull"))
            {
                if (_passOrder.Count == 0)
                    return;

                _passOrder[^1].MarkAlive();

                for (int i = _passOrder.Count - 1; i >= 0; --i)
                {
                    RenderPassIndex renderPass = _passOrder[i];
                    if (!renderPass.IsAlive)
                        continue;

                    foreach (RenderPassIndex dependency in renderPass.Dependencies)
                    {
                        dependency.MarkAlive();
                    }
                }
            }
        }

        private void GroupPasses()
        {
            using (new ProfilingScope("Group"))
            {
                foreach (RenderPassIndex renderPass in _passOrder)
                {
                    if (renderPass.IsAlive)
                    {
                        switch (renderPass.PassType)
                        {
                            case RenderPassType.Raster: _rasterPasses.Add(renderPass); break;
                            case RenderPassType.Compute: _rasterPasses.Add(renderPass); break;
                        }
                    }
                }
            }
        }

        private void FlushEvents(CompiledRenderPlan plan)
        {
            using (new ProfilingScope("Flush"))
            {
                FrameGraphOptions options = _frameGraph.Options;

                _seenPasses.Clear();
                foreach (RenderPassIndex renderPass in _passOrder)
                {
                    if (!_seenPasses.Add(renderPass))
                        continue;

                    if (renderPass.IsAlive)
                    {
                        CompiledRenderPass compiledRenderPass = plan.FindRenderPass(renderPass);
                        plan.AddEvent(RenderEventType.ExecuteDirect, compiledRenderPass);
                    }
                }
            }
        }
    }
}
