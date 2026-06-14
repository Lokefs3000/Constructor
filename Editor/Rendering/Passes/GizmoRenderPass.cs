using Primary.Assets;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.Rendering.Passes
{
    internal sealed class GizmoRenderPass : IRenderPass
    {
        private ShaderAsset? _lineShader;
        private ShaderAsset? _triangleShader;

        private ShaderAsset? _screenLineShader;
        private ShaderAsset? _screenTriangleShader;

        private PropertyBlock? _block;

        public GizmoRenderPass()
        {
            _lineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/GizmoLine.shader");
            _triangleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/GizmoTriangle.shader");

            _screenLineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/ScreenGizmoLine.shader");
            _screenTriangleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmo/ScreenGizmoTriangle.shader");

            _block = _lineShader.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            Gizmos gizmos = Gizmos.Instance;
            ScreenGizmos screenGizmos = ScreenGizmos.Instance;

            int vtxSize = screenGizmos.Vertices.Length * Unsafe.SizeOf<ScreenGizmoVertex>() + gizmos.Vertices.Length * Unsafe.SizeOf<GizmoVertex>();
            int idxSize = screenGizmos.Indices.Length * Unsafe.SizeOf<ushort>() + gizmos.Indices.Length * Unsafe.SizeOf<uint>();

            if (vtxSize > 0 && idxSize > 0)
            {
                RenderCameraData cameraData = context.Get<RenderCameraData>()!;

                using (RasterPassDescription desc = renderPass.SetupRasterPass("Gizmos", out PassData data))
                {
                    data.GlobalBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)Unsafe.SizeOf<Matrix4x4>(),
                        Usage = FGBufferUsage.ConstantBuffer
                    }, "GizmoGlobals");

                    data.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)vtxSize,
                        Stride = 0,
                        Usage = FGBufferUsage.VertexBuffer
                    }, "GizmoVertices");

                    data.IndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)idxSize,
                        Stride = 0,
                        Usage = FGBufferUsage.IndexBuffer
                    }, "GizmoIndices");

                    data.OutColor = cameraData.ColorTexture;

                    data.LineShader = _lineShader;
                    data.TriangleShader = _triangleShader;

                    data.ScreenLineShader = _screenLineShader;
                    data.ScreenTriangleShader = _screenTriangleShader;

                    data.Block = _block;

                    desc.UseResource(FGResourceUsage.Write, data.GlobalBuffer);
                    desc.UseResource(FGResourceUsage.ReadWrite, data.VertexBuffer);
                    desc.UseResource(FGResourceUsage.ReadWrite, data.IndexBuffer);
                    desc.UseRenderTarget(cameraData.ColorTexture);

                    desc.SetRenderFunction<PassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;
            RasterCommandBuffer cmd = context.CommandBuffer;

            Gizmos gizmos = Gizmos.Instance;
            ScreenGizmos screenGizmos = ScreenGizmos.Instance;

            passData.Block!.SetResource("cbGlobals", passData.GlobalBuffer);

            int drawStartIdx = 0;

            int vtxDataOffset = 0;
            int idxDataOffset = 0;

            cmd.SetRenderTarget(0, passData.OutColor);

            cmd.SetProperties(passData.Block);

            // gizmos
            if (gizmos.HasAnyDrawData)
            {
                cmd.Upload(new FGBufferUploadDesc(passData.VertexBuffer, 0), gizmos.Vertices);
                cmd.Upload(new FGBufferUploadDesc(passData.IndexBuffer, 0), gizmos.Indices);

                cmd.Upload(passData.GlobalBuffer, cameraData.ViewProjection);

                cmd.SetVertexBuffer(new FGSetBufferDesc(passData.VertexBuffer, Unsafe.SizeOf<GizmoVertex>()));
                cmd.SetIndexBuffer(new FGSetBufferDesc(passData.IndexBuffer, Unsafe.SizeOf<uint>()));

                GizmosMode currentMode = GizmosMode.Undefined;

                Span<GizmoSection> sections = gizmos.Sections;
                for (int i = 0; i < sections.Length; ++i)
                {
                    ref GizmoSection currentSection = ref sections[i];
                    int indexCount = i + 1 < sections.Length ? sections[i + 1].IndexStart - currentSection.IndexStart : gizmos.Indices.Length - currentSection.IndexStart;

                    if (currentSection.Mode != currentMode)
                    {
                        cmd.SetPipeline(currentSection.Mode switch
                        {
                            GizmosMode.Solid => passData.TriangleShader!,
                            GizmosMode.Wire => passData.LineShader!,
                            _ => throw new NotSupportedException()
                        });

                        currentMode = currentSection.Mode;
                    }

                    if (indexCount > 0)
                    {
                        cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc((uint)indexCount, StartIndexLocation: (uint)drawStartIdx));
                    }
                }

                //vtxDataOffset += gizmos.Vertices.Length * Unsafe.SizeOf<GizmoVertex>();
                //idxDataOffset += gizmos.Indices.Length * Unsafe.SizeOf<ushort>();
            }

            // screen gizmos
            if (screenGizmos.HasAnyDrawData)
            {
                Matrix3x2 projection =
                    Matrix3x2.CreateScale(new Vector2(2.0f) / new Int2(cameraData.ClientSize.X, -cameraData.ClientSize.Y).AsVector2()) *
                    Matrix3x2.CreateTranslation(-1.0f, 1.0f);

                cmd.Upload(new FGBufferUploadDesc(passData.VertexBuffer, (uint)vtxDataOffset), screenGizmos.Vertices);
                cmd.Upload(new FGBufferUploadDesc(passData.IndexBuffer, (uint)idxDataOffset), screenGizmos.Indices);

                cmd.Upload(passData.GlobalBuffer, projection);

                cmd.SetVertexBuffer(new FGSetBufferDesc(passData.VertexBuffer, Unsafe.SizeOf<ScreenGizmoVertex>()));
                cmd.SetIndexBuffer(new FGSetBufferDesc(passData.IndexBuffer, Unsafe.SizeOf<ushort>()));

                ScreenGizmosMode currentMode = ScreenGizmosMode.Undefined;
                object? currentResource = null;

                Span<ScreenGizmoSection> sections = screenGizmos.Sections;
                for (int i = 0; i < sections.Length; ++i)
                {
                    ref ScreenGizmoSection currentSection = ref sections[i];
                    int indexCount = i + 1 < sections.Length ? sections[i + 1].IndexStart - currentSection.IndexStart : screenGizmos.Indices.Length - currentSection.IndexStart;

                    if (currentSection.Mode != currentMode)
                    {
                        cmd.SetPipeline(currentSection.Mode switch
                        {
                            ScreenGizmosMode.Solid => passData.ScreenTriangleShader!,
                            ScreenGizmosMode.Wire => passData.ScreenLineShader!,
                            _ => throw new NotSupportedException()
                        });

                        currentMode = currentSection.Mode;
                    }

                    if (indexCount > 0)
                    {
                        cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc((uint)indexCount, StartIndexLocation: (uint)drawStartIdx));
                    }
                }
            }

            gizmos.ClearDrawData();
            screenGizmos.ClearDrawData();
        }

        private class PassData : IPassData
        {
            public FrameGraphTexture OutColor;

            public FrameGraphBuffer GlobalBuffer;
            public FrameGraphBuffer IndexBuffer;
            public FrameGraphBuffer VertexBuffer;

            public ShaderAsset? LineShader;
            public ShaderAsset? TriangleShader;

            public ShaderAsset? ScreenLineShader;
            public ShaderAsset? ScreenTriangleShader;

            public PropertyBlock? Block;

            public void Clear()
            {
                GlobalBuffer = FrameGraphBuffer.Invalid;
                IndexBuffer = FrameGraphBuffer.Invalid;
                VertexBuffer = FrameGraphBuffer.Invalid;

                LineShader = null;
                TriangleShader = null;

                ScreenLineShader = null;
                ScreenTriangleShader = null;

                Block = null;
            }
        }
    }
}
