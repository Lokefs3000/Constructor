using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
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
    public sealed class SetupDataPass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderPathBlackboard pathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
            BufferDataBlackboard bufferData = renderPass.Blackboard.Add<BufferDataBlackboard>();

            using (RasterPassDescription desc = renderPass.SetupRasterPass("SetupData", out PassData data))
            {
                data.RenderFlagBuffer = pathBlackboard.RenderList == null ? FrameGraphBuffer.Invalid : desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<RenderFlag>() * pathBlackboard.RenderList.TotalFlagCount),
                    Stride = Unsafe.SizeOf<RenderFlag>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.Structured | FGBufferUsage.Global
                }, "RenderFlagBuffer");

                data.WorldMatriciesBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<WorldMatrixData>(),
                    Stride = Unsafe.SizeOf<WorldMatrixData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.Global
                }, "WorldMatriciesBuffer");

                data.CameraDataBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<CameraData>(),
                    Stride = Unsafe.SizeOf<CameraData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.Global
                }, "CameraDataBuffer");

                data.PointLightsBuffer = pathBlackboard.LightCollector == null ? FrameGraphBuffer.Invalid : desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<PointLightData>() * (pathBlackboard.LightCollector.PointLights.Count + 1)),
                    Stride = Unsafe.SizeOf<PointLightData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.Structured | FGBufferUsage.Global
                }, "PointLights");

                data.RenderList = pathBlackboard.RenderList;
                data.LightCollector = pathBlackboard.LightCollector;

                if (!data.RenderFlagBuffer.IsNull)
                    desc.UseResource(FGResourceUsage.Write, data.RenderFlagBuffer);
                if (!data.PointLightsBuffer.IsNull)
                    desc.UseResource(FGResourceUsage.Write, data.PointLightsBuffer);

                desc.UseResource(FGResourceUsage.Write, data.WorldMatriciesBuffer);
                desc.UseResource(FGResourceUsage.Write, data.CameraDataBuffer);

                desc.SetRenderFunction<PassData>(PassFunction);

                // Setup blackboard with data
                bufferData.WorldMatriciesBuffer = data.WorldMatriciesBuffer;
                bufferData.CameraDataBuffer = data.CameraDataBuffer;
                bufferData.RenderFlagBuffer = data.RenderFlagBuffer;
                bufferData.PointLightsBuffer = data.PointLightsBuffer;
            }

            // Initialize global values
            ShaderGlobalsManager.SetGlobalBuffer("cbWorldMatricies", bufferData.WorldMatriciesBuffer);
            ShaderGlobalsManager.SetGlobalBuffer("cbCameraData", bufferData.CameraDataBuffer);

            if (!bufferData.RenderFlagBuffer.IsNull)
                ShaderGlobalsManager.SetGlobalBuffer("sbRenderFlags", bufferData.RenderFlagBuffer);
            if (!bufferData.RenderFlagBuffer.IsNull)
                ShaderGlobalsManager.SetGlobalBuffer("sbPointLights", bufferData.PointLightsBuffer);
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

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
            }

            {
                WorldMatrixData matrixData = new WorldMatrixData(cameraData.View, cameraData.Projection, cameraData.ViewProjection);
                cmd.Upload(passData.WorldMatriciesBuffer, matrixData);
            }

            {
                CameraData camera = new CameraData(cameraData.Transform.Transformation.Translation.AsVector4Unsafe(), cameraData.Transform.ForwardVector.AsVector4Unsafe(), cameraData.ZNear, cameraData.ZFar);
                cmd.Upload(passData.CameraDataBuffer, camera);
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

            public FrameGraphBuffer RenderFlagBuffer;
            public FrameGraphBuffer MaterialDataBuffer;

            public FrameGraphBuffer WorldMatriciesBuffer;
            public FrameGraphBuffer CameraDataBuffer;

            public FrameGraphBuffer PointLightsBuffer;

            public void Clear()
            {
                RenderList = null;
                LightCollector = null;

                RenderFlagBuffer = FrameGraphBuffer.Invalid;
                MaterialDataBuffer = FrameGraphBuffer.Invalid;

                WorldMatriciesBuffer = FrameGraphBuffer.Invalid;
                CameraDataBuffer = FrameGraphBuffer.Invalid;

                PointLightsBuffer = FrameGraphBuffer.Invalid;
            }
        }

        private readonly record struct WorldMatrixData(Matrix4x4 View, Matrix4x4 Projection, Matrix4x4 ViewProjection);
        private readonly record struct CameraData(Vector4 CameraPos, Vector4 CameraDir, float ZNear, float ZFar);
    }

    public sealed class BufferDataBlackboard : IBlackboardData
    {
        public FrameGraphBuffer RenderFlagBuffer;
        public FrameGraphBuffer WorldMatriciesBuffer;
        public FrameGraphBuffer CameraDataBuffer;
        public FrameGraphBuffer PointLightsBuffer;

        public void Clear()
        {
            RenderFlagBuffer = FrameGraphBuffer.Invalid;
            WorldMatriciesBuffer = FrameGraphBuffer.Invalid;
            CameraDataBuffer = FrameGraphBuffer.Invalid;
            PointLightsBuffer = FrameGraphBuffer.Invalid;
        }
    }
}
