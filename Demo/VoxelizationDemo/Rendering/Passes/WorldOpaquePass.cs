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
using Primary.Rendering.Structures;

namespace VoxelizationDemo.Rendering.Passes
{
    public sealed class WorldOpaquePass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderPathBlackboard pathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
            if (pathBlackboard.RenderList == null)
                return;

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            using (RasterPassDescription desc = renderPass.SetupRasterPass("WorldOpaque", out PassData passData))
            {
                passData.RenderList = pathBlackboard.RenderList;

                passData.OutColor = cameraData.ColorTexture;
                passData.OutDepth = cameraData.DepthTexture;

                desc.UseRenderTarget(cameraData.ColorTexture);
                desc.UseDepthStencil(cameraData.DepthTexture);

                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            cmd.SetRenderTarget(0, passData.OutColor);
            cmd.SetDepthStencil(passData.OutDepth);

            MaterialAsset? currentMaterial = null;
            IRenderMeshSource? currentMeshSource = null;

            foreach (ShaderRenderBatcher renderBatcher in passData.RenderList!.ShaderBatchers)
            {
                cmd.SetPipeline(renderBatcher.ActiveShader!);

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

                        cmd.SetVertexBuffer((FrameGraphBuffer)currentMeshSource.VertexBuffer!);
                        cmd.SetIndexBuffer((FrameGraphBuffer)currentMeshSource.IndexBuffer!);
                    }
                    else
                    {
                        if (currentMeshSource == null)
                            continue;
                    }
             
                    cmd.SetConstants((uint)segment.FlagIndexStart);

                    ref readonly RenderMeshDrawArgs drawArgs = ref segment.Mesh.Args;
                    if (drawArgs.NeedsIndexedDraw)
                        cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(drawArgs.VertexOrIndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), drawArgs.IndexOffset, (int)drawArgs.VertexOffset));
                    else
                        cmd.DrawInstanced(new FGDrawInstancedDesc(drawArgs.VertexOrIndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), drawArgs.VertexOffset));
                }
            }
        }

        private sealed class PassData : IPassData
        {
            public RenderList? RenderList;

            public FrameGraphTexture OutColor;
            public FrameGraphTexture OutDepth;

            public void Clear()
            {
                RenderList = null;

                OutColor = FrameGraphTexture.Invalid;
                OutDepth = FrameGraphTexture.Invalid;
            }
        }
    }
}
