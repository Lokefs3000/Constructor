using Primary.Assets;
using Primary.Mathematics;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Diagnostics.Passes
{
    internal sealed class GizmoRenderPass : IRenderPass, IDisposable
    {
        private readonly ShaderAsset _lineShader;
        private readonly ShaderAsset _triangleShader;

        private readonly ShaderAsset _screenLineShader;
        private readonly ShaderAsset _screenTriangleShader;

        private readonly PropertyBlock _block;

        public GizmoRenderPass()
        {
            _lineShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Gizmo/GizmoLine.shader");
            _triangleShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Gizmo/GizmoTriangle.shader");

            _screenLineShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Gizmo/ScreenGizmoLine.shader");
            _screenTriangleShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Gizmo/ScreenGizmoTriangle.shader");

            _block = _lineShader.CreatePropertyBlock();
        }

        public void Dispose()
        {
            _block?.Dispose();
        }

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            Gizmos gizmos = Gizmos.Instance;
            ScreenGizmos screenGizmos = ScreenGizmos.Instance;

            int vtxSize = screenGizmos.Vertices.Length * Unsafe.SizeOf<ScreenGizmoVertex>() + gizmos.Vertices.Length * Unsafe.SizeOf<GizmoVertex>();
            int idxSize = screenGizmos.Indices.Length * Unsafe.SizeOf<ushort>() + gizmos.Indices.Length * Unsafe.SizeOf<uint>();

            if (vtxSize > 0 && idxSize > 0)
            {
                RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>()!;

                using (RasterPassBuilder desc = frameGraph.AddRasterPass("Gizmos", out PassData data))
                {
                    data.GlobalBuffer = desc.CreateBuffer(new FGBufferDesc
                    {
                        Name = "GizmoGlobals",

                        ElementCount = 1,
                        Stride = Unsafe.SizeOf<Matrix4x4>(),
                        Target = FGBufferTarget.Constant
                    });

                    data.VertexBuffer = desc.CreateBuffer(new FGBufferDesc
                    {
                        Name = "GizmoVertices",

                        ElementCount = vtxSize,
                        Stride = 1,
                        Target = FGBufferTarget.VertexInput
                    });

                    data.IndexBuffer = desc.CreateBuffer(new FGBufferDesc
                    {
                        Name = "GizmoIndices",

                        ElementCount = idxSize,
                        Stride = 1,
                        Target = FGBufferTarget.IndexInput
                    });

                    data.OutColor = cameraData.ColorTexture;

                    data.LineShader = _lineShader;
                    data.TriangleShader = _triangleShader;

                    data.ScreenLineShader = _screenLineShader;
                    data.ScreenTriangleShader = _screenTriangleShader;

                    data.Block = _block;

                    desc.Write(data.GlobalBuffer);
                    desc.Write(cameraData.ColorTexture);

                    desc.ReadWrite(data.VertexBuffer);
                    desc.ReadWrite(data.IndexBuffer);

                    desc.SetFunction(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
            PassData passData = context.GetPassData<PassData>()!;
            RasterCommandBuffer cmd = context.CommandBuffer;

            RenderingManager manager = Engine.GlobalSingleton.RenderingManager;

            RenderCameraData cameraData = context.GetContext<RenderCameraData>();

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
                cmd.UploadBuffer(passData.VertexBuffer, 0, gizmos.Vertices);
                cmd.UploadBuffer(passData.IndexBuffer, 0, gizmos.Indices);

                cmd.UploadBuffer(passData.GlobalBuffer, 0, cameraData.ViewProjection);

                cmd.SetVertexBuffer(passData.VertexBuffer, Unsafe.SizeOf<GizmoVertex>());
                cmd.SetIndexBuffer(passData.IndexBuffer, Unsafe.SizeOf<uint>());

                GizmosMode currentMode = GizmosMode.Undefined;

                Span<GizmoSection> sections = gizmos.Sections;
                for (int i = 0; i < sections.Length; ++i)
                {
                    ref GizmoSection currentSection = ref sections[i];
                    int indexCount = i + 1 < sections.Length ? sections[i + 1].IndexStart - currentSection.IndexStart : gizmos.Indices.Length - currentSection.IndexStart;

                    if (currentSection.Mode != currentMode)
                    {
                        cmd.SetShader(currentSection.Mode switch
                        {
                            GizmosMode.Solid => passData.TriangleShader!,
                            GizmosMode.Wire => passData.LineShader!,
                            _ => throw new NotSupportedException()
                        });

                        currentMode = currentSection.Mode;
                    }

                    if (indexCount > 0)
                    {
                        cmd.DrawIndexedInstanced((uint)indexCount, 1, (uint)drawStartIdx, 0, 0);
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

                cmd.UploadBuffer(passData.VertexBuffer, (uint)vtxDataOffset, screenGizmos.Vertices);
                cmd.UploadBuffer(passData.IndexBuffer, (uint)idxDataOffset, screenGizmos.Indices);

                cmd.UploadBuffer(passData.GlobalBuffer, 0, projection);

                cmd.SetVertexBuffer(passData.VertexBuffer, Unsafe.SizeOf<ScreenGizmoVertex>());
                cmd.SetIndexBuffer(passData.IndexBuffer, Unsafe.SizeOf<ushort>());

                ScreenGizmosMode currentMode = ScreenGizmosMode.Undefined;
                object? currentResource = null;

                Span<ScreenGizmoSection> sections = screenGizmos.Sections;
                for (int i = 0; i < sections.Length; ++i)
                {
                    ref ScreenGizmoSection currentSection = ref sections[i];
                    int indexCount = i + 1 < sections.Length ? sections[i + 1].IndexStart - currentSection.IndexStart : screenGizmos.Indices.Length - currentSection.IndexStart;

                    if (currentSection.Mode != currentMode)
                    {
                        cmd.SetShader(currentSection.Mode switch
                        {
                            ScreenGizmosMode.Solid => passData.ScreenTriangleShader!,
                            ScreenGizmosMode.Wire => passData.ScreenLineShader!,
                            _ => throw new NotSupportedException()
                        });

                        currentMode = currentSection.Mode;
                    }

                    if (indexCount > 0)
                    {
                        cmd.DrawIndexedInstanced((uint)indexCount, 1, (uint)drawStartIdx, 0, 0);
                    }
                }
            }

            gizmos.ClearDrawData();
            screenGizmos.ClearDrawData();
        }

        private class PassData : IPassData
        {
            public TextureHandle OutColor;

            public BufferHandle GlobalBuffer;
            public BufferHandle IndexBuffer;
            public BufferHandle VertexBuffer;

            public ShaderAsset? LineShader;
            public ShaderAsset? TriangleShader;

            public ShaderAsset? ScreenLineShader;
            public ShaderAsset? ScreenTriangleShader;

            public PropertyBlock? Block;

            public void Clear()
            {
                OutColor = TextureHandle.Null;

                GlobalBuffer = BufferHandle.Null;
                IndexBuffer = BufferHandle.Null;
                VertexBuffer = BufferHandle.Null;

                LineShader = null;
                TriangleShader = null;

                ScreenLineShader = null;
                ScreenTriangleShader = null;

                Block = null;
            }
        }
    }
}
