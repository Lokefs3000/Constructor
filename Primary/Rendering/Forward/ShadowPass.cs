using Primary.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Forward.Managers;
using Primary.Rendering.Raw;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Forward
{
    internal sealed class ShadowPass
    {
        private ShaderAsset _shadowPassShader;
        private ShaderBindGroup _shadowBindGroup;

        private RHI.Buffer _shadowData;

        public ShadowPass()
        {
            _shadowPassShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/ShadowPass.hlsl")!;
            _shadowBindGroup = _shadowPassShader.CreateDefaultBindGroup();

            _shadowData = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<ShadowData>(),
                Stride = (uint)Unsafe.SizeOf<ShadowData>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Default,
                Mode = RHI.BufferMode.None,
                Usage = RHI.BufferUsage.ConstantBuffer
            }, nint.Zero);

            _shadowData.Name = "ForwardRP - cbShadowData";
        }

        public void Dispose()
        {
            _shadowData.Dispose();
        }

        public void ExecutePass(RenderPass renderPass)
        {
            using (RasterPassDescription pass = renderPass.CreateRasterPass())
            {
                pass.SetThreadingPolicy(RenderPassThreadingPolicy.None);
                pass.SetFunction(PassFunction);
            }
        }

        private void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData)
        {
            RenderingManager manager = Engine.GlobalSingleton.RenderingManager;
            RenderBatcher batcher = manager.RenderBatcher;

            ForwardRenderPath forward = (ForwardRenderPath)manager.RenderPath;
            ShadowManager shadows = forward.Shadows;

            using (new CommandBufferEventScope(commandBuffer, "ForwardRP - Shadows"))
            {
                commandBuffer.SetDepthStencil(shadows.ShadowAtlas);
                commandBuffer.ClearDepthStencil(shadows.ShadowAtlas, RHI.ClearFlags.Depth);

                commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 10000, 10000));

                commandBuffer.SetShader(_shadowPassShader);
                commandBuffer.SetBindGroups(forward.BuffersBindGroup, forward.LightingBindGroup, _shadowBindGroup);

                Span<ShadowManager.FrameCasterData> casters = shadows.FrameCasters;
                for (int i = 0; i < casters.Length; i++)
                {
                    ref ShadowManager.FrameCasterData lightData = ref casters[i];
                    DrawCasterView(commandBuffer, forward, batcher, ref lightData);
                }
            }
        }

        private unsafe void DrawCasterView(RasterCommandBuffer commandBuffer, ForwardRenderPath path, RenderBatcher batcher, ref ShadowManager.FrameCasterData lightData)
        {
            //TODO: improve this so it uses a range-based approach instead of clumping them here instead and allow for global indexing!

            //ref WorldTransform transform = ref lightData.Entity.GetComponent<WorldTransform>();
            //ref Light spotLight = ref lightData.Entity.GetComponent<SpotLight>();

            {
                ShadowData* ptr = (ShadowData*)commandBuffer.Map(_shadowData, RHI.MapIntent.Write);
                if (ptr == null)
                {
                    throw new NotImplementedException("placeholder implement me!");
                }

                *ptr = new ShadowData(lightData.LightProjection, new Vector4(lightData.WorldPosition, lightData.IsPointLight ? 20.0f : -1.0f));
                commandBuffer.Unmap(_shadowData);
            }

            _shadowBindGroup.SetResource("cbShadow", _shadowData);
            commandBuffer.CommitShaderResources();

            commandBuffer.SetViewport(new RHI.Viewport(lightData.AtlasOffset.X, lightData.AtlasOffset.Y, lightData.AtlasResolution, lightData.AtlasResolution));

            ModelAsset? currentlyBoundAsset = null;

            uint offsetInMatrixBuffer = 0;

            Span<FlagRenderBatch> batches = batcher.UsedBatches;
            for (int i = 0; i < batches.Length; i++)
            {
                FlagRenderBatch batch = batches[i];

                Span<RenderMeshBatchData> batchDatas = batch.RenderMeshBatches;
                for (int j = 0; j < batchDatas.Length; j++)
                {
                    RenderMeshBatchData data = batchDatas[j];
                    RenderMesh mesh = data.Mesh!;

                    Debug.Assert(mesh != null);

                    {
                        cbObjectDataStruct* mapPointer = (cbObjectDataStruct*)commandBuffer.Map(path.CbObjectData, RHI.MapIntent.Write, (ulong)sizeof(cbObjectDataStruct));
                        mapPointer->MatrixId = offsetInMatrixBuffer;
                        commandBuffer.Unmap(path.CbObjectData);
                    }

                    if (currentlyBoundAsset != mesh.Model)
                    {
                        commandBuffer.SetVertexBuffer(0, mesh.Model.VertexBuffer!);
                        commandBuffer.SetIndexBuffer(mesh.Model.IndexBuffer);

                        currentlyBoundAsset = mesh.Model;
                    }

                    commandBuffer.DrawIndexedInstanced(new RHI.DrawIndexedInstancedArgs(mesh.IndexCount, mesh.IndexOffset, (int)mesh.VertexOffset, (uint)data.BatchableFlags.Count));
                    offsetInMatrixBuffer += (uint)data.BatchableFlags.Count;
                }
            }
        }

        private record struct ShadowData(Matrix4x4 LightProjection, Vector4 LightPos_FarPlane);
    }
}
