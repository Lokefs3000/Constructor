using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.RHI;
using VoxelizationDemo.Rendering.Passes;
using Primary.Rendering.Datatypes;

namespace VoxelizationDemo.Rendering.Diagnostics
{
    public sealed class LightHeatmapPass : IRenderPass
    {
        private readonly ShaderAsset _lightHeatmap;
        private readonly PropertyBlock _lightHeatmapBlock;

        public LightHeatmapPass()
        {
            _lightHeatmap = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Diagnostic/LightHeatmap.shader");
            _lightHeatmapBlock = _lightHeatmap.CreatePropertyBlock();
        }

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            RenderStageBlackboard pathBlackboard = frameGraph.GetBlackboard<RenderStageBlackboard>()!;
            if (pathBlackboard.RenderList == null || pathBlackboard.LightCollector == null || !_lightHeatmap.IsLoaded)
                return;

            RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>()!;

            using (RasterPassBuilder desc = frameGraph.AddRasterPass("LightHeatmap", out PassData passData))
            {
                passData.OutColor = cameraData.ColorTexture;

                passData.Shader = _lightHeatmap;
                passData.Block = _lightHeatmapBlock;

                desc.Write(passData.OutColor);
                desc.SetFunction(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
            PassData passData = context.GetPassData<PassData>()!;
            RasterCommandBuffer cmd = context.CommandBuffer;

            ref readonly FGTextureDesc outColorDesc = ref context.GetDescription(passData.OutColor);
            int outputWidth = outColorDesc.Width;
            int outputHeight = outColorDesc.Height;

            cmd.SetRenderTarget(0, passData.OutColor);
            cmd.SetViewport(0, new FGViewport(0, 0, outputWidth, outputHeight));

            cmd.SetShader(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            cmd.SetConstants(new Int2(outputWidth, outputHeight));

            cmd.DrawInstanced(3, 1, 0, 0);
        }

        private sealed class PassData : IPassData
        {
            public TextureHandle OutColor;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public void Clear()
            {
                OutColor = TextureHandle.Null;

                Shader = null;
                Block = null;
            }
        }
    }
}
