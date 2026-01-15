using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Rendering.Passes
{
    internal sealed class GizmoRenderPass : IRenderPass
    {
        private ShaderAsset? _lineShader;
        private ShaderAsset? _triangleShader;

        private PropertyBlock? _block;

        public GizmoRenderPass()
        {
            _lineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/GizmoLine.hlsl2");
            _triangleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/GizmoTriangle.hlsl2");

            _block = _lineShader.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            Gizmos gizmos = Gizmos.Instance;
            gizmos.FinishDrawData();

            if (gizmos.HasDrawData)
            {
                RenderCameraData cameraData = context.Get<RenderCameraData>()!;

                using (RasterPassDescription desc = renderPass.SetupRasterPass("EdGizmo", out PassData data))
                {
                    data.GlobalBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)Unsafe.SizeOf<Matrix4x4>(),
                        Usage = FGBufferUsage.ConstantBuffer
                    }, "EdGzGlobal");

                    data.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)(Unsafe.SizeOf<GZVertex>() * gizmos.Vertices.Length),
                        Stride = Unsafe.SizeOf<GZVertex>(),
                        Usage = FGBufferUsage.VertexBuffer
                    }, "EdGzVertex");

                    data.LineShader = _lineShader;
                    data.TriangleShader = _triangleShader;

                    data.Block = _block;

                    desc.UseResource(FGResourceUsage.Write, data.GlobalBuffer);
                    desc.UseResource(FGResourceUsage.ReadWrite, data.VertexBuffer);
                    desc.UseRenderTarget(cameraData.ColorTexture);

                    desc.SetRenderFunction<PassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;
            RasterCommandBuffer cmd = context.CommandBuffer;

            Gizmos gizmos = Gizmos.Instance;

            data.Block!.SetResource(PropertyBlock.GetID("cbGlobals"), data.GlobalBuffer);

            cmd.Upload(data.VertexBuffer, gizmos.Vertices);

            cmd.SetRenderTarget(0, cameraData.ColorTexture);
            cmd.SetVertexBuffer(data.VertexBuffer);
            cmd.SetProperties(data.Block);

            GZVertexType lastVertexType = unchecked((GZVertexType)(-1));

            int vertexOffset = 0;
            foreach (GZDrawSection section in gizmos.Sections)
            {
                if (section.Matrix.HasValue)
                {
                    Matrix4x4 matrix = section.Matrix.Value;
                    if (section.NeedsProjection)
                        matrix = cameraData.ViewProjection * matrix;

                    cmd.Upload(data.GlobalBuffer, matrix);
                }

                if (lastVertexType != section.VertexType)
                {
                    cmd.SetPipeline((section.VertexType switch
                    {
                        GZVertexType.Triangle => data.TriangleShader!,
                        GZVertexType.Line => data.LineShader!,
                        _ => throw new NotImplementedException(),
                    }).GraphicsPipeline!);

                    lastVertexType = section.VertexType;
                }

                cmd.DrawInstanced(new FGDrawInstancedDesc((uint)section.VertexCount, 1, (uint)vertexOffset));
                vertexOffset += section.VertexCount;
            }

            gizmos.ClearDrawData();
        }

        private class PassData : IPassData
        {
            public FrameGraphBuffer GlobalBuffer;
            public FrameGraphBuffer VertexBuffer;

            public ShaderAsset? LineShader;
            public ShaderAsset? TriangleShader;

            public PropertyBlock? Block;

            public void Clear()
            {
                GlobalBuffer = FrameGraphBuffer.Invalid;
                VertexBuffer = FrameGraphBuffer.Invalid;

                LineShader = null;
                TriangleShader = null;

                Block = null;
            }
        }
    }
}
