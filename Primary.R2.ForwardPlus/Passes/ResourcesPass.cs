using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.R2.ForwardPlus.Passes
{
    internal sealed class ResourcesPass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderStateData stateData = context.Get<RenderStateData>()!;
            ForwardPlusRenderPath renderPath = Unsafe.As<ForwardPlusRenderPath>(stateData.Path);

            RenderList list = renderPath.PrimaryRenderList!;
            if (list.TotalFlagCount == 0)
                return;

            GenericResources resources = renderPass.Blackboard.Add<GenericResources>();

            using (RasterPassDescription desc = renderPass.SetupRasterPass("FP-Resources", out PassData passData))
            {
                resources.MatrixBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(list.TotalFlagCount * Unsafe.SizeOf<RenderFlag>()),
                    Stride = Unsafe.SizeOf<RenderFlag>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.Structured | FGBufferUsage.Global,
                }, "FP-RenderFlagBuffer");

                resources.RawDataBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Math.Max(CountRawDataSize(list), 4),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.Raw | FGBufferUsage.Global
                }, "FP-RawDataBuffer");

                resources.GlobalMatricies = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<GlobalMatriciesData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.Global
                }, "FP-GlobalResources");

                resources.CameraData = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<CameraData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.Global
                }, "FP-CameraData");

                resources.DynamicDataBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<DynamicDataData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.Global
                }, "FP-DynamicData");

                {
                    passData.MatrixBuffer = resources.MatrixBuffer;
                    passData.RawDataBuffer = resources.RawDataBuffer;
                    passData.GlobalMatricies = resources.GlobalMatricies;
                    passData.CameraData = resources.CameraData;
                    passData.RenderList = list;
                }

                {
                    ShaderGlobalsManager.SetGlobalBuffer("sbFP_RenderFlagBuffer", resources.MatrixBuffer);
                    ShaderGlobalsManager.SetGlobalBuffer("baFP_RawDataBuffer", resources.RawDataBuffer);
                    ShaderGlobalsManager.SetGlobalBuffer("cbFP_GlobalMatricies", resources.GlobalMatricies);
                    ShaderGlobalsManager.SetGlobalBuffer("cbFP_CameraData", resources.CameraData);
                    ShaderGlobalsManager.SetGlobalBuffer("cbFP_DynamicData", resources.DynamicDataBuffer);
                }

                desc.UseResource(FGResourceUsage.Write, passData.MatrixBuffer);
                desc.UseResource(FGResourceUsage.Write, passData.RawDataBuffer);
                desc.UseResource(FGResourceUsage.Write, passData.GlobalMatricies);
                desc.UseResource(FGResourceUsage.Write, passData.CameraData);

                desc.SetRenderFunction<PassData>(ExecutePass);
            }
        }

        private static void ExecutePass(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer commandBuffer = context.CommandBuffer;

            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

            {
                using FGMappedSubresource<RenderFlag> flags = commandBuffer.Map<RenderFlag>(passData.MatrixBuffer);

                Span<RenderFlag> tempFlags = flags.Span;
                foreach (ShaderRenderBatcher renderBatcher in passData.RenderList!.ShaderBatchers)
                {
                    renderBatcher.Flags.CopyTo(tempFlags);
                    tempFlags = tempFlags.Slice(renderBatcher.Flags.Length);
                }
            }

            unsafe
            {
                using RentedArray<MaterialAsset> materials = RentedArray<MaterialAsset>.Rent(passData.RenderList.MaterialIds.Count);
                foreach (var kvp in passData.RenderList.MaterialIds)
                {
                    materials[(int)kvp.Value] = kvp.Key;
                }

                if (materials.Count > 0)
                {
                    using FGMappedSubresource<byte> rawData = commandBuffer.Map<byte>(passData.RawDataBuffer);

                    fixed (byte* ptr = rawData.Span)
                    {
                        byte* dataPtr = ptr;
                        foreach (MaterialAsset material in materials)
                        {
                            ROPropertyBlock block = material.PropertyBlock;

                            block.CopyBlockDataTo((nint)dataPtr);
                            dataPtr += block.BlockSize;
                        }
                    }
                }
            }

            {
                GlobalMatriciesData data = new GlobalMatriciesData(
                    cameraData.ViewProjection,
                    cameraData.View,
                    cameraData.Projection);

                commandBuffer.Upload(passData.GlobalMatricies, data);
            }

            {
                CameraData data = new CameraData(
                    cameraData.Transform.Transformation.Translation,
                    cameraData.Transform.ForwardVector);

                commandBuffer.Upload(passData.CameraData, data);
            }
        }

        private static int CountRawDataSize(RenderList list)
        {
            int size = 0;

            foreach (var kvp in list.MaterialIds)
            {
                MaterialAsset material = kvp.Key;
                size += material.PropertyBlock.BlockSize;
            }

            return size;
        }

        private class PassData : IPassData
        {
            public FrameGraphBuffer MatrixBuffer;
            public FrameGraphBuffer RawDataBuffer;
            public FrameGraphBuffer GlobalMatricies;
            public FrameGraphBuffer CameraData;
            public RenderList? RenderList;

            public void Clear()
            {
                MatrixBuffer = FrameGraphBuffer.Invalid;
                RawDataBuffer = FrameGraphBuffer.Invalid;
                GlobalMatricies = FrameGraphBuffer.Invalid;
                CameraData = FrameGraphBuffer.Invalid;
                RenderList = null;
            }
        }

        private readonly record struct GlobalMatriciesData(Matrix4x4 ViewProjection, Matrix4x4 View, Matrix4x4 Projection);

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct CameraData(Vector3 position, Vector3 direction)
        {
            [FieldOffset(0)] public readonly Vector3 Position = position;
            [FieldOffset(16)] public readonly Vector3 Direction = direction;
        }
    }

    internal readonly record struct DynamicDataData(uint InstanceOffset);

    internal class GenericResources : IBlackboardData
    {
        public FrameGraphBuffer MatrixBuffer;
        public FrameGraphBuffer RawDataBuffer;
        public FrameGraphBuffer GlobalMatricies;
        public FrameGraphBuffer CameraData;
        public FrameGraphBuffer DynamicDataBuffer;

        public void Clear()
        {
            MatrixBuffer = FrameGraphBuffer.Invalid;
            RawDataBuffer = FrameGraphBuffer.Invalid;
            GlobalMatricies = FrameGraphBuffer.Invalid;
            CameraData = FrameGraphBuffer.Invalid;
            DynamicDataBuffer = FrameGraphBuffer.Invalid;
        }
    }
}
