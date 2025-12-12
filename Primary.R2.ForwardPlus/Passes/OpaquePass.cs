using Primary.Rendering.Data;
using Primary.Rendering2;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Batching;
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
    internal sealed class OpaquePass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderStateData stateData = context.Get<RenderStateData>()!;
            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            ForwardPlusRenderPath renderPath = Unsafe.As<ForwardPlusRenderPath>(stateData.Path);

            using (RasterPassDescription desc = renderPass.SetupRasterPass("Opaque", out PassData data))
            {
                {
                    data.RtColor = cameraData.ColorTexture;
                    data.DsDepth = cameraData.DepthTexture;

                    data.RenderList = renderPath.PrimaryRenderList;
                }

                desc.UseRenderTarget(cameraData.ColorTexture);
                desc.UseDepthStencil(cameraData.DepthTexture);

                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            cmd.SetRenderTarget(0, data.RtColor);
            cmd.SetDepthStencil(data.DsDepth);

            MaterialAsset2? lastMaterialAsset = null;
            IRenderMeshSource? lastRenderMeshSource = null;

            RenderList list = data.RenderList!;
            foreach (ShaderRenderBatcher batch in list.ShaderBatchers)
            {
                cmd.SetPipeline(batch.ActiveShader!.GraphicsPipeline!);

                foreach (ref readonly RenderSegment segment in batch.Segments)
                {
                    if (lastMaterialAsset != segment.Material)
                    {
                        lastMaterialAsset = segment.Material;
                        cmd.SetProperties(lastMaterialAsset.PropertyBlock);
                    }

                    if (lastRenderMeshSource != segment.Mesh.Source)
                    {
                        lastRenderMeshSource = segment.Mesh.Source;
                        cmd.SetVertexBuffer(new FGSetBufferDesc(lastRenderMeshSource.VertexBuffer!));
                        cmd.SetIndexBuffer(new FGSetBufferDesc(lastRenderMeshSource.IndexBuffer!));
                    }

                    RawRenderMesh renderMesh = segment.Mesh;
                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(
                        renderMesh.IndexCount,
                        (uint)(segment.FlagIndexStart - segment.FlagIndexEnd),
                        renderMesh.IndexOffset,
                        (int)renderMesh.VertexOffset));
                }
            }
        }

        private class PassData : IPassData
        {
            public FrameGraphTexture RtColor;
            public FrameGraphTexture DsDepth;

            public RenderList? RenderList;

            public void Clear()
            {
                RtColor = FrameGraphTexture.Invalid;
                DsDepth = FrameGraphTexture.Invalid;

                RenderList = null;
            }
        }
    }
}
