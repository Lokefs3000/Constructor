using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
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
        private UIFontAsset? _interFont;

        private HashSet<IRenderableHost> _hostsToRedraw;

        private Queue<DrawContext> _pooledDrawContexts;

        private Queue<HostRedrawData> _queuedHostRedraws;
        private HashSet<UIDockHost> _uncompositedDockHosts;

        private List<HostRedrawData> _unbuiltDraws;

        internal UIRenderer(UIManager manager)
        {
            _manager = manager;

            _gradientManager = new UIGradientManager();
            _interFont = null;

            _hostsToRedraw = new HashSet<IRenderableHost>();

            _pooledDrawContexts = new Queue<DrawContext>();

            _queuedHostRedraws = new Queue<HostRedrawData>();
            _uncompositedDockHosts = new HashSet<UIDockHost>();

            _unbuiltDraws = new List<HostRedrawData>();
        }

        internal void AddHostToRedrawQueue(IRenderableHost host)
        {
            _hostsToRedraw.Add(host);
        }

        internal void RemoveHostToRedrawQueue(IRenderableHost host)
        {
            _hostsToRedraw.Remove(host);
        }

        public void InstallRenderPasses(RenderPassManager passes)
        {
            passes.AddRenderPass<UIGenGradientsRenderPass>();
            passes.AddRenderPass<UIUpdateFontsRenderPass>();
            passes.AddRenderPass<UIWindowRenderPass>();
            //passes.AddRenderPass<UICompositorRenderPass>();
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

            if (_interFont == null)
                _interFont = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont");

            foreach (DrawContext ctx in _pooledDrawContexts)
            {
                ctx.Clear();
            }

            if (_hostsToRedraw.Count > 0)
            {
                using (new ProfilingScope("PrepareCmds"))
                {
                    Debug.Assert(_unbuiltDraws.Count == 0);

                    foreach (IRenderableHost host in _hostsToRedraw)
                    {
                        PrepareHostForDrawing(host);
                    }
                }
            }
        }

        private void PrepareHostForDrawing(IRenderableHost host)
        {
            using (new ProfilingScope("Host"))
            {
                if (!_pooledDrawContexts.TryDequeue(out DrawContext context))
                    context = new DrawContext(new UIPainter(), new UIDrawBuilder(), new UIMeshBuilder());

                context.Clear();

                Rect contentMetrics = host.ContentMetrics;

                Boundaries invalidRegion = Boundaries.Clip(host.InvalidVisualRegion, new Boundaries(Vector2.Zero, contentMetrics.Size.AsVector2()));
                invalidRegion = new Boundaries(Vector2.Zero, contentMetrics.Size.AsVector2());

                long drawTimestampStart = Stopwatch.GetTimestamp();

                ushort objectIndex = 1;

                if (host is IWindowHost windowHost)
                {
                    IRenderableWindow? window = windowHost.ActiveWindow as IRenderableWindow;
                    if (window != null)
                    {
                        RecursiveDraw(invalidRegion, window.RootElement, 0);
                        window.RootElement.RemoveStateFlags(UIStateFlags.InvalidVisual);
                    }
                }

                host.DrawVisual(new UIPainterContext(context.Painter, ushort.MaxValue, 0));
                host.RemoveStateFlags(UIStateFlags.InvalidVisual);

                void RecursiveDraw(Boundaries invalidRegion, UIElement element, int zIndex)
                {
                    UIPainterContext painter = new UIPainterContext(context.Painter, (ushort)zIndex, objectIndex);
                    if (element.PixelCoordinates.IsIntersecting(invalidRegion))
                    {
                        element.DrawVisual(painter);
                        element.ExecuteVisualMods(painter);
                    }

                    ++objectIndex;

                    if (element.Children.Count > 0)
                    {
                        bool hasScroll = element.ScrollPosition != Vector2.Zero;
                        if (hasScroll)
                        {
                            invalidRegion = Boundaries.Offset(invalidRegion, element.ScrollPosition);
                            painter.PushMatrix(Matrix3x2.CreateTranslation(-element.ScrollPosition));
                        }

                        if (element.ClipDescendents)
                            painter.PushClippingRect(element.PixelCoordinates);

                        ++zIndex;
                        foreach (UIElement child in element.Children)
                        {
                            if (!child.IsEnabled)
                                break;

                            if (Flags.HasFlag(child.StateFlags, UIStateFlags.InvalidVisual) || element.ElementTreeBounds.IsIntersecting(invalidRegion))
                            {
                                RecursiveDraw(invalidRegion, child, zIndex);
                                child.RemoveStateFlags(UIStateFlags.InvalidVisual);
                            }
                        }

                        if (element.ClipDescendents)
                            painter.PopClippingRect();
                        if (hasScroll)
                            painter.PopMatrix();
                    }
                }

                TimeSpan renderTimeTaken = Stopwatch.GetElapsedTime(drawTimestampStart);
                if (false)
                {
                    UIPainterContext painter = new UIPainterContext(context.Painter, ushort.MaxValue, ushort.MaxValue);

                    painter.DrawRect(new Boundaries(new Vector2(5.0f), new Vector2(135.0f, 65.0f)), UIPaint.FromColor(new Color(0.0f, 0.5f)));
                    painter.DrawText(new Vector2(10.0f), UIPaint.FromColor(Color.Red), TextBuilder.Default, _interFont?.FindStyle(), 0.8f, @$"DrawVisual: {(int)renderTimeTaken.TotalMilliseconds}ms
FrameIdx: {Time.FrameIndex}
Objects: o{objectIndex} c{context.Painter.Cmds.Length} s{context.Painter.Segments.Length}");
                }

                context.Painter.FinishDrawing();

                if (!context.Painter.Cmds.IsEmpty)
                    _unbuiltDraws.Add(new HostRedrawData(host, context, invalidRegion));
                else
                {
                    context.Clear();
                    _pooledDrawContexts.Enqueue(context);
                }
            }
        }

        internal bool TryDequeueQueuedWindow([NotNullWhen(true)] out HostRedrawData result)
        {
            if (_queuedHostRedraws.TryDequeue(out result))
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
                foreach (HostRedrawData redraw in _unbuiltDraws)
                {
                    redraw.Context.Builder.BuildDraws(redraw.Context.Painter, redraw.Context.Mesh);
                    redraw.Context.Painter.ClearStoredObjects();

                    if (!redraw.Context.Mesh.IsEmpty)
                        _queuedHostRedraws.Enqueue(redraw);
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

        public bool AreAnyHostsQueued => _queuedHostRedraws.Count > 0;
        public bool HasUnbuiltDraws => _unbuiltDraws.Count > 0;
        public bool HasUncompositedHosts => false;

        public int HostQueueSize => _queuedHostRedraws.Count;
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

    public readonly record struct HostRedrawData(IRenderableHost Host, DrawContext Context, Boundaries Region);
}
