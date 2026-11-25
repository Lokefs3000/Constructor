using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using TerraFX.Interop.Windows;

namespace VoxelizationDemo.Rendering.Passes
{
    public sealed class WorldOpaquePass : IRenderPass
    {
        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            RenderStageBlackboard pathBlackboard = frameGraph.GetBlackboard<RenderStageBlackboard>()!;
            if (pathBlackboard.RenderList == null)
                return;

            RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>()!;

            using (RasterPassBuilder desc = frameGraph.AddRasterPass("WorldOpaque", out PassData passData))
            {
                passData.RenderList = pathBlackboard.RenderList;

                passData.OutColor = cameraData.ColorTexture;
                passData.OutDepth = cameraData.DepthTexture;

                desc.Write(cameraData.ColorTexture);
                desc.Write(cameraData.DepthTexture);

                desc.SetFunction(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
            PassData passData = context.GetPassData<PassData>()!;
            RasterCommandBuffer cmd = context.CommandBuffer;

            BufferDataBlackboard bufferData = context.GetBlackboard<BufferDataBlackboard>();

            cmd.ClearRenderTarget(passData.OutColor);
            cmd.ClearDepthStencil(passData.OutDepth);

            cmd.SetRenderTarget(0, passData.OutColor);
            cmd.SetDepthStencil(passData.OutDepth);

            ShaderAsset? currentShader = null;
            ROPropertyBlock currentMaterial = null;
            ModelAsset? currentModel = null;

            foreach (RenderListRange range in passData.RenderList!.Ranges)
            {
                if (currentShader != range.Pass.Shader)
                {
                    currentShader = range.Pass.Shader;
                    if (currentShader == null || !currentShader.IsReady)
                    {
                        currentShader = null;
                        continue;
                    }

                    cmd.SetShader(currentShader);
                }

                if (currentMaterial != range.Pass.Properties)
                {
                    currentMaterial = range.Pass.Properties;
                    if (currentMaterial.IsNull)
                    {
                        currentMaterial = null;
                        continue;
                    }

                    cmd.SetProperties(range.Pass.Properties);
                }

                if (currentModel != range.Model)
                {
                    currentModel = range.Model;
                    if (!currentModel.IsReady)
                    {
                        currentModel = null;
                        continue;
                    }

                    IRenderMeshSource source = range.Model.MeshSource;
                    cmd.SetVertexBuffer(source.VertexBuffer);
                    cmd.SetIndexBuffer(source.IndexBuffer);
                }

                ConstantsData constantsData = new ConstantsData(
                    (uint)range.MatrixOffset,
                    bufferData.MaterialOffsets[range.Pass.Material]);

                cmd.SetConstants(constantsData);

                ref readonly RenderMeshDrawArgs drawArgs = ref range.Mesh.Args;
                if (drawArgs.NeedsIndexedDraw)
                    cmd.DrawIndexedInstanced(drawArgs.VertexOrIndexCount, (uint)range.InstanceCount, drawArgs.IndexOffset, (int)drawArgs.VertexOffset, 0);
                else
                    cmd.DrawInstanced(drawArgs.VertexOrIndexCount, (uint)range.InstanceCount, drawArgs.VertexOffset, 0);
            }
        }

        private sealed class PassData : IPassData
        {
            public RenderList? RenderList;

            public TextureHandle OutColor;
            public TextureHandle OutDepth;

            public void Clear()
            {
                RenderList = null;

                OutColor = TextureHandle.Null;
                OutDepth = TextureHandle.Null;
            }
        }

        private readonly record struct ConstantsData(uint FlagOffset, uint MaterialOffset);
    }
}
