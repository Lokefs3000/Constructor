using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using System.Runtime.CompilerServices;

namespace Primary.R2.ForwardPlus.Passes
{
    internal sealed class TestPass : IRenderPass
    {
        private static ShaderAsset _shader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/ForwardPlus/Test.hlsl2", true);
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
