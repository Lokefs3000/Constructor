using Primary.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Pass;
using Primary.Rendering.Raw;
using System.Diagnostics;

namespace Primary.Rendering.Forward.Debugging
{
    internal class SDebugOpaquePass
    {
        private ShaderAsset? _unlitShader;
        private ShaderAsset? _lightingOnlyShader;
        private ShaderAsset? _detailLightingShader;
        private ShaderAsset? _wireframeShader;
        private ShaderAsset? _normalsShader;
        private ShaderAsset? _overdrawShader;

        internal SDebugOpaquePass()
        {
            _unlitShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Debug/Unlit.hlsl");
            _lightingOnlyShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Debug/LightingOnly.hlsl");
            _detailLightingShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Debug/DetailLighting.hlsl");
            _wireframeShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Debug/Wireframe.hlsl");
            _normalsShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Debug/Normals.hlsl");
            _overdrawShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Debug/Overdraw.hlsl");
        }

        public void Dispose() { }

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
            using (new CommandBufferEventScope(commandBuffer, "ForwardRP - DebugVM"))
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
                    HandleRenderBatch(commandBuffer, manager, batcher, renderPath, renderBatch, ref offsetInMatrixBuffer);
                }
            }
        }

        private unsafe void HandleRenderBatch(RasterCommandBuffer commandBuffer, RenderingManager manager, RenderBatcher batcher, ForwardRenderPath path, FlagRenderBatch renderBatch, ref uint offsetInMatrixBuffer)
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

            IRenderMeshSource? activeSource = null;
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

                if (activeSource != mesh.Source)
                {
                    activeSource = mesh.Source;

                    Debug.Assert(activeSource.VertexBuffer != null);
                    Debug.Assert(activeSource.IndexBuffer != null);

                    commandBuffer.SetVertexBuffer(0, activeSource.VertexBuffer!);
                    commandBuffer.SetIndexBuffer(activeSource.IndexBuffer!);
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
