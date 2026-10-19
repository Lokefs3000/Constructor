using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Data;
using Primary.RHI;

namespace Primary.Rendering.Passes
{
    internal class SetupWindowPass : IRenderPass
    {
        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            using (RasterPassBuilder builder = frameGraph.AddRasterPass("SetupCamera"))
            {
                RenderWindowData windowData = frameGraph.GetContext<RenderWindowData>();
                
                if (windowData.ColorTexture.IsNull)
                {
                    windowData.ColorTexture = builder.CreateTexture(new FGTextureDesc(windowData.Window.ClientSize.X, windowData.Window.ClientSize.Y, RHIFormat.RGB10A2_UNorm, FGTextureTarget.RenderTarget, "WndColor"));
                }

                builder.SetFunction(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
        }
    }
}
