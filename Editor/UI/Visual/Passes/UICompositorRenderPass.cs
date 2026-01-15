using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual.Passes
{
    internal sealed class UICompositorRenderPass : IRenderPass
    {
        private ShaderAsset _blitShader;
        private PropertyBlock _dataBlock;

        public UICompositorRenderPass()
        {
            _blitShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Blit.hlsl2", true);
            _dataBlock = _blitShader.CreatePropertyBlock()!;
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            UIRenderer renderer = UIManager.Instance.Renderer;
            if (renderer.HasUncompositedHosts)
            {
                RenderCameraData cameraData = context.Get<RenderCameraData>()!;

                UIWindowRenderPass.BlackboardData blackboardData = renderPass.Blackboard.Get<UIWindowRenderPass.BlackboardData>()!;

                using (RasterPassDescription desc = renderPass.SetupRasterPass("UI-Composite", out PassData data))
                {
                    data.Renderer = renderer;

                    data.Shader = _blitShader;
                    data.DataBlock = _dataBlock;

                    data.Regions = blackboardData.Regions;
                    data.RegionCount = blackboardData.RegionCount;

                    data.OutColor = cameraData.ColorTexture;

                    bool hasHostWindowAlready = false;
                    for (int i = 0; i < data.RegionCount; ++i)
                    {
                        UICompositeRegion region = data.Regions![i];
                        desc.UseResource(FGResourceUsage.Read, region.Texture);

                        if (!hasHostWindowAlready)
                        {
                            UIDockHost? host = region.Host;
                            while (host != null && !host.IsExternallyHosted)
                                host = host.ParentHost;

                            if (host != null)
                            {
                                //desc.UseRenderTarget(host.HostTexture!);
                                desc.UseResource(FGResourceUsage.Write, cameraData.ColorTexture);

                                hasHostWindowAlready = true;
                            }    
                        }
                    }

                    desc.UseRenderTarget(cameraData.ColorTexture);

                    desc.AllowPassCulling(false);
                    desc.SetRenderFunction<PassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            cmd.SetPipeline(data.Shader!.GraphicsPipeline!);

            for (int i = 0; i < data.RegionCount; ++i)
            {
                UICompositeRegion region = data.Regions![i];

                if (region.Host.IsExternallyHosted)
                    cmd.SetRenderTarget(0, region.Host.HostTexture!);
                else
                    throw new NotImplementedException();

                Vector2 actualClientSize;
                {
                    UIDockHost host = region.Host;
                    while (host.ParentHost != null)
                        host = host.ParentHost;

                    actualClientSize = host.ClientSize;
                }

                Vector2 offset = region.Region.Minimum / actualClientSize;
                Vector2 scale = region.Region.Size / actualClientSize;

                data.DataBlock!.SetResource(s_pbTexture, region.Texture);

                cmd.SetProperties(data.DataBlock);
                cmd.SetConstants(new BlitData(offset, scale));
                cmd.DrawInstanced(new FGDrawInstancedDesc(3));
            }

            data.Renderer!.ClearUncompositedHosts();
        }

        private readonly static int s_pbTexture = PropertyBlock.GetID("txTexture");

        private class PassData : IPassData
        {
            public UIRenderer? Renderer;

            public ShaderAsset? Shader;
            public PropertyBlock? DataBlock;

            public UICompositeRegion[]? Regions;
            public int RegionCount;

            public FrameGraphTexture OutColor;

            public void Clear()
            {
                Renderer = null;

                Shader = null;
                DataBlock = null;

                Regions = null;
                RegionCount = 0;

                OutColor = FrameGraphTexture.Invalid;
            }
        }

        private readonly record struct BlitData(Vector2 Offset, Vector2 Scale);
    }
}
