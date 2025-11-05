using Primary.Assets;
using Primary.Profiling;
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

        public unsafe void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData, object? userArg)
        {
            using (new ProfilingScope("Fwd-OpaquePass"))
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

                    ReadOnlySpan<ShaderRenderBatch> batches = batcher.UsedBatches;
                    uint offsetInMatrixBuffer = 0;
                    
                    for (int i = 0; i < batches.Length; i++)
                    {
                        ShaderRenderBatch renderBatch = batches[i];
                        HandleRenderBatch(commandBuffer, batcher, renderPath, renderBatch, ref offsetInMatrixBuffer);
                    }
                }
            }
        }

        private unsafe void HandleRenderBatch(RasterCommandBuffer commandBuffer, RenderBatcher batcher, ForwardRenderPath path, ShaderRenderBatch renderBatch, ref uint offsetInMatrixBuffer)
        {
            Debug.Assert(renderBatch.ShaderReference != null);
            ShaderAsset shader = renderBatch.ShaderReference!;
            
            commandBuffer.SetShader(shader);

            Span<BatchedRenderFlag> renderFlags = renderBatch.RenderFlags;
            Span<BatchedSegment> segments = renderBatch.Segments;

            IRenderMeshSource? oldSource = null;
            MaterialAsset? oldMaterial = null;

            for (int i = 0; i < segments.Length; i++)
            {
                ref BatchedSegment segment = ref segments[i];
                
                if (oldSource != segment.Mesh.Source)
                {
                    oldSource = segment.Mesh.Source;

                    Debug.Assert(oldSource.VertexBuffer != null);
                    Debug.Assert(oldSource.IndexBuffer != null);

                    commandBuffer.SetVertexBuffer(0, oldSource.VertexBuffer!);
                    commandBuffer.SetIndexBuffer(oldSource.IndexBuffer!);
                }

                if (oldMaterial != segment.Material)
                {
                    oldMaterial = segment.Material;

                    if (oldMaterial.BindGroup == null)
                        commandBuffer.SetBindGroups(path.BuffersBindGroup, path.LightingBindGroup);
                    else
                        commandBuffer.SetBindGroups(oldMaterial.BindGroup, path.BuffersBindGroup, path.LightingBindGroup);
                    commandBuffer.CommitShaderResources();
                }

                {
                    cbObjectDataStruct* mapPointer = (cbObjectDataStruct*)commandBuffer.Map(path.CbObjectData, RHI.MapIntent.Write, (ulong)sizeof(cbObjectDataStruct));
                    mapPointer->MatrixId = offsetInMatrixBuffer;
                    commandBuffer.Unmap(path.CbObjectData);
                }

                int count = segment.IdxEnd - segment.IdxStart;
                Debug.Assert(count > 0);

                commandBuffer.DrawIndexedInstanced(new RHI.DrawIndexedInstancedArgs
                {
                    IndexCountPerInstance = segment.Mesh.IndexCount,
                    InstanceCount = (uint)count,
                    StartIndexLocation = segment.Mesh.IndexOffset,
                    BaseVertexLocation = (int)segment.Mesh.VertexOffset,
                    StartInstanceLocation = 0
                });

                offsetInMatrixBuffer += (uint)count;
            }
        }
    }
}
