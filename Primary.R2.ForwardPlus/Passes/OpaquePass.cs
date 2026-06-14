using Primary.Assets;
using Primary.Components;
using Primary.R2.ForwardPlus.Statistics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using System.Runtime.CompilerServices;

namespace Primary.R2.ForwardPlus.Passes
{
    internal sealed class OpaquePass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderStateData stateData = context.Get<RenderStateData>()!;
            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            ForwardPlusRenderPath renderPath = Unsafe.As<ForwardPlusRenderPath>(stateData.Path);
            GenericResources? resources = renderPass.Blackboard.Get<GenericResources>();

            if (resources == null)
                return;

            using (RasterPassDescription desc = renderPass.SetupRasterPass("FP-Opaque", out PassData data))
            {
                {
                    data.RtColor = cameraData.ColorTexture;
                    data.DsDepth = cameraData.DepthTexture;

                    data.DynamicDataBuffer = resources.DynamicDataBuffer;

                    data.RenderList = renderPath.PrimaryRenderList;

                    data.Statistics = renderPath.Statistics;
                }

                desc.UseResource(FGResourceUsage.ReadWrite, resources.DynamicDataBuffer);

                desc.UseRenderTarget(cameraData.ColorTexture);
                desc.UseDepthStencil(cameraData.DepthTexture);

                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

            cmd.ClearRenderTarget(data.RtColor, cameraData.CameraEntity.GetComponent<Camera>().ClearColor);

            cmd.SetRenderTarget(0, data.RtColor);
            cmd.SetDepthStencil(data.DsDepth);

            MaterialAsset? lastMaterialAsset = null;
            IRenderMeshSource? lastRenderMeshSource = null;
 
            RenderList list = data.RenderList!;
            foreach (ShaderRenderBatcher batch in list.ShaderBatchers)
            {
                cmd.SetPipeline(batch.ActiveShader!);

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

                        RHIBuffer? vertexBuffer = lastRenderMeshSource.VertexBuffer;
                        if (vertexBuffer == null)
                            continue;

                        cmd.SetVertexBuffer(new FGSetBufferDesc(vertexBuffer));
                        
                        if (segment.Mesh.HasIndices)
                        {
                            RHIBuffer? indexBuffer = lastRenderMeshSource.IndexBuffer;
                            if (indexBuffer == null)
                                continue;

                            cmd.SetIndexBuffer(new FGSetBufferDesc(indexBuffer));
                        }
                    }
                    
                    cmd.Upload(data.DynamicDataBuffer, new DynamicDataData((uint)segment.FlagIndexStart));
                    
                    RawRenderMesh renderMesh = segment.Mesh;
                    if (renderMesh.HasIndices)
                    {
                        cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(
                            renderMesh.IndexCount,
                            (uint)(segment.FlagIndexEnd - segment.FlagIndexStart),
                            renderMesh.IndexOffset,
                            (int)renderMesh.VertexOffset));
                    }
                    else
                    {
                        cmd.DrawInstanced(new FGDrawInstancedDesc(
                            renderMesh.IndexCount,
                            (uint)(segment.FlagIndexEnd - segment.FlagIndexStart),
                            renderMesh.VertexOffset));
                    }
                }

                data.Statistics!.Draw.OpaqueDrawCalls += batch.Segments.Length;
                data.Statistics!.Draw.OpaqueFlagCount += batch.Flags.Length;
            }

            data.Statistics!.Draw.OpaqueShaderCount += list.ShaderBatchers.Length;
        }

        private class PassData : IPassData
        {
            public FrameGraphTexture RtColor;
            public FrameGraphTexture DsDepth;

            public FrameGraphBuffer DynamicDataBuffer;

            public RenderList? RenderList;

            public RenderPathStatistics? Statistics;

            public void Clear()
            {
                RtColor = FrameGraphTexture.Invalid;
                DsDepth = FrameGraphTexture.Invalid;

                DynamicDataBuffer = FrameGraphBuffer.Invalid;

                RenderList = null;

                Statistics = null;
            }
        }
    }
}
