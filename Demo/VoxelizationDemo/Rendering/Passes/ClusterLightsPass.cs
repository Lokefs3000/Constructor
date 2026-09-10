using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using TerraFX.Interop.Windows;

namespace VoxelizationDemo.Rendering.Passes
{
    public sealed class ClusterLightsPass : IRenderPass
    {
        private readonly ComputeShaderAsset _buildClustersShader;
        private readonly ComputeShaderAsset _clusterLightsShader;

        private PropertyBlock? _buildClustersKernelBlock;
        private PropertyBlock? _binLightsKernelBlock;

        public ClusterLightsPass()
        {
            _buildClustersShader = AssetManager.LoadAsset<ComputeShaderAsset>("Content/Shaders/Compute/BuildClusters.compute");
            _clusterLightsShader = AssetManager.LoadAsset<ComputeShaderAsset>("Content/Shaders/Compute/ClusterLights.compute");
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            if (!_buildClustersShader.TryFindKernel("CSBuildClusters", out ComputeShaderKernel? buildClusters) ||
                !_clusterLightsShader.TryFindKernel("CSBinLights", out ComputeShaderKernel? binLights))
                return;

            _buildClustersKernelBlock ??= buildClusters.CreatePropertyBlock();
            _binLightsKernelBlock ??= binLights.CreatePropertyBlock();

            if (_buildClustersKernelBlock == null || _binLightsKernelBlock == null)
                return;

            RenderPathBlackboard pathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
            if (pathBlackboard.LightCollector == null)
                return;

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            FrameGraphBuffer clusterBoundsBuffer;
            FrameGraphBuffer screenToViewLight;

            using (ComputePassDescription desc = renderPass.SetupComputePass("BuildClusters", out BuildClustersPassData passData))
            {
                passData.ScreenToViewDataBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<ScreenToViewData>(),
                    Stride = Unsafe.SizeOf<ScreenToViewData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer
                }, "ScreenToView");

                passData.ScreenToViewLightBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<ScreenToViewLight>(),
                    Stride = Unsafe.SizeOf<ScreenToViewLight>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.Global
                }, "ScreenToViewLight");

                passData.ClustersBoundsBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<ClusterAABB>() * (ClustersWidth * ClustersHeight * ClustersDepth)),
                    Stride = Unsafe.SizeOf<ClusterAABB>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.Structured | FGBufferUsage.UnorderedAccess
                }, "ClusterBoundsBuffer");

                passData.BuildClusters = buildClusters;
                passData.BuildClustersBlock = _buildClustersKernelBlock;

                desc.UseResource(FGResourceUsage.ReadWrite, passData.ScreenToViewDataBuffer);
                desc.UseResource(FGResourceUsage.Write, passData.ScreenToViewLightBuffer);
                desc.UseResource(FGResourceUsage.Write, passData.ClustersBoundsBuffer);

                desc.SetRenderFunction<BuildClustersPassData>(BuildClustersPassFunction);

                // Assign outside
                clusterBoundsBuffer = passData.ClustersBoundsBuffer;
                screenToViewLight = passData.ScreenToViewLightBuffer;
            }

            FrameGraphBuffer lightIndexList;
            FrameGraphBuffer lightGridBuffer;

            using (ComputePassDescription desc = renderPass.SetupComputePass("BinLights", out BinLightsPassData passData))
            {
                passData.ClustersBoundsBuffer = clusterBoundsBuffer;

                passData.LightIndexCount = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<uint>(),
                    Stride = Unsafe.SizeOf<uint>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.Raw | FGBufferUsage.UnorderedAccess
                }, "LightIndexCount");

                passData.LightIndexList = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<uint>() * Math.Clamp(pathBlackboard.LightCollector.PointLights.Count, 1, 128) * (ClustersWidth * ClustersHeight * ClustersDepth)),
                    Stride = Unsafe.SizeOf<uint>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.Raw | FGBufferUsage.UnorderedAccess | FGBufferUsage.Global
                }, "LightIndexList");

                passData.LightGrid = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<LightGrid>() * (ClustersWidth * ClustersHeight * ClustersDepth)),
                    Stride = Unsafe.SizeOf<LightGrid>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.Structured | FGBufferUsage.UnorderedAccess | FGBufferUsage.Global
                }, "LightGrid");

                passData.BinLights = binLights;
                passData.BinLightsBlock = _binLightsKernelBlock;

                desc.UseResource(FGResourceUsage.Read, passData.ClustersBoundsBuffer);
                desc.UseResource(FGResourceUsage.Write, passData.LightIndexCount);
                desc.UseResource(FGResourceUsage.Write, passData.LightIndexList);
                desc.UseResource(FGResourceUsage.Write, passData.LightGrid);

                desc.SetRenderFunction<BinLightsPassData>(BinLightsPassFunction);

                // Assign outside
                lightIndexList = passData.LightIndexList;
                lightGridBuffer = passData.LightGrid;
            }

            // Setup globals
            ShaderGlobalsManager.SetGlobalBuffer("cbScreenToViewLight", screenToViewLight);
            ShaderGlobalsManager.SetGlobalBuffer("baLightIndexList", lightIndexList);
            ShaderGlobalsManager.SetGlobalBuffer("sbLightGrid", lightGridBuffer);
        }

        private static void BuildClustersPassFunction(ComputePassContext context, BuildClustersPassData passData)
        {
            Debug.Assert(passData.BuildClusters != null && passData.BuildClustersBlock != null);

            ComputeCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

            cmd.SetPipeline(passData.BuildClusters);
            cmd.SetProperties(passData.BuildClustersBlock);

            cmd.SetConstants(new BuildClustersComputeData(cameraData.ZNear, cameraData.ZFar));

            passData.BuildClustersBlock.SetResource("cbScreenToView", passData.ScreenToViewDataBuffer);
            passData.BuildClustersBlock.SetResource("sbClusterBoundsBuffer", passData.ClustersBoundsBuffer);

            {
                int screenWidth = cameraData.ColorTexture.Description.Width;
                int screenHeight = cameraData.ColorTexture.Description.Height;

                Matrix4x4.Invert(cameraData.Projection, out Matrix4x4 invProjection);
                cmd.Upload(passData.ScreenToViewDataBuffer, new ScreenToViewData(
                    invProjection,
                    (ushort)ClustersWidth,
                    (ushort)ClustersHeight,
                    (ushort)ClustersDepth,
                    (ushort)MathF.Ceiling(screenWidth / (float)ClustersWidth),
                    (ushort)screenWidth,
                    (ushort)screenHeight));

                float sliceScalingFactor = ClustersDepth / MathF.Log2(cameraData.ZFar / cameraData.ZNear);
                float sliceBiasFactor = -(ClustersDepth * MathF.Log2(cameraData.ZNear) / MathF.Log2(cameraData.ZFar / cameraData.ZNear));

                cmd.Upload(passData.ScreenToViewLightBuffer, new ScreenToViewLight(
                    invProjection,
                    (ushort)ClustersWidth,
                    (ushort)ClustersHeight,
                    (ushort)ClustersDepth,
                    (ushort)MathF.Ceiling(screenWidth / (float)ClustersWidth),
                    (ushort)screenWidth,
                    (ushort)screenHeight,
                    sliceScalingFactor,
                    sliceBiasFactor));
            }

            cmd.Dispatch(ClustersWidth, ClustersHeight, ClustersDepth);
        }

        private static void BinLightsPassFunction(ComputePassContext context, BinLightsPassData passData)
        {
            Debug.Assert(passData.BinLights != null && passData.BinLightsBlock != null);

            ComputeCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

            cmd.SetPipeline(passData.BinLights);
            cmd.SetProperties(passData.BinLightsBlock);

            cmd.SetConstants(cameraData.View);

            passData.BinLightsBlock.SetResource("sbClusterBoundsBuffer", passData.ClustersBoundsBuffer);
            passData.BinLightsBlock.SetResource("baLightIndexCount", passData.LightIndexCount);
            passData.BinLightsBlock.SetResource("baLightIndexList", passData.LightIndexList);
            passData.BinLightsBlock.SetResource("sbLightGrid", passData.LightGrid);

            cmd.Dispatch(1, 1, 24 / 4);
        }

        private const uint ClustersWidth = 16;
        private const uint ClustersHeight = 9;
        private const uint ClustersDepth = 24;

        private sealed class BuildClustersPassData : IPassData
        {
            public FrameGraphBuffer ScreenToViewDataBuffer;
            public FrameGraphBuffer ScreenToViewLightBuffer;
            public FrameGraphBuffer ClustersBoundsBuffer;

            public ComputeShaderKernel? BuildClusters;
            public PropertyBlock? BuildClustersBlock;

            public void Clear()
            {
                ScreenToViewDataBuffer = FrameGraphBuffer.Invalid;
                ScreenToViewLightBuffer = FrameGraphBuffer.Invalid;
                ClustersBoundsBuffer = FrameGraphBuffer.Invalid;

                BuildClusters = null;
                BuildClustersBlock = null;
            }
        }

        private sealed class BinLightsPassData : IPassData
        {
            public FrameGraphBuffer ClustersBoundsBuffer;
            public FrameGraphBuffer LightIndexCount;
            public FrameGraphBuffer LightIndexList;
            public FrameGraphBuffer LightGrid;

            public ComputeShaderKernel? BinLights;
            public PropertyBlock? BinLightsBlock;

            public void Clear()
            {
                ClustersBoundsBuffer = FrameGraphBuffer.Invalid;
                LightIndexCount = FrameGraphBuffer.Invalid;
                LightIndexList = FrameGraphBuffer.Invalid;
                LightGrid = FrameGraphBuffer.Invalid;

                BinLights = null;
                BinLightsBlock = null;
            }
        }

        private readonly record struct BuildClustersComputeData(float ZNear, float ZFar);

        private readonly record struct ScreenToViewData(Matrix4x4 InverseProjection, ushort TileSizeX, ushort TileSizeY, ushort TileSizeZ, ushort TileSizePerPixel, ushort ScreenWidth, ushort ScreenHeight);
        private readonly record struct ClusterAABB(Vector4 MinPoint, Vector4 MaxPoint);
        private readonly record struct LightGrid(uint Offset, uint Count);

        private readonly record struct ScreenToViewLight(Matrix4x4 InverseProjection, ushort TileSizeX, ushort TileSizeY, ushort TileSizeZ, ushort TileSizePerPixel, ushort ScreenWidth, ushort ScreenHeight, float Scale, float Bias);
    }
}
