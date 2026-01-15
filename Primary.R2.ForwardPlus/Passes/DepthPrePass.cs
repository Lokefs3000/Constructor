using Primary.Rendering;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using System.Runtime.CompilerServices;

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
