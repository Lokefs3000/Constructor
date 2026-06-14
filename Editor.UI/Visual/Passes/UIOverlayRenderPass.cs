using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;

namespace Editor.UI.Visual.Passes
{
    [RenderPassSetup(RunContext = RenderPassRunContext.PerWindow)]
    internal class UIOverlayRenderPass : IRenderPass
    {
        public UIOverlayRenderPass()
        {

        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            if (UIManager.Instance.ActiveHosts.Count > 0)
            {
                RenderWindowData windowData = context.Get<RenderWindowData>()!;
                using (RasterPassDescription desc = renderPass.SetupRasterPass("UI-Overlay", out GenericPassData _))
                {
                    foreach (IInterfaceHost host in UIManager.Instance.ActiveHosts)
                    {
                        if (host is IRenderableHost renderableHost && renderableHost.HostWindow != null)
                        {
                            desc.UseResource(FGResourceUsage.Write, windowData.ColorTexture);
                        }
                    }

                    desc.AllowPassCulling(false);
                    desc.SetRenderFunction<GenericPassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, GenericPassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;
            RenderWindowData windowData = context.Container.Get<RenderWindowData>()!;

            foreach (IInterfaceHost host in UIManager.Instance.ActiveHosts)
            {
                if (host is IRenderableHost renderableHost && renderableHost.HostWindow == windowData.Window && renderableHost.HostTexture != null)
                {
                    ReadOnlySpan<IWindowHost> hosts = ReadOnlySpan<IWindowHost>.Empty;

                    Int2 baseOffset = renderableHost.ContentMetrics.Position;
                    if (host is IWindowDockHost dockHost)
                    {
                        hosts = dockHost.Hosts;
                    }

                    RHITexture src = renderableHost.HostTexture;

                    if (src.Description.Width > windowData.ColorTexture.Description.Width - baseOffset.X || src.Description.Height > windowData.ColorTexture.Description.Height - baseOffset.Y)
                    {
                        UIManager.Logger?.Warning("Incompatible size between source and destination: {src} >? {dst} (offset: {off})", new Int2(src.Description.Width, src.Description.Height), new Int2(windowData.ColorTexture.Description.Width, windowData.ColorTexture.Description.Height), baseOffset);
                    }
                    else
                        cmd.Copy(new FGTextureCopyDesc((FrameGraphTexture)renderableHost.HostTexture, null, windowData.ColorTexture, (uint)baseOffset.X, (uint)baseOffset.Y, 0));

                    foreach (IWindowHost child in hosts)
                    {
                        RecursiveHostCopy(cmd, windowData.ColorTexture, child);
                    }
                }
            }

            void RecursiveHostCopy(RasterCommandBuffer cmd, FrameGraphTexture dest, IWindowHost host)
            {
                if (host.HostTexture != null)
                {
                    ReadOnlySpan<IWindowHost> hosts = ReadOnlySpan<IWindowHost>.Empty;

                    Int2 baseOffset = host.ContentMetrics.Position;
                    if (host is IWindowDockHost dockHost)
                    {
                        hosts = dockHost.Hosts;
                    }

                    RHITexture src = host.HostTexture;

                    if (src.Description.Width > windowData.ColorTexture.Description.Width - baseOffset.X || src.Description.Height > windowData.ColorTexture.Description.Height - baseOffset.Y)
                    {
                        UIManager.Logger?.Warning("Incompatible size between source and destination: {src} >? {dst} (offset: {off})", new Int2(src.Description.Width, src.Description.Height), new Int2(windowData.ColorTexture.Description.Width, windowData.ColorTexture.Description.Height), baseOffset);
                    }
                    else
                        cmd.Copy(new FGTextureCopyDesc((FrameGraphTexture)src, null, dest, (uint)baseOffset.X, (uint)baseOffset.Y, 0));

                    foreach (UIDockHost child in hosts)
                    {
                        RecursiveHostCopy(cmd, dest, child);
                    }
                }
            }
        }

        private class PassData : IPassData
        {
            public UIFontManager? FontManager;

            public void Clear()
            {
                FontManager = null;
            }
        }
    }
}
