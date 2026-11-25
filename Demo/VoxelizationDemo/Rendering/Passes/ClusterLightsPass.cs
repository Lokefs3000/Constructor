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

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            if (!_buildClustersShader.TryFindKernel("CSBuildClusters", out ComputeShaderKernel? buildClusters) ||
                !_clusterLightsShader.TryFindKernel("CSBinLights", out ComputeShaderKernel? binLights))
                return;

            _buildClustersKernelBlock ??= buildClusters.CreatePropertyBlock();
            _binLightsKernelBlock ??= binLights.CreatePropertyBlock();

            if (_buildClustersKernelBlock == null || _binLightsKernelBlock == null)
                return;

            RenderStageBlackboard pathBlackboard = frameGraph.GetBlackboard<RenderStageBlackboard>()!;
            if (pathBlackboard.LightCollector == null)
                return;

            RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>()!;

            BufferHandle clusterBoundsBuffer;
            BufferHandle screenToViewLight;

            using (ComputePassBuilder desc = frameGraph.AddComputePass("BuildClusters", out BuildClustersPassData passData))
            {
                passData.ScreenToViewDataBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "ScreenToView",
                    ElementCount = 1,
                    Stride = Unsafe.SizeOf<ScreenToViewData>(),
                    Target = FGBufferTarget.Constant
                });

                passData.ScreenToViewLightBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "ScreenToViewLight",
                    ElementCount = 1,
                    Stride = Unsafe.SizeOf<ScreenToViewLight>(),
                    Target = FGBufferTarget.Constant
                });

                passData.ClustersBoundsBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "ClusterBoundsBuffer",
                    ElementCount = (int)(ClustersWidth * ClustersHeight * ClustersDepth),
                    Stride = Unsafe.SizeOf<ClusterAABB>(),
                    Target = FGBufferTarget.Structured
                });

                passData.BuildClusters = buildClusters;
                passData.BuildClustersBlock = _buildClustersKernelBlock;

                desc.ReadWrite(passData.ScreenToViewDataBuffer);
                desc.Write(passData.ScreenToViewLightBuffer);
                desc.Write(passData.ClustersBoundsBuffer);

                desc.SetFunction(BuildClustersPassFunction);

                // Assign outside
                clusterBoundsBuffer = passData.ClustersBoundsBuffer;
                screenToViewLight = passData.ScreenToViewLightBuffer;
            }

            BufferHandle lightIndexList;
            BufferHandle lightGridBuffer;

            using (ComputePassBuilder desc = frameGraph.AddComputePass("BinLights", out BinLightsPassData passData))
            {
                passData.ClustersBoundsBuffer = clusterBoundsBuffer;

                passData.LightIndexCount = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "LightIndexCount",
                    ElementCount = 1,
                    Stride = Unsafe.SizeOf<uint>(),
                    Target = FGBufferTarget.Raw
                });

                passData.LightIndexList = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "LightIndexList",
                    ElementCount = (int)(Math.Clamp(pathBlackboard.LightCollector.PointLights.Count, 1, 128) * (ClustersWidth * ClustersHeight * ClustersDepth)),
                    Stride = Unsafe.SizeOf<uint>(),
                    Target = FGBufferTarget.Raw
                });

                passData.LightGrid = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "LightGrid",
                    ElementCount = (int)(ClustersWidth * ClustersHeight * ClustersDepth),
                    Stride = Unsafe.SizeOf<LightGrid>(),
                    Target = FGBufferTarget.Structured
                });

                passData.BinLights = binLights;
                passData.BinLightsBlock = _binLightsKernelBlock;

                desc.Read(passData.ClustersBoundsBuffer);
                desc.Write(passData.LightIndexCount);
                desc.Write(passData.LightIndexList);
                desc.Write(passData.LightGrid);

                desc.SetFunction(BinLightsPassFunction);

                // Assign outside
                lightIndexList = passData.LightIndexList;
                lightGridBuffer = passData.LightGrid;
            }

            // Setup globals
            ShaderGlobalsManager.SetGlobalBuffer("cbScreenToViewLight", screenToViewLight);
            ShaderGlobalsManager.SetGlobalBuffer("baLightIndexList", lightIndexList);
            ShaderGlobalsManager.SetGlobalBuffer("sbLightGrid", lightGridBuffer);
        }

        private static void BuildClustersPassFunction(ComputePassContext context)
        {
            BuildClustersPassData passData = context.GetPassData<BuildClustersPassData>()!;
            Debug.Assert(passData.BuildClusters != null && passData.BuildClustersBlock != null);

            ComputeCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.GetContext<RenderCameraData>()!;

            cmd.SetShader(passData.BuildClusters);
            cmd.SetProperties(passData.BuildClustersBlock);

            cmd.SetConstants(new BuildClustersComputeData(cameraData.ZNear, cameraData.ZFar));

            passData.BuildClustersBlock.SetResource("cbScreenToView", passData.ScreenToViewDataBuffer);
            passData.BuildClustersBlock.SetResource("sbClusterBoundsBuffer", passData.ClustersBoundsBuffer);

            {
                ref readonly FGTextureDesc colorTextureDesc = ref context.GetDescription(cameraData.ColorTexture);
                int screenWidth = colorTextureDesc.Width;
                int screenHeight = colorTextureDesc.Height;

                Matrix4x4.Invert(cameraData.Projection, out Matrix4x4 invProjection);
                cmd.UploadBuffer(passData.ScreenToViewDataBuffer, 0, new ScreenToViewData(
                    invProjection,
                    (ushort)ClustersWidth,
                    (ushort)ClustersHeight,
                    (ushort)ClustersDepth,
                    (ushort)MathF.Ceiling(screenWidth / (float)ClustersWidth),
                    (ushort)screenWidth,
                    (ushort)screenHeight));

                float sliceScalingFactor = ClustersDepth / MathF.Log2(cameraData.ZFar / cameraData.ZNear);
                float sliceBiasFactor = -(ClustersDepth * MathF.Log2(cameraData.ZNear) / MathF.Log2(cameraData.ZFar / cameraData.ZNear));

                cmd.UploadBuffer(passData.ScreenToViewLightBuffer, 0, new ScreenToViewLight(
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

        private static void BinLightsPassFunction(ComputePassContext context)
        {
            BinLightsPassData passData = context.GetPassData<BinLightsPassData>()!;
            Debug.Assert(passData.BinLights != null && passData.BinLightsBlock != null);

            ComputeCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.GetContext<RenderCameraData>()!;

            cmd.SetShader(passData.BinLights);
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
            public BufferHandle ScreenToViewDataBuffer;
            public BufferHandle ScreenToViewLightBuffer;
            public BufferHandle ClustersBoundsBuffer;

            public ComputeShaderKernel? BuildClusters;
            public PropertyBlock? BuildClustersBlock;

            public void Clear()
            {
                ScreenToViewDataBuffer = BufferHandle.Null;
                ScreenToViewLightBuffer = BufferHandle.Null;
                ClustersBoundsBuffer = BufferHandle.Null;

                BuildClusters = null;
                BuildClustersBlock = null;
            }
        }

        private sealed class BinLightsPassData : IPassData
        {
            public BufferHandle ClustersBoundsBuffer;
            public BufferHandle LightIndexCount;
            public BufferHandle LightIndexList;
            public BufferHandle LightGrid;

            public ComputeShaderKernel? BinLights;
            public PropertyBlock? BinLightsBlock;

            public void Clear()
            {
                ClustersBoundsBuffer = BufferHandle.Null;
                LightIndexCount = BufferHandle.Null;
                LightIndexList = BufferHandle.Null;
                LightGrid = BufferHandle.Null;

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
