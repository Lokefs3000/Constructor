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
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using VoxelizationDemo.Rendering.Passes;

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

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderPathBlackboard pathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
            if (pathBlackboard.RenderList == null || pathBlackboard.LightCollector == null || !_lightHeatmap.IsLoaded)
                return;

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            using (RasterPassDescription desc = renderPass.SetupRasterPass("LightHeatmap", out PassData passData))
            {
                passData.OutColor = cameraData.ColorTexture;

                passData.Shader = _lightHeatmap;
                passData.Block = _lightHeatmapBlock;

                desc.UseRenderTarget(passData.OutColor);
                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            int outputWidth = passData.OutColor.Description.Width;
            int outputHeight = passData.OutColor.Description.Height;

            cmd.SetRenderTarget(0, passData.OutColor);
            cmd.SetViewport(0, new FGViewport(0, 0, outputWidth, outputHeight));

            cmd.SetPipeline(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            cmd.SetConstants(new Int2(outputWidth, outputHeight));

            cmd.DrawInstanced(new FGDrawInstancedDesc(3));
        }

        private sealed class PassData : IPassData
        {
            public FrameGraphTexture OutColor;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public void Clear()
            {
                OutColor = FrameGraphTexture.Invalid;

                Shader = null;
                Block = null;
            }
        }
    }
}
