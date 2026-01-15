using Editor.UI.Elements;
using Editor.UI.Visual.Passes;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual
{
    public sealed class UIRenderer
    {
        private readonly UIManager _manager;

        private UIGradientManager _gradientManager;

        private HashSet<UIDockHost> _hostsToRedraw;

        private ConcurrentQueue<UICommandBuffer> _pooledCmdBuffers;
        private ConcurrentQueue<UIBakedCommandBuffer> _pooledBakedCmdBuffers;

        private ConcurrentQueue<UIWindowRedraw> _queuedWindowRedraws;
        private HashSet<UIDockHost> _uncompositedDockHosts;

        private int _threadsWaitingOnCompute;
        private ManualResetEventSlim _waitForFinalComputeEvent;

        internal UIRenderer(UIManager manager)
        {
            _manager = manager;

            _gradientManager = new UIGradientManager();

            _hostsToRedraw = new HashSet<UIDockHost>();

            _pooledCmdBuffers = new ConcurrentQueue<UICommandBuffer>();
            _pooledBakedCmdBuffers = new ConcurrentQueue<UIBakedCommandBuffer>();

            _queuedWindowRedraws = new ConcurrentQueue<UIWindowRedraw>();
            _uncompositedDockHosts = new HashSet<UIDockHost>();

            _threadsWaitingOnCompute = 0;
            _waitForFinalComputeEvent = new ManualResetEventSlim(false);
        }

        internal void AddHostToRedrawQueue(UIDockHost host)
        {
            _hostsToRedraw.Add(host);
        }

        internal void InstallRenderPasses(RenderPassManager passes)
        {
            passes.AddRenderPass<UIGenGradientsRenderPass>();
            passes.AddRenderPass<UIUpdateFontsRenderPass>();
            passes.AddRenderPass<UIWindowRenderPass>();
            passes.AddRenderPass<UICompositorRenderPass>();
            passes.AddRenderPass<UIOverlayRenderPass>();
        }

        internal void UninstallRenderPasses(RenderPassManager passes)
        {
            passes.RemoveRenderPass<UIGenGradientsRenderPass>();
            passes.RemoveRenderPass<UIUpdateFontsRenderPass>();
            passes.RemoveRenderPass<UIWindowRenderPass>();
            passes.RemoveRenderPass<UICompositorRenderPass>();
            passes.RemoveRenderPass<UIOverlayRenderPass>();
        }

        public void PrepareForRendering()
        {
            _gradientManager.ClearPreviousData();

            if (_hostsToRedraw.Count > 0)
            {
                using (new ProfilingScope("UIPrepareDraw"))
                {
                    if (_hostsToRedraw.Count == 1 || true)
                    {
                        foreach (UIDockHost host in _hostsToRedraw)
                        {
                            //host.RemoveInvalidFlags(UIInvalidationFlags.Visual);

                            PrepareWindowForDrawing(host.ActiveWindow!, true);

                            UIDockHost child = host;
                            while (child.ParentHost != null)
                                child = child.ParentHost;
                            _uncompositedDockHosts.Add(child);
                        }
                    }
                    else
                    {
                        using RentedArray<Task> tasks = RentedArray<Task>.Rent(_hostsToRedraw.Count, true);

                        _waitForFinalComputeEvent.Reset();
                        _threadsWaitingOnCompute = 0;

                        int taskIndex = 0;
                        foreach (UIDockHost host in _hostsToRedraw)
                        {
                            host.RemoveInvalidFlags(UIInvalidationFlags.Visual);
                            tasks[taskIndex] = Task.Factory.StartNew(() => PrepareWindowForDrawing(host.ActiveWindow!, false));

                            UIDockHost hostCopy = host;
                            while (hostCopy.ParentHost != null)
                                hostCopy = hostCopy.ParentHost;
                            _uncompositedDockHosts.Add(hostCopy);
                        }

                        //TODO: implement better system that also wont get struck if a task throws
                        while (_threadsWaitingOnCompute < tasks.Count)
                            Thread.Yield();

                        _waitForFinalComputeEvent.Set();

                        Task.WaitAll(tasks.Span);
                    }

                    _hostsToRedraw.Clear();
                }
            }
        }

        private void PrepareWindowForDrawing(UIWindow window, bool isSinglethreaded)
        {
            using (new ProfilingScope("UIGenCmds"))
            {
                window.RemoveInvalidFlags(UIInvalidationFlags.Visual);

                if (!_pooledCmdBuffers.TryDequeue(out UICommandBuffer? commandBuffer))
                    commandBuffer = new UICommandBuffer(this);

                Queue<UIElement> drawQueue = new Queue<UIElement>();
                drawQueue.Enqueue(window.RootElement);

                //find all drawable regions
                {
                    Queue<UIElement> searchQueue = new Queue<UIElement>();
                    Stack<UIElement> bottomUpStack = new Stack<UIElement>();

                    searchQueue.Enqueue(window.RootElement);
                    while (searchQueue.TryDequeue(out UIElement? element))
                    {
                        bottomUpStack.Push(element);

                        if (element.Children.Count > 0)
                        {
                            foreach (UIElement child in element.Children)
                            {
                                if (true || FlagUtility.HasFlag(child.InvalidFlags, UIInvalidationFlags.Visual))
                                {
                                    searchQueue.Enqueue(child);
                                }
                            }
                        }
                    }

                    while (bottomUpStack.TryPop(out UIElement? element))
                    {
                        Boundaries bounds = element.InvalidVisualRegion;
                        if (element.Children.Count > 0)
                        {
                            foreach (UIElement child in element.Children)
                            {
                                if (true || FlagUtility.HasFlag(child.InvalidFlags, UIInvalidationFlags.Visual))
                                    bounds = Boundaries.Combine(bounds, child.InvalidVisualRegion);
                            }
                        }

                        element.InvalidVisualRegion = bounds;
                    }
                }

                Boundaries invalidRegion = window.InvalidVisualRegion;
                commandBuffer.ClearCommands(invalidRegion);

                while (drawQueue.TryDequeue(out UIElement? element))
                {
                    element.RemoveInvalidFlag(UIInvalidationFlags.Visual);

                    if (element.DrawVisual(commandBuffer) && element.Children.Count > 0)
                    {
                        foreach (UIElement child in element.Children)
                        {
                            if (true || FlagUtility.HasFlag(child.InvalidFlags, UIInvalidationFlags.Visual))
                                drawQueue.Enqueue(child);
                        }
                    }
                }

                if (!_pooledBakedCmdBuffers.TryDequeue(out UIBakedCommandBuffer? bakedCommandBuffer))
                    bakedCommandBuffer = new UIBakedCommandBuffer();

                if (!isSinglethreaded)
                {
                    Interlocked.Increment(ref _threadsWaitingOnCompute);
                    _waitForFinalComputeEvent.Wait();
                }
                else
                {
                    _gradientManager.ComputeGradientLayouts();
                }

                bakedCommandBuffer.Bake(this, commandBuffer);

                _queuedWindowRedraws.Enqueue(new UIWindowRedraw(window, bakedCommandBuffer, invalidRegion));

                commandBuffer.ClearCommands(Boundaries.Zero);
                _pooledCmdBuffers.Enqueue(commandBuffer);
            }
        }

        internal bool TryDequeueQueuedWindow([NotNullWhen(true)] out UIWindowRedraw result)
        {
            if (_queuedWindowRedraws.TryDequeue(out result))
            {
                _pooledBakedCmdBuffers.Enqueue(result.CommandBuffer);
                return true;
            }

            return false;
        }

        internal void ClearUncompositedHosts()
        {
            _uncompositedDockHosts.Clear();
        }

        public UIGradientManager GradientManager => _gradientManager;

        public bool AreAnyWindowsQueued => _queuedWindowRedraws.Count > 0;
        public bool HasUncompositedHosts => _uncompositedDockHosts.Count > 0;

        public int WindowQueueSize => _queuedWindowRedraws.Count;
        public int CompositerQueueSize => _uncompositedDockHosts.Count;

        internal HashSet<UIDockHost> UncompositedHosts => _uncompositedDockHosts;

        private readonly record struct SearchElementData(UIElement Element, Boundaries VisualRegion);
    }

    public readonly record struct UIWindowRedraw(UIWindow Window, UIBakedCommandBuffer CommandBuffer, Boundaries Region);
}
