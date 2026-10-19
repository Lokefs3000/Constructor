using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Memory;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;

namespace VoxelizationDemo.Rendering.Passes
{
    public sealed class SetupDataPass : IRenderPass
    {
        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            RenderStageBlackboard pathBlackboard = frameGraph.GetBlackboard<RenderStageBlackboard>()!;
            BufferDataBlackboard bufferData = frameGraph.AddBlackboard<BufferDataBlackboard>();

            if (pathBlackboard.RenderList != null)
            {
                if (bufferData.MaterialOffsets.Length < pathBlackboard.RenderList.MaterialIds.Count)
                {
                    bufferData.MaterialOffsets = new uint[MemoryHelper.AlignUp(pathBlackboard.RenderList.MaterialIds.Count, 16)];
                }
            }

            using (RasterPassBuilder desc = frameGraph.AddRasterPass("SetupData", out PassData data))
            {
                data.RenderFlagBuffer = pathBlackboard.RenderList == null ? BufferHandle.Null : desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "RenderFlagBuffer",
                    ElementCount = pathBlackboard.RenderList.TotalFlagCount,
                    Stride = Unsafe.SizeOf<RenderFlag>(),
                    Target = FGBufferTarget.Structured
                });

                data.MaterialDataBuffer = pathBlackboard.RenderList == null ? BufferHandle.Null : desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "MaterialDataBuffer",
                    ElementCount = Math.Max(MemoryHelper.AlignUp((int)pathBlackboard.RenderList.MaterialByteSize, 4), 4),
                    Stride = 1,
                    Target = FGBufferTarget.Raw
                });

                data.WorldMatriciesBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "WorldMatriciesBuffer",
                    ElementCount = 1,
                    Stride = Unsafe.SizeOf<WorldMatrixData>(),
                    Target = FGBufferTarget.Constant
                });

                data.CameraDataBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "CameraDataBuffer",
                    ElementCount = 1,
                    Stride = Unsafe.SizeOf<CameraData>(),
                    Target = FGBufferTarget.Constant
                });

                data.PointLightsBuffer = pathBlackboard.LightCollector == null ? BufferHandle.Null : desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "PointLights",
                    ElementCount = pathBlackboard.LightCollector.PointLights.Count + 1,
                    Stride = Unsafe.SizeOf<PointLightData>(),
                    Target = FGBufferTarget.Structured
                });

                data.RenderList = pathBlackboard.RenderList;
                data.LightCollector = pathBlackboard.LightCollector;

                if (!data.RenderFlagBuffer.IsNull)
                    desc.Write(data.RenderFlagBuffer);
                if (!data.PointLightsBuffer.IsNull)
                    desc.Write(data.PointLightsBuffer);

                desc.Write(data.WorldMatriciesBuffer);
                desc.Write(data.CameraDataBuffer);

                desc.SetFunction(PassFunction);

                // Setup blackboard with data
                bufferData.WorldMatriciesBuffer = data.WorldMatriciesBuffer;
                bufferData.CameraDataBuffer = data.CameraDataBuffer;
                bufferData.RenderFlagBuffer = data.RenderFlagBuffer;
                bufferData.MaterialBufferData = data.MaterialDataBuffer;
                bufferData.PointLightsBuffer = data.PointLightsBuffer;
            }

            // Initialize global values
            ShaderGlobalsManager.SetGlobalBuffer("cbWorldMatricies", bufferData.WorldMatriciesBuffer);
            ShaderGlobalsManager.SetGlobalBuffer("cbCameraData", bufferData.CameraDataBuffer);

            if (!bufferData.RenderFlagBuffer.IsNull)
                ShaderGlobalsManager.SetGlobalBuffer("sbRenderFlags", bufferData.RenderFlagBuffer);
            if (!bufferData.MaterialBufferData.IsNull)
                ShaderGlobalsManager.SetGlobalBuffer("baMaterialData", bufferData.MaterialBufferData);

            if (!bufferData.PointLightsBuffer.IsNull)
                ShaderGlobalsManager.SetGlobalBuffer("sbPointLights", bufferData.PointLightsBuffer);
        }

        private static void PassFunction(RasterPassContext context)
        {
            PassData passData = context.GetPassData<PassData>()!;
            RasterCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.GetContext<RenderCameraData>()!;
            BufferDataBlackboard bufferData = context.GetBlackboard<BufferDataBlackboard>();

            if (passData.RenderList != null)
            {
                using (FGMappedSubresource<RenderFlag> mapped = cmd.Map<RenderFlag>(passData.RenderFlagBuffer))
                {
                    int flagBufferOffset = 0;
                    foreach (ShaderRenderBatcher renderBatcher in passData.RenderList.ShaderBatchers)
                    {
                        renderBatcher.Flags.CopyTo(mapped.Span[flagBufferOffset..]);
                        flagBufferOffset += renderBatcher.Flags.Length;
                    }
                }

                if (passData.RenderList.MaterialByteSize > 0)
                {
                    using (FGMappedSubresource<byte> mapped = cmd.Map<byte>(passData.MaterialDataBuffer))
                    {
                        int materialDataOffset = 0;
                        foreach (var (material, index) in passData.RenderList.MaterialIds)
                        {
                            bufferData.MaterialOffsets[index] = (uint)materialDataOffset;

                            if (material.IsLoaded)
                            {
                                material.PropertyBlock.CopyBlockDataTo(mapped.Span[materialDataOffset..]);
                                materialDataOffset += MemoryHelper.AlignUp(material.PropertyBlock.BlockSize, 4);
                            }
                        }
                    }
                }
            }

            {
                WorldMatrixData matrixData = new WorldMatrixData(cameraData.View, cameraData.Projection, cameraData.ViewProjection);
                cmd.UploadBuffer(passData.WorldMatriciesBuffer, 0, matrixData);
            }

            {
                CameraData camera = new CameraData(cameraData.Transform.Transformation.Translation.AsVector4Unsafe(), cameraData.Transform.ForwardVector.AsVector4Unsafe(), cameraData.ZNear, cameraData.ZFar);
                cmd.UploadBuffer(passData.CameraDataBuffer, 0, camera);
            }

            if (passData.LightCollector != null)
            {
                using (FGMappedSubresource<PointLightData> mapped = cmd.Map<PointLightData>(passData.PointLightsBuffer))
                {
                    passData.LightCollector.PointLights.AsSpan().CopyTo(mapped.Span);
                    mapped.Span[^1] = default;
                }
            }
        }

        private sealed class PassData : IPassData
        {
            public RenderList? RenderList;
            public LightCollector? LightCollector;

            public BufferHandle RenderFlagBuffer;
            public BufferHandle MaterialDataBuffer;

            public BufferHandle WorldMatriciesBuffer;
            public BufferHandle CameraDataBuffer;

            public BufferHandle PointLightsBuffer;

            public void Clear()
            {
                RenderList = null;
                LightCollector = null;

                RenderFlagBuffer = BufferHandle.Null;
                MaterialDataBuffer = BufferHandle.Null;

                WorldMatriciesBuffer = BufferHandle.Null;
                CameraDataBuffer = BufferHandle.Null;

                PointLightsBuffer = BufferHandle.Null;
            }
        }

        private readonly record struct WorldMatrixData(Matrix4x4 View, Matrix4x4 Projection, Matrix4x4 ViewProjection);
        private readonly record struct CameraData(Vector4 CameraPos, Vector4 CameraDir, float ZNear, float ZFar);
    }

    public sealed class BufferDataBlackboard : IBlackboardData
    {
        public BufferHandle RenderFlagBuffer;
        public BufferHandle MaterialBufferData;
        public BufferHandle WorldMatriciesBuffer;
        public BufferHandle CameraDataBuffer;
        public BufferHandle PointLightsBuffer;

        public uint[] MaterialOffsets = [];

        public void Clear()
        {
            RenderFlagBuffer = BufferHandle.Null;
            MaterialBufferData = BufferHandle.Null;
            WorldMatriciesBuffer = BufferHandle.Null;
            CameraDataBuffer = BufferHandle.Null;
            PointLightsBuffer = BufferHandle.Null;
        }
    }
}
