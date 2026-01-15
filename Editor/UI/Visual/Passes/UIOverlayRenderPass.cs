using Primary.Rendering;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Editor.UI.Visual.Passes
{
    internal class UIOverlayRenderPass : IRenderPass
    {
        public UIOverlayRenderPass()
        {

        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            if (UIManager.Instance.ActiveHosts.Count > 0)
            {
                RenderCameraData cameraData = context.Get<RenderCameraData>()!;
                using (RasterPassDescription desc = renderPass.SetupRasterPass("UI-Overlay", out GenericPassData _))
                {
                    foreach (UIDockHost host in UIManager.Instance.ActiveHosts)
                    {
                        if (host.IsExternallyHosted)
                        {
                            desc.UseResource(FGResourceUsage.Write, cameraData.ColorTexture);
                        }
                        else
                            throw new NotImplementedException();
                    }

                    desc.AllowPassCulling(false);
                    desc.SetRenderFunction<GenericPassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, GenericPassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;
            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

            foreach (UIDockHost host in UIManager.Instance.ActiveHosts)
            {
                if (host.IsExternallyHosted)
                {
                    cmd.Copy(new FGTextureCopyDesc((FrameGraphTexture)host.HostTexture!, null, cameraData.ColorTexture, 0, 0, 0));
                }
                else
                    throw new NotImplementedException();
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
