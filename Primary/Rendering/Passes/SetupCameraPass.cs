using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Data;
using Primary.RHI;

namespace Primary.Rendering.Passes
{
    internal class SetupCameraPass : IRenderPass
    {
        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            using (RasterPassBuilder builder = frameGraph.AddRasterPass("SetupCamera"))
            {
                RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>();
                
                if (cameraData.OutputData.TargetTexture == null)
                {
                    cameraData.ColorTexture = builder.CreateTexture(new FGTextureDesc(cameraData.ClientSize.X, cameraData.ClientSize.Y, RHIFormat.RGB10A2_UNorm, FGTextureTarget.RenderTarget, "CamColor"));
                }
                else
                {
                    cameraData.ColorTexture = builder.ImportTexture(cameraData.OutputData.TargetTexture);
                }

                cameraData.DepthTexture = builder.CreateTexture(new FGTextureDesc(cameraData.ClientSize.X, cameraData.ClientSize.Y, RHIFormat.D24_UNorm_S8_UInt, FGTextureTarget.DepthStencil, "CamDepth"));

                builder.SetFunction(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
        }
    }
}
