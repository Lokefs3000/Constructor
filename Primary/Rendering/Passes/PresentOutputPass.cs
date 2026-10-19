using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.RHI;

namespace Primary.Rendering.Passes
{
    internal sealed class PresentOutputPass : IRenderPass, IDisposable
    {
        private readonly ShaderAsset _finalBlitSwapChain;
        private readonly PropertyBlock _finalBlitSwapChainBlock;

        internal PresentOutputPass()
        {
            _finalBlitSwapChain = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Core/BlitToSwapChain.shader").WaitIfNotLoaded();
            _finalBlitSwapChainBlock = _finalBlitSwapChain.CreatePropertyBlock();
        }

        public void Dispose()
        {
            _finalBlitSwapChainBlock.Dispose();
            GC.SuppressFinalize(this);
        }

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            TextureHandle sourceTexture = frameGraph.HasContext<RenderCameraData>() ?
                frameGraph.GetContext<RenderCameraData>().ColorTexture :
                frameGraph.GetContext<RenderWindowData>().ColorTexture;

            RenderStateData stateData = frameGraph.GetContext<RenderStateData>();

            using (RasterPassBuilder desc = frameGraph.AddRasterPass("Present", out PassData passData))
            {
                passData.Source = sourceTexture;

                passData.Destination = desc.ImportTexture(stateData.SwapChain!.Backbuffers[stateData.SwapChain.CurrentBackbufferIndex]);
                passData.SwapChain = stateData.SwapChain;

                passData.Shader = _finalBlitSwapChain;
                passData.Block = _finalBlitSwapChainBlock;

                desc.Read(sourceTexture);
                desc.Write(passData.Destination);

                desc.SetFunction(PassFunction);
            }

            Engine.GlobalSingleton.RenderingManager.FrameGraph.UseSwapChain(stateData.SwapChain!);
        }

        private static void PassFunction(RasterPassContext context)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;
            PassData passData = context.GetPassData<PassData>()!;

            cmd.SetRenderTarget(0, passData.Destination);

            cmd.SetShader(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            passData.Block!.SetResource("txFinalTexture", passData.Source);
            cmd.DrawInstanced(3, 1, 0, 0);
        }

        private sealed class PassData : IPassData
        {
            public TextureHandle Source;

            public TextureHandle Destination;
            public RHISwapChain? SwapChain;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public void Clear()
            {
                Source = TextureHandle.Null;

                Destination = TextureHandle.Null;
                SwapChain = null;

                Shader = null;
                Block = null;
            }
        }
    }
}
