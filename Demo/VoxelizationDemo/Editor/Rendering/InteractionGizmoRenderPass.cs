using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Collections;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Commands;
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

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            if (!_gizmoHandlesShader.IsLoaded)
                return;

            RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>();

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

            using (RasterPassBuilder desc = frameGraph.AddRasterPass("InteractionGizmos", out PassData passData))
            {
                passData.OutColor = cameraData.ColorTexture;

                passData.VertexBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "VertexBuffer",
                    ElementCount = vertices.Count,
                    Stride = Unsafe.SizeOf<InteractionGizmoVertex>(),
                    Target = FGBufferTarget.VertexInput
                });

                passData.IndexBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "IndexBuffer",
                    ElementCount = indices.Count,
                    Stride = Unsafe.SizeOf<ushort>(),
                    Target = FGBufferTarget.IndexInput
                });

                passData.Shader = _gizmoHandlesShader;
                passData.Block = _gizmoHandlesBlock;

                passData.Vertices = vertices;
                passData.Indices = indices;
                passData.Draws = draws;

                desc.ReadWrite(passData.VertexBuffer);
                desc.ReadWrite(passData.IndexBuffer);

                desc.Write(passData.OutColor);

                desc.SetFunction(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
            PassData passData = context.GetPassData<PassData>()!;
            RasterCommandBuffer cmd = context.CommandBuffer;

            RenderCameraData cameraData = context.GetContext<RenderCameraData>()!;

            cmd.UploadBuffer(passData.VertexBuffer, 0, passData.Vertices.AsSpan());
            cmd.UploadBuffer(passData.IndexBuffer, 0, passData.Indices.AsSpan());

            cmd.SetRenderTarget(0, passData.OutColor);

            cmd.SetShader(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            cmd.SetVertexBuffer(passData.VertexBuffer);
            cmd.SetIndexBuffer(passData.IndexBuffer);

            foreach (InteractionGizmoDraw draw in passData.Draws)
            {
                cmd.SetConstants(draw.Model);
                cmd.DrawIndexedInstanced((uint)draw.IndexCount, 1, (uint)draw.IndexOffset, draw.BaseVertex, 0);
            }
        }

        private sealed class PassData : IPassData
        {
            public TextureHandle OutColor;

            public BufferHandle VertexBuffer;
            public BufferHandle IndexBuffer;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public RentedList<InteractionGizmoVertex> Vertices;
            public RentedList<ushort> Indices;
            public RentedList<InteractionGizmoDraw> Draws;

            public void Clear()
            {
                OutColor = TextureHandle.Null;

                VertexBuffer = BufferHandle.Null;
                IndexBuffer = BufferHandle.Null;

                Shader = null;
                Block = null;

                Vertices.Dispose();
                Indices.Dispose();
                Draws.Dispose();
            }
        }
    }
}
