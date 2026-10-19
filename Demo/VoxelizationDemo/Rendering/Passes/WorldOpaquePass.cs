using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Commands;

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

            MaterialAsset? currentMaterial = null;
            IRenderMeshSource? currentMeshSource = null;

            int currentAppendOffset = 0;
            foreach (ShaderRenderBatcher renderBatcher in passData.RenderList!.ShaderBatchers)
            {
                cmd.SetShader(renderBatcher.ActiveShader!);

                foreach (RenderSegment segment in renderBatcher.Segments)
                {
                    if (!segment.Material.IsLoaded)
                        continue;

                    if (currentMaterial != segment.Material)
                    {
                        currentMaterial = segment.Material;
                        cmd.SetProperties(segment.Material.PropertyBlock);
                    }

                    if (currentMeshSource != segment.Mesh.MeshSource)
                    {
                        currentMeshSource = segment.Mesh.MeshSource;
                        if (currentMeshSource == null)
                            continue;

                        cmd.SetVertexBuffer(currentMeshSource.VertexBuffer);
                        cmd.SetIndexBuffer(currentMeshSource.IndexBuffer);
                    }
                    else
                    {
                        if (currentMeshSource == null)
                            continue;
                    }

                    ConstantsData constantsData = new ConstantsData(
                        (uint)(segment.FlagIndexStart + currentAppendOffset),
                        bufferData.MaterialOffsets[segment.MaterialIndex]);
                    cmd.SetConstants(constantsData);

                    ref readonly RenderMeshDrawArgs drawArgs = ref segment.Mesh.Args;
                    if (drawArgs.NeedsIndexedDraw)
                        cmd.DrawIndexedInstanced(drawArgs.VertexOrIndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), drawArgs.IndexOffset, (int)drawArgs.VertexOffset, 0);
                    else
                        cmd.DrawInstanced(drawArgs.VertexOrIndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), drawArgs.VertexOffset, 0);
                }

                currentAppendOffset += renderBatcher.Flags.Length;
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
