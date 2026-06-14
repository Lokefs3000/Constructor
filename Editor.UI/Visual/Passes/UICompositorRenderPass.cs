using Primary.Assets;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual.Passes
{
    [RenderPassSetup(RunContext = RenderPassRunContext.PerWindow)]
    internal sealed class UICompositorRenderPass : IRenderPass
    {
        private ShaderAsset _blitShader;
        private PropertyBlock _dataBlock;

        public UICompositorRenderPass()
        {
            _blitShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Blit.shader", true);
            _dataBlock = _blitShader.CreatePropertyBlock()!;
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            UIRenderer renderer = UIManager.Instance.Renderer;
            if (renderer.HasUncompositedHosts)
            {
                RenderWindowData windowData = context.Get<RenderWindowData>()!;

                UIWindowRenderPass.BlackboardData? blackboardData = renderPass.Blackboard.Get<UIWindowRenderPass.BlackboardData>();
                if (blackboardData == null)
                {
                    renderer.ClearUncompositedHosts();
                    return;
                }

                if (!Array.Exists(blackboardData.Regions!, (x) => GetHostWindow(x.Host) == windowData.Window))
                    return;

                using (RasterPassDescription desc = renderPass.SetupRasterPass("UI-Composite", out PassData data))
                {
                    data.Renderer = renderer;

                    data.Shader = _blitShader;
                    data.DataBlock = _dataBlock;

                    data.Window = windowData.Window;

                    data.Regions = blackboardData.Regions;
                    data.RegionCount = blackboardData.RegionCount;

                    data.OutColor = windowData.ColorTexture;

                    for (int i = 0; i < data.RegionCount; ++i)
                    {
                        UICompositeRegion region = data.Regions![i];
                        desc.UseResource(FGResourceUsage.Read, region.Texture);
                    }

                    desc.UseRenderTarget(windowData.ColorTexture);

                    desc.AllowPassCulling(false);
                    desc.SetRenderFunction<PassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            cmd.ClearRenderTarget(data.OutColor, null);
            cmd.SetPipeline(data.Shader!);

            for (int i = 0; i < data.RegionCount; ++i)
            {
                UICompositeRegion region = data.Regions![i];

                if (GetHostWindow(region.Host) != data.Window)
                    continue;

                if (region.Host is UIDockHost dockHost)
                {
                    if (dockHost.IsExternallyHosted || dockHost.HostTexture != null)
                        cmd.SetRenderTarget(0, region.Host.HostTexture!);
                    else
                        throw new NotImplementedException();
                }
                else
                {
                    Window? window = region.Host.HostWindow;
                    if (window != null)
                        cmd.SetRenderTarget(0, data.OutColor);
                }

                Vector2 actualClientSize = region.Host.HostMetrics.Size.AsVector2();

                Vector2 offset = region.Region.Minimum / actualClientSize;
                Vector2 scale = region.Region.Size / actualClientSize;

                data.DataBlock!.SetResource("txTexture", region.Texture);

                cmd.SetProperties(data.DataBlock);
                cmd.SetConstants(new BlitData(offset, scale));
                cmd.DrawInstanced(new FGDrawInstancedDesc(3));
            }

            data.Renderer!.ClearUncompositedHosts();
        }

        private static Window? GetHostWindow(IWindowHost host)
        {
            IWindowHost? currentHost = host;
            do
            {
                Window? window = currentHost?.HostWindow;
                if (window != null)
                    return window;
            } while ((currentHost = (currentHost is IWindowDockHost dockHost ? dockHost.ParentHost : null)) != null);

            return null;
        }

        private class PassData : IPassData
        {
            public UIRenderer? Renderer;

            public ShaderAsset? Shader;
            public PropertyBlock? DataBlock;

            public Window? Window;

            public UICompositeRegion[]? Regions;
            public int RegionCount;

            public FrameGraphTexture OutColor;

            public void Clear()
            {
                Renderer = null;

                Shader = null;
                DataBlock = null;

                Window = null;

                Regions = null;
                RegionCount = 0;

                OutColor = FrameGraphTexture.Invalid;
            }
        }

        private readonly record struct BlitData(Vector2 Offset, Vector2 Scale);
    }
}
