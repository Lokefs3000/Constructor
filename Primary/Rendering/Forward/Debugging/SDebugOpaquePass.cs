using Primary.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Forward.Debugging
{
    internal class SDebugOpaquePass : IRenderPass
    {
        private ShaderAsset? _unlitShader;
        private ShaderAsset? _lightingOnlyShader;
        private ShaderAsset? _detailLightingShader;
        private ShaderAsset? _wireframeShader;
        private ShaderAsset? _normalsShader;
        private ShaderAsset? _overdrawShader;

        internal SDebugOpaquePass()
        {
            _unlitShader = AssetManager.LoadAsset<ShaderAsset>("Hidden/Debug/Unlit");
            _lightingOnlyShader = AssetManager.LoadAsset<ShaderAsset>("Hidden/Debug/LightingOnly");
            _detailLightingShader = AssetManager.LoadAsset<ShaderAsset>("Hidden/Debug/DetailLighting");
            _wireframeShader = AssetManager.LoadAsset<ShaderAsset>("Hidden/Debug/Wireframe");
            _normalsShader = AssetManager.LoadAsset<ShaderAsset>("Hidden/Debug/Normals");
            _overdrawShader = AssetManager.LoadAsset<ShaderAsset>("Hidden/Debug/Overdraw");
        }

        public void Dispose() { }

        public unsafe void ExecutePass(IRenderPath path, RenderPassData passData)
        {
            CommandBuffer commandBuffer = CommandBufferPool.Get();

            using (new CommandBufferEventScope(commandBuffer, "ForwardRP - DebugVM"))
            {
                ForwardRenderPath renderPath = (ForwardRenderPath)path;

                RenderingManager manager = Engine.GlobalSingleton.RenderingManager;
                RenderBatcher batcher = manager.RenderBatcher;

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
                    HandleRenderBatch(commandBuffer, manager, batcher, renderPath, renderBatch, ref offsetInMatrixBuffer);
                }
            }

            CommandBufferPool.Return(commandBuffer);
        }

        private unsafe void HandleRenderBatch(CommandBuffer commandBuffer, RenderingManager manager, RenderBatcher batcher, ForwardRenderPath path, FlagRenderBatch renderBatch, ref uint offsetInMatrixBuffer)
        {
            Span<RenderMeshBatchData> batchDatas = renderBatch.RenderMeshBatches;

            ref RenderingConfig rconfig = ref manager.Configuration;

            ShaderAsset? shader = null;
            switch (rconfig.RenderMode)
            {
                case RenderingMode.Unlit:
                    {
                        shader = _unlitShader;
                        break;
                    }
                case RenderingMode.Wireframe:
                    {
                        shader = _wireframeShader;
                        break;
                    }
                case RenderingMode.Normals:
                    {
                        shader = _normalsShader;
                        break;
                    }
                case RenderingMode.Lighting:
                    {
                        shader = _lightingOnlyShader;
                        break;
                    }
                case RenderingMode.DetailLighting:
                    {
                        shader = _detailLightingShader;
                        break;
                    }
                case RenderingMode.Reflections:
                    {
                        break;
                    }
                case RenderingMode.ShaderComplexity:
                    {
                        break;
                    }
                case RenderingMode.Overdraw:
                    {
                        shader = _overdrawShader;
                        break;
                    }
                default: return;
            }

            if (shader == null || shader.Status != ResourceStatus.Success)
                return;

            commandBuffer.SetShader(shader);

            ModelAsset? activeModel = null;
            for (int i = 0; i < batchDatas.Length; i++)
            {
                RenderMeshBatchData batchData = batchDatas[i];
                if (batchData.BatchableFlags.Count == 0)
                    continue;

                Debug.Assert(batchData.Mesh != null);
                RenderMesh mesh = batchData.Mesh!;

                Span<BatchedRenderFlag> renderFlags = batchData.BatchableFlags.AsSpan();

                {
                    cbObjectDataStruct* mapPointer = (cbObjectDataStruct*)commandBuffer.Map(path.CbObjectData, RHI.MapIntent.Write, (ulong)sizeof(cbObjectDataStruct));
                    mapPointer->MatrixId = offsetInMatrixBuffer;
                    commandBuffer.Unmap(path.CbObjectData);
                }

                if (activeModel != mesh.Model)
                {
                    activeModel = mesh.Model;

                    Debug.Assert(activeModel.VertexBuffer != null);
                    Debug.Assert(activeModel.IndexBuffer != null);

                    commandBuffer.SetVertexBuffer(0, activeModel.VertexBuffer!);
                    commandBuffer.SetIndexBuffer(activeModel.IndexBuffer!);
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

        public void CleanupFrame(IRenderPath path, RenderPassData passData) { }
        public void PrepareFrame(IRenderPath path, RenderPassData passData) { }
    }
}
