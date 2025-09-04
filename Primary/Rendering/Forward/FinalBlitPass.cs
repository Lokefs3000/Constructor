using Primary.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Forward
{
    [RenderPassPriority(true, typeof(ForwardOpaquePass))]
    public sealed class FinalBlitPass : IRenderPass
    {
        private ShaderAsset _shadowBlit;
        private ShaderBindGroup _shadowBG;

        public FinalBlitPass()
        {
            _shadowBlit = AssetManager.LoadAsset<ShaderAsset>("Hidden/ShadowBlit")!;
            _shadowBG = _shadowBlit.CreateDefaultBindGroup();
        }

        public void CleanupFrame(IRenderPath path, RenderPassData passData) { }
        public void Dispose() { }
        public void PrepareFrame(IRenderPath path, RenderPassData passData) { }

        public void ExecutePass(IRenderPath path, RenderPassData passData)
        {
            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;
            ForwardRenderPath forward = (ForwardRenderPath)path;

            CommandBuffer commandBuffer = CommandBufferPool.Get();

            using (new CommandBufferEventScope(commandBuffer, "ForwardRP - Final blit"))
            {
                commandBuffer.SetRenderTarget(viewportData.BackBufferRenderTarget);
                commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 100000));

                Blitter.Blit(commandBuffer, viewportData.CameraRenderTarget.ColorTexture!);
            }

            CommandBufferPool.Return(commandBuffer);
        }


        private record struct ShadowBlitData(Vector2 Offset, Vector2 Scale, float Near, float Far);
    }
}
