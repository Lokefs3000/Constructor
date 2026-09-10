using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Diagnostics;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using Primary.Timing;

namespace VoxelizationDemo.Rendering.Passes
{
    public sealed class WriteVoxelsPass : IRenderPass
    {
        private readonly ShaderAsset _writeVoxelsShader;
        private readonly PropertyBlock _writeVoxelsBlock;

        public WriteVoxelsPass()
        {
            _writeVoxelsShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Voxel/WriteVoxels.shader");
            _writeVoxelsBlock = _writeVoxelsShader.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderPathBlackboard pathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
            if (pathBlackboard.RenderList == null)
                return;

            Vector3 focusPoint = Vector3.Zero;
            Vector3 halfVolumeSize = new Vector3(VoxelSpaceX, VoxelSpaceY, VoxelSpaceZ) * 0.5f;
            Gizmos.DrawWireAABB(new Primary.Mathematics.AABB(focusPoint - halfVolumeSize, focusPoint + halfVolumeSize), Color.Indigo);

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            VoxelBlackboard voxelBlackboard = renderPass.Blackboard.Add<VoxelBlackboard>();
            
            using (RasterPassDescription desc = renderPass.SetupRasterPass("WriteVoxels", out PassData passData))
            {
                passData.RenderList = pathBlackboard.RenderList;

                passData.VoxelBuffer = desc.CreateTexture(new FrameGraphTextureDesc
                {
                    Width = (int)VoxelSpaceX,
                    Height = (int)VoxelSpaceY,
                    Depth = (int)VoxelSpaceZ,

                    Dimension = FGTextureDimension._3D,
                    Format = RHIFormat.R32_UInt,
                    Usage = FGTextureUsage.PixelShader | FGTextureUsage.ShaderResource | FGTextureUsage.UnorderedAccess,

                    Swizzle = FGTextureSwizzle.RGBA
                }, "VoxelBuffer");

                passData.VoxelInfo = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<VoxelSpaceInfo>(),
                    Stride = Unsafe.SizeOf<VoxelSpaceInfo>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer
                }, "VoxelInfo");

                passData.ProjectedView = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<Matrix4x4>(),
                    Stride = Unsafe.SizeOf<Matrix4x4>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer
                }, "ProjectedView");

                passData.OutColor = desc.CreateTexture(new FrameGraphTextureDesc
                {
                    Width = (int)VoxelSpaceX,
                    Height = (int)VoxelSpaceY,

                    Dimension = FGTextureDimension._2D,
                    Format = RHIFormat.R8_UNorm,
                    Usage = FGTextureUsage.RenderTarget,

                    Swizzle = FGTextureSwizzle.RGBA
                }, "VoxelPixelOut");

                passData.WriteVoxels = _writeVoxelsShader;
                passData.WriteVoxelsBlock = _writeVoxelsBlock;

                desc.UseResource(FGResourceUsage.Write, passData.VoxelBuffer);
                desc.UseResource(FGResourceUsage.ReadWrite, passData.VoxelInfo);
                desc.UseResource(FGResourceUsage.ReadWrite, passData.ProjectedView);

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

            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;
            cmd.Upload(passData.VoxelInfo, new VoxelSpaceInfo(focusPoint, default, VoxelSpaceX, VoxelSpaceY, VoxelSpaceZ));

            if (passData.WriteVoxels != null && passData.WriteVoxelsBlock != null && passData.WriteVoxels.IsLoaded)
            {
                cmd.SetPipeline(passData.WriteVoxels);
                cmd.SetProperties(passData.WriteVoxelsBlock);

                cmd.SetRenderTarget(0, passData.OutColor);

                passData.WriteVoxelsBlock.SetResource("cbVoxelInfo", passData.VoxelInfo);
                passData.WriteVoxelsBlock.SetResource("txVoxelBuffer", passData.VoxelBuffer);
                passData.WriteVoxelsBlock.SetResource("cbProjectedView", passData.ProjectedView);

                IRenderMeshSource? currentMeshSource = null;

                Vector3 halfVolumeSize = new Vector3(VoxelSpaceX, VoxelSpaceY, VoxelSpaceZ) * 0.5f;

                Matrix4x4 orthoXForward = Matrix4x4.CreateOrthographic(VoxelSpaceZ, VoxelSpaceY, 0.0f, VoxelSpaceX);
                Matrix4x4 orthoYForward = Matrix4x4.CreateOrthographic(VoxelSpaceX, VoxelSpaceZ, 0.0f, VoxelSpaceY);
                Matrix4x4 orthoZForward = Matrix4x4.CreateOrthographic(VoxelSpaceX, VoxelSpaceY, 0.0f, VoxelSpaceZ);

                using RentedArray<uint> temp = new RentedArray<uint>((int)(VoxelSpaceX * VoxelSpaceY * VoxelSpaceZ));
                temp.Span.Fill(0);

                cmd.Upload(new FGTextureUploadDesc(passData.VoxelBuffer, null, 0, (int)(VoxelSpaceX * Unsafe.SizeOf<uint>())), temp.Span);

                int orientationIndex = 0;
                foreach (Vector3 dir in _orientations)
                {
                    using (cmd.BeginEvent("Pass"u8))
                    {
                        Matrix4x4 view = Matrix4x4.CreateLookTo(focusPoint - dir * halfVolumeSize, -dir, dir.Y != 0.0f ? Vector3.UnitZ : Vector3.UnitY);
                        Matrix4x4 proj = (orientationIndex % 3) switch
                        {
                            0 => orthoXForward,
                            1 => orthoYForward,
                            2 => orthoZForward,
                            _ => throw new NotImplementedException()
                        };
                
                        cmd.Upload(passData.ProjectedView, view * proj);
                
                        foreach (ShaderRenderBatcher renderBatcher in passData.RenderList!.ShaderBatchers)
                        {
                            foreach (RenderSegment segment in renderBatcher.Segments)
                            {
                                if (!segment.Material.IsLoaded)
                                    continue;
                
                                // if (currentMeshSource != segment.Mesh.Source)
                                // {
                                //     currentMeshSource = segment.Mesh.Source;
                                //     cmd.SetVertexBuffer((FrameGraphBuffer)segment.Mesh.Source.VertexBuffer!);
                                //     cmd.SetIndexBuffer((FrameGraphBuffer)segment.Mesh.Source.IndexBuffer!);
                                // }
                                // 
                                // cmd.SetConstants((uint)segment.FlagIndexStart);
                                // 
                                // if (segment.Mesh.HasIndices)
                                //     cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(segment.Mesh.IndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), segment.Mesh.IndexOffset, (int)segment.Mesh.VertexOffset));
                                // else
                                //     cmd.DrawInstanced(new FGDrawInstancedDesc(segment.Mesh.IndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), segment.Mesh.VertexOffset));
                            }
                        }
                    }
                
                    ++orientationIndex;
                }
            }
        }

        private static readonly FastNoiseLite s_noise = new FastNoiseLite();
        private static float s_offset = 0.0f;

        private static readonly ImmutableArray<Vector3> _orientations = [
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, -1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, -1.0f),
            ];

        public const uint VoxelSpaceX = 48;
        public const uint VoxelSpaceY = 48;
        public const uint VoxelSpaceZ = 48;

        private sealed class PassData : IPassData
        {
            public RenderList? RenderList;

            public FrameGraphBuffer VoxelInfo;
            public FrameGraphTexture VoxelBuffer;
            public FrameGraphBuffer ProjectedView;

            public FrameGraphTexture OutColor;

            public ShaderAsset? WriteVoxels;
            public PropertyBlock? WriteVoxelsBlock;

            public void Clear()
            {
                RenderList = null;

                VoxelInfo = FrameGraphBuffer.Invalid;
                VoxelBuffer = FrameGraphTexture.Invalid;
                ProjectedView = FrameGraphBuffer.Invalid;

                OutColor = FrameGraphTexture.Invalid;

                WriteVoxels = null;
                WriteVoxelsBlock = null;
            }
        }

        private readonly record struct VoxelSpaceInfo(Vector3 VoxelOrigin, uint __pad0, uint VoxelSizeX, uint VoxelSizeY, uint VoxelSizeZ);
    }

    public sealed class VoxelBlackboard : IBlackboardData
    {
        public FrameGraphBuffer VoxelInfo;
        public FrameGraphTexture VoxelBuffer;

        public void Clear()
        {
            VoxelInfo = FrameGraphBuffer.Invalid;
            VoxelBuffer = FrameGraphTexture.Invalid;
        }
    }
}
