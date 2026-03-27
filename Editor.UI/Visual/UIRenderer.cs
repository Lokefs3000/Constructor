using CommunityToolkit.HighPerformance;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Utility;
using Editor.UI.Visual;
using Editor.UI.Visual.Passes;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Timing;
using SharpGen.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Visual
{
    public sealed class UIRenderer
    {
        private readonly UIManager _manager;

        private UIGradientManager _gradientManager;

        private HashSet<IWindowHost> _hostsToRedraw;

        private Queue<DrawContext> _pooledDrawContexts;

        private Queue<UIWindowRedraw> _queuedWindowRedraws;
        private HashSet<UIDockHost> _uncompositedDockHosts;

        private List<UIWindowRedraw> _unbuiltDraws;

        internal UIRenderer(UIManager manager)
        {
            _manager = manager;

            _gradientManager = new UIGradientManager();

            _hostsToRedraw = new HashSet<IWindowHost>();

            _pooledDrawContexts = new Queue<DrawContext>();

            _queuedWindowRedraws = new Queue<UIWindowRedraw>();
            _uncompositedDockHosts = new HashSet<UIDockHost>();

            _unbuiltDraws = new List<UIWindowRedraw>();
        }

        internal void AddHostToRedrawQueue(IWindowHost host)
        {
            _hostsToRedraw.Add(host);
        }

        public void InstallRenderPasses(RenderPassManager passes)
        {
            passes.AddRenderPass<UIGenGradientsRenderPass>();
            passes.AddRenderPass<UIUpdateFontsRenderPass>();
            passes.AddRenderPass<UIWindowRenderPass>();
            passes.AddRenderPass<UICompositorRenderPass>();
            passes.AddRenderPass<UIOverlayRenderPass>();
        }

        public void UninstallRenderPasses(RenderPassManager passes)
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

            foreach (DrawContext ctx in _pooledDrawContexts)
            {
                ctx.Clear();
            }

            if (_hostsToRedraw.Count > 0)
            {
                using (new ProfilingScope("PrepareCmds"))
                {
                    Debug.Assert(_unbuiltDraws.Count == 0);

                    foreach (IWindowHost host in _hostsToRedraw)
                    {
                        PrepareWindowForDrawing(host.ActiveWindow!);

                        if (host is UIDockHost dockHost)
                        {
                            UIDockHost child = dockHost;
                            while (child.ParentHost != null)
                                child = child.ParentHost;

                            _uncompositedDockHosts.Add(child);
                        }
                    }
                }
            }
        }

        private void PrepareWindowForDrawing(UIWindow window)
        {
            using (new ProfilingScope(window.WindowTitle))
            {
                if (!_pooledDrawContexts.TryDequeue(out DrawContext context))
                    context = new DrawContext(new UIPainter(), new UIDrawBuilder(), new UIMeshBuilder());

                context.Clear();

                Boundaries invalidRegion = Boundaries.Clip(window.InvalidVisualRegion, new Boundaries(Vector2.Zero, window.ClientSize.AsVector2()));
                invalidRegion = new Boundaries(Vector2.Zero, window.ClientSize.AsVector2());

                ushort objectIndex = 0;
                RecursiveDraw(window.RootElement, 0);

                void RecursiveDraw(UIElement element, int zIndex)
                {
                    if (element.PixelCoordinates.IsIntersecting(invalidRegion))
                    {
                        element.DrawVisual(new UIPainterContext(context.Painter, (ushort)zIndex, objectIndex));
                    }

                    ++objectIndex;

                    if (element.Children.Count > 0)
                    {
                        ++zIndex;
                        foreach (UIElement child in element.Children)
                        {
                            if (element.ElementTreeBounds.IsIntersecting(invalidRegion))
                            {
                                RecursiveDraw(child, zIndex);
                            }
                        }
                    }

                }

                context.Painter.FinishDrawing();

                if (!context.Painter.Cmds.IsEmpty)
                    _unbuiltDraws.Add(new UIWindowRedraw(window, context, invalidRegion));
                else
                {
                    context.Clear();
                    _pooledDrawContexts.Enqueue(context);
                }
            }
        }

        internal bool TryDequeueQueuedWindow([NotNullWhen(true)] out UIWindowRedraw result)
        {
            if (_queuedWindowRedraws.TryDequeue(out result))
            {
                _pooledDrawContexts.Enqueue(result.Context);
                return true;
            }

            return false;
        }

        internal void BuildDrawCommands()
        {
            using (new ProfilingScope("BuildCmds"))
            {
                foreach (UIWindowRedraw redraw in _unbuiltDraws)
                {
                    redraw.Context.Builder.BuildDraws(redraw.Context.Painter, redraw.Context.Mesh);
                    redraw.Context.Painter.ClearStoredObjects();

                    if (!redraw.Context.Mesh.IsEmpty)
                        _queuedWindowRedraws.Enqueue(redraw);
                    else
                    {
                        redraw.Context.Clear();
                        _pooledDrawContexts.Enqueue(redraw.Context);
                    }
                }

                _unbuiltDraws.Clear();
            }
        }

        internal void ClearUncompositedHosts() => _uncompositedDockHosts.Clear();

        public UIGradientManager GradientManager => _gradientManager;

        public bool AreAnyWindowsQueued => _queuedWindowRedraws.Count > 0;
        public bool HasUnbuiltDraws => _unbuiltDraws.Count > 0;
        public bool HasUncompositedHosts => _uncompositedDockHosts.Count > 0;

        public int WindowQueueSize => _queuedWindowRedraws.Count;
        public int CompositerQueueSize => _uncompositedDockHosts.Count;

        internal HashSet<UIDockHost> UncompositedHosts => _uncompositedDockHosts;
    }

    public readonly record struct DrawContext(UIPainter Painter, UIDrawBuilder Builder, UIMeshBuilder Mesh)
    {
        public void Clear()
        {
            Painter.Clear();
            Builder.Clear();
            Mesh.Clear();
        }
    }

    public readonly record struct UIWindowRedraw(UIWindow Window, DrawContext Context, Boundaries Region);
}
