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

                {
                    Vector2 big = new Vector2(1336.0f, 726.0f);
                    Vector2 size = new Vector2(200.0f);
                    commandBuffer.SetScissorRect(new RHI.ScissorRect(50, (int)(big.Y - size.X - 50), (int)(50 + size.X), (int)(big.Y + size.Y + 50)));

                    bool r = _shadowBG.SetResource("txTexture", forward.Shadows.ShadowAtlas.DepthTexture!);
                    Debug.Assert(r);

                    commandBuffer.SetShader(_shadowBlit);
                    commandBuffer.SetBindGroups(_shadowBG);
                    commandBuffer.SetConstants(new ShadowBlitData { Offset = new Vector2(-1.0f) + (size * 1.5f) / big, Scale = size / big, Near = 0.1f, Far = 20.0f });
                    commandBuffer.CommitShaderResources();
                    commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(3));
                }
            }

            CommandBufferPool.Return(commandBuffer);
        }


        private record struct ShadowBlitData(Vector2 Offset, Vector2 Scale, float Near, float Far);
    }
}
