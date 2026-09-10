using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using Silk.NET.Direct2D;

namespace VoxelizationDemo.Rendering.Passes
{
    public sealed class VoxelRaymarchPass : IRenderPass
    {
        private readonly ShaderAsset _raymarchVoxelsShader;
        private readonly PropertyBlock _raymarchVoxelsBlock;

        public VoxelRaymarchPass()
        {
            _raymarchVoxelsShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Voxel/RaymarchVoxels.shader");
            _raymarchVoxelsBlock = _raymarchVoxelsShader.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderPathBlackboard pathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
            if (pathBlackboard.RenderList == null)
                return;

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            VoxelBlackboard voxelBlackboard = renderPass.Blackboard.Get<VoxelBlackboard>()!;

            using (RasterPassDescription desc = renderPass.SetupRasterPass("RaymarchVoxels", out PassData passData))
            {
                passData.VoxelInfo = voxelBlackboard.VoxelInfo;
                passData.VoxelBuffer = voxelBlackboard.VoxelBuffer;
                passData.OutColor = cameraData.ColorTexture;

                passData.ProjectionData = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<ProjectionData>(),
                    Stride = Unsafe.SizeOf<ProjectionData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer
                }, "ProjectionData");

                passData.RaymarchVoxels = _raymarchVoxelsShader;
                passData.RaymarchVoxelsBlock = _raymarchVoxelsBlock;

                desc.UseResource(FGResourceUsage.Read, passData.VoxelBuffer);
                desc.UseResource(FGResourceUsage.Read, passData.VoxelInfo);
                desc.UseResource(FGResourceUsage.ReadWrite, passData.ProjectionData);

                desc.UseRenderTarget(passData.OutColor);

                desc.SetRenderFunction<PassData>(PassFunction);

                // Setup blackboard
                voxelBlackboard.VoxelBuffer = passData.VoxelBuffer;
                voxelBlackboard.VoxelInfo = passData.VoxelInfo;
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            Vector3 focusPoint = Vector3.Zero;

            if (passData.RaymarchVoxels != null && passData.RaymarchVoxelsBlock != null && passData.RaymarchVoxels.IsLoaded)
            {
                RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

                cmd.SetPipeline(passData.RaymarchVoxels);
                cmd.SetProperties(passData.RaymarchVoxelsBlock);

                cmd.SetRenderTarget(0, passData.OutColor);

                passData.RaymarchVoxelsBlock.SetResource("cbVoxelInfo", passData.VoxelInfo);
                passData.RaymarchVoxelsBlock.SetResource("txVoxelBuffer", passData.VoxelBuffer);
                passData.RaymarchVoxelsBlock.SetResource("cbProjectionData", passData.ProjectionData);

                Matrix4x4.Invert(cameraData.Projection, out Matrix4x4 proj);
                Matrix4x4.Invert(cameraData.View, out Matrix4x4 view);

                cmd.Upload(passData.ProjectionData, new ProjectionData(view, proj, cameraData.Transform.Transformation.Translation));

                cmd.DrawInstanced(new FGDrawInstancedDesc(3));
            }
        }

        private sealed class PassData : IPassData
        {
            public FrameGraphBuffer VoxelInfo;
            public FrameGraphTexture VoxelBuffer;
            public FrameGraphBuffer ProjectionData;

            public FrameGraphTexture OutColor;

            public ShaderAsset? RaymarchVoxels;
            public PropertyBlock? RaymarchVoxelsBlock;

            public void Clear()
            {
                VoxelInfo = FrameGraphBuffer.Invalid;
                VoxelBuffer = FrameGraphTexture.Invalid;
                ProjectionData = FrameGraphBuffer.Invalid;

                OutColor = FrameGraphTexture.Invalid;

                RaymarchVoxels = null;
                RaymarchVoxelsBlock = null;
            }
        }

        private readonly record struct ProjectionData(Matrix4x4 InvProjectionMatrix, Matrix4x4 InvViewMatrix, Vector3 CameraOrigin);
    }
}
