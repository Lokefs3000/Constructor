using Primary.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Forward
{
    public sealed class FinalBlitPass
    {
        internal FinalBlitPass()
        {

        }

        public void ExecutePass(RenderPass renderPass)
        {
            using (RasterPassDescription pass = renderPass.CreateRasterPass())
            {
                pass.SetThreadingPolicy(RenderPassThreadingPolicy.None);
                pass.SetFunction(PassFunction);
            }
        }

        private void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData)
        {
            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;
            ForwardRenderPath forward = (ForwardRenderPath)Engine.GlobalSingleton.RenderingManager.RenderPath;

            using (new CommandBufferEventScope(commandBuffer, "ForwardRP - Final blit"))
            {
                commandBuffer.SetRenderTarget(viewportData.BackBufferRenderTarget);
                commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 100000));

                Blitter.Blit(commandBuffer, viewportData.CameraRenderTarget.ColorTexture!);
            }
        }

        private record struct ShadowBlitData(Vector2 Offset, Vector2 Scale, float Near, float Far);
    }
}
