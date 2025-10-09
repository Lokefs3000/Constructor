using Primary.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Pass;
using Primary.Rendering.Raw;
using System.Diagnostics;

namespace Primary.Rendering.Forward
{
    public sealed class ForwardOpaquePass
    {
        private bool _disposedValue;

        public ForwardOpaquePass()
        {

        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void ExecutePass(RenderPass renderPass)
        {
            using (RasterPassDescription pass = renderPass.CreateRasterPass())
            {
                pass.SetThreadingPolicy(RenderPassThreadingPolicy.None);
                pass.SetFunction(PassFunction);
            }
        }

        public unsafe void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData)
        {
            using (new CommandBufferEventScope(commandBuffer, "ForwardRP - Opaque"))
            {
                RenderingManager manager = Engine.GlobalSingleton.RenderingManager;
                RenderBatcher batcher = manager.RenderBatcher;

                ForwardRenderPath renderPath = (ForwardRenderPath)manager.RenderPath;

                RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

                commandBuffer.ClearRenderTarget(viewportData.CameraRenderTarget, viewportData.Camera.ClearColor.AsVector4());
                commandBuffer.ClearDepthStencil(viewportData.CameraRenderTarget, RHI.ClearFlags.Depth);

                commandBuffer.SetRenderTarget(viewportData.CameraRenderTarget, true);
                commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 100000));

                Span<FlagRenderBatch> batches = batcher.UsedBatches;
                uint offsetInMatrixBuffer = 0;

                for (int i = 0; i < batches.Length; i++)
                {
                    FlagRenderBatch renderBatch = batches[i];
                    HandleRenderBatch(commandBuffer, batcher, renderPath, renderBatch, ref offsetInMatrixBuffer);
                }
            }
        }

        private unsafe void HandleRenderBatch(RasterCommandBuffer commandBuffer, RenderBatcher batcher, ForwardRenderPath path, FlagRenderBatch renderBatch, ref uint offsetInMatrixBuffer)
        {
            Span<RenderMeshBatchData> batchDatas = renderBatch.RenderMeshBatches;

            Debug.Assert(renderBatch.ShaderReference != null);
            ShaderAsset shader = renderBatch.ShaderReference!;

            commandBuffer.SetShader(shader);

            IRenderMeshSource? activeMeshSource = null;
            for (int i = 0; i < batchDatas.Length; i++)
            {
                RenderMeshBatchData batchData = batchDatas[i];
                if (batchData.BatchableFlags.Count == 0)
                    continue;

                Debug.Assert(batchData.Mesh != null);
                RawRenderMesh mesh = batchData.Mesh!;

                Span<BatchedRenderFlag> renderFlags = batchData.BatchableFlags.AsSpan();

                {
                    cbObjectDataStruct* mapPointer = (cbObjectDataStruct*)commandBuffer.Map(path.CbObjectData, RHI.MapIntent.Write, (ulong)sizeof(cbObjectDataStruct));
                    mapPointer->MatrixId = offsetInMatrixBuffer;
                    commandBuffer.Unmap(path.CbObjectData);
                }

                if (activeMeshSource != mesh.Source)
                {
                    activeMeshSource = mesh.Source;

                    Debug.Assert(activeMeshSource.VertexBuffer != null);
                    Debug.Assert(activeMeshSource.IndexBuffer != null);

                    commandBuffer.SetVertexBuffer(0, activeMeshSource.VertexBuffer!);
                    commandBuffer.SetIndexBuffer(activeMeshSource.IndexBuffer!);
                }

                MaterialAsset? material = batcher.GetMaterialFromIndex(renderFlags[0].MaterialIndex);
                if (material == null)
                {
                    throw new NotImplementedException();
                }

                if (material.BindGroup == null)
                    commandBuffer.SetBindGroups(path.BuffersBindGroup, path.LightingBindGroup);
                else
                    commandBuffer.SetBindGroups(material.BindGroup, path.BuffersBindGroup, path.LightingBindGroup);

                commandBuffer.CommitShaderResources();
                commandBuffer.DrawIndexedInstanced(new RHI.DrawIndexedInstancedArgs
                {
                    IndexCountPerInstance = mesh.IndexCount,
                    InstanceCount = (uint)(batchData.BatchableFlags.Count),
                    StartIndexLocation = mesh.IndexOffset,
                    BaseVertexLocation = (int)mesh.VertexOffset,
                    StartInstanceLocation = 0
                });

                offsetInMatrixBuffer += (uint)batchData.BatchableFlags.Count;
            }
        }
    }
}
