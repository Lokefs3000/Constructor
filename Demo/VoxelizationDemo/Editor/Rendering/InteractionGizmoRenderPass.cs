using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Collections;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Gizmo;

namespace VoxelizationDemo.Editor.Rendering
{
    internal sealed class InteractionGizmoRenderPass : IRenderPass
    {
        private readonly ShaderAsset _gizmoHandlesShader;
        private readonly PropertyBlock _gizmoHandlesBlock;

        public InteractionGizmoRenderPass()
        {
            _gizmoHandlesShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Editor/Gizmo/GizmoHandles.shader");
            _gizmoHandlesBlock = _gizmoHandlesShader.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            if (!_gizmoHandlesShader.IsLoaded)
                return;

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            InteractionGizmoManager gizmoManager = VoxelRuntime.Instance.EditorManager.InteractionGizmoManager;

            RentedList<InteractionGizmoDraw> draws = new RentedList<InteractionGizmoDraw>();
            RentedList<InteractionGizmoVertex> vertices = new RentedList<InteractionGizmoVertex>();
            RentedList<ushort> indices = new RentedList<ushort>();
            gizmoManager.GetGizmoPolygons(cameraData.ViewFrustrum, cameraData.Transform.Position, ref draws, ref vertices, ref indices);

            if (draws.IsEmpty || vertices.IsEmpty || indices.IsEmpty)
            {
                draws.Dispose();
                vertices.Dispose();
                indices.Dispose();
                return;
            }

            using (RasterPassDescription desc = renderPass.SetupRasterPass("InteractionGizmos", out PassData passData))
            {
                passData.OutColor = cameraData.ColorTexture;

                passData.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<InteractionGizmoVertex>() * vertices.Count),
                    Stride = Unsafe.SizeOf<InteractionGizmoVertex>(),
                    Usage = FGBufferUsage.VertexBuffer
                }, "VertexBuffer");

                passData.IndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<ushort>() * indices.Count),
                    Stride = Unsafe.SizeOf<ushort>(),
                    Usage = FGBufferUsage.IndexBuffer
                }, "VertexBuffer");

                passData.Shader = _gizmoHandlesShader;
                passData.Block = _gizmoHandlesBlock;

                passData.Vertices = vertices;
                passData.Indices = indices;
                passData.Draws = draws;

                desc.UseResource(FGResourceUsage.ReadWrite, passData.VertexBuffer);
                desc.UseResource(FGResourceUsage.ReadWrite, passData.IndexBuffer);

                desc.UseRenderTarget(passData.OutColor);

                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

            cmd.Upload(passData.VertexBuffer, passData.Vertices.AsSpan());
            cmd.Upload(passData.IndexBuffer, passData.Indices.AsSpan());

            cmd.SetRenderTarget(0, passData.OutColor);

            cmd.SetPipeline(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            cmd.SetVertexBuffer(passData.VertexBuffer);
            cmd.SetIndexBuffer(passData.IndexBuffer);

            foreach (InteractionGizmoDraw draw in passData.Draws)
            {
                cmd.SetConstants(draw.Model);
                cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc((uint)draw.IndexCount, StartIndexLocation: (uint)draw.IndexOffset, BaseVertexLocation: draw.BaseVertex));
            }
        }

        private sealed class PassData : IPassData
        {
            public FrameGraphTexture OutColor;

            public FrameGraphBuffer VertexBuffer;
            public FrameGraphBuffer IndexBuffer;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public RentedList<InteractionGizmoVertex> Vertices;
            public RentedList<ushort> Indices;
            public RentedList<InteractionGizmoDraw> Draws;

            public void Clear()
            {
                OutColor = FrameGraphTexture.Invalid;

                VertexBuffer = FrameGraphBuffer.Invalid;
                IndexBuffer = FrameGraphBuffer.Invalid;

                Shader = null;
                Block = null;

                Vertices.Dispose();
                Indices.Dispose();
                Draws.Dispose();
            }
        }
    }
}
