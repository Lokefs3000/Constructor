using Primary.Assets;
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
    internal sealed class TestPass : IRenderPass
    {
        private static ShaderAsset2 _shader = AssetManager.LoadAsset<ShaderAsset2>("Engine/Shaders/ForwardPlus/Test.hlsl2", true);
        private static PropertyBlock _block = _shader.CreatePropertyBlock()!;

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderStateData stateData = context.Get<RenderStateData>()!;
            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            ForwardPlusRenderPath renderPath = Unsafe.As<ForwardPlusRenderPath>(stateData.Path);

            using (RasterPassDescription desc = renderPass.SetupRasterPass("FP-TestPass", out PassData data))
            {
                {
                    data.OutColor = cameraData.ColorTexture;
                }

                desc.UseRenderTarget(cameraData.ColorTexture);
                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;



            cmd.SetRenderTarget(0, data.OutColor);
            cmd.SetPipeline(_shader.GraphicsPipeline!);
            cmd.SetProperties(_block);

            cmd.DrawInstanced(new FGDrawInstancedDesc(3));
        }

        private class PassData : IPassData
        {
            public FrameGraphTexture OutColor;

            public void Clear()
            {
                OutColor = FrameGraphTexture.Invalid;
            }
        }
    }
}
