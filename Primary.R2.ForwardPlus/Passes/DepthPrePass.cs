using Primary.Rendering2;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Data;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using Primary.Rendering2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.R2.ForwardPlus.Passes
{
    internal sealed class DepthPrePass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderStateData stateData = context.Get<RenderStateData>()!;
            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            ForwardPlusRenderPath renderPath = Unsafe.As<ForwardPlusRenderPath>(stateData.Path);

            using (RasterPassDescription desc = renderPass.SetupRasterPass("FP-DepthPrepass", out PassData data))
            {
                {
                    data.OutDepth = cameraData.DepthTexture;
                }

                desc.UseDepthStencil(cameraData.DepthTexture);
                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            cmd.SetDepthStencil(data.OutDepth);
        }

        private class PassData : IPassData
        {
            public FrameGraphTexture OutDepth;

            public void Clear()
            {
                OutDepth = FrameGraphTexture.Invalid;
            }
        }
    }
}
