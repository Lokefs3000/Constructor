using Editor.UI.Assets;
using Editor.UI.Visual;
using Primary;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Gdiplus;

namespace Editor.UI.Visual.Passes
{
    internal sealed class UIWindowRenderPass : IRenderPass
    {
        private ShaderAsset[] _shaders;
        private PropertyBlock[] _dataBlocks;

        public UIWindowRenderPass()
        {
            _shaders = new ShaderAsset[Enum.GetValues<BuiltSegmentType>().Length];
            _dataBlocks = new PropertyBlock[Enum.GetValues<DataBlockTarget>().Length];

            _shaders[(int)BuiltSegmentType.Points] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Points.shader");
            _shaders[(int)BuiltSegmentType.Lines] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Lines.shader");
            _shaders[(int)BuiltSegmentType.Rect] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Rect.shader");
            _shaders[(int)BuiltSegmentType.RoundedRect] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/RoundedRect.shader");
            _shaders[(int)BuiltSegmentType.Circle] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Circle.shader");
            _shaders[(int)BuiltSegmentType.Triangle] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Triangle.shader");
            _shaders[(int)BuiltSegmentType.Image] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Image.shader");
            _shaders[(int)BuiltSegmentType.Text] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Text.shader");

            _dataBlocks[(int)DataBlockTarget.Default] = _shaders[(int)BuiltSegmentType.Points].CreatePropertyBlock();
            _dataBlocks[(int)DataBlockTarget.Image] = _shaders[(int)BuiltSegmentType.Image].CreatePropertyBlock();
            _dataBlocks[(int)DataBlockTarget.Text] = _shaders[(int)BuiltSegmentType.Text].CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            UIRenderer renderer = UIManager.Instance.Renderer;
            if (renderer.AreAnyWindowsQueued)
            {
                BlackboardData blackboard = renderPass.Blackboard.Add<BlackboardData>()!;

                UIGenGradientsRenderPass.BlackboardData? gradientsBlackboard = renderPass.Blackboard.Get<UIGenGradientsRenderPass.BlackboardData>();

                blackboard.Regions = ArrayPool<UICompositeRegion>.Shared.Rent(renderer.WindowQueueSize);
                blackboard.RegionCount = 0;

                while (renderer.TryDequeueQueuedWindow(out UIWindowRedraw redraw))
                {
                    using (RasterPassDescription desc = renderPass.SetupRasterPass(Engine.IsDebugBuild ? $"UI-DrawWnd({redraw.Window.WindowTitle})" : "UI-DrawWnd", out PassData data))
                    {
                        DrawContext drawCtx = redraw.Context;
                        IWindowHost host = redraw.Window.ParentHost!;

                        UIDockHost? dockHost = host as UIDockHost;

                        data.Renderer = renderer;

                        data.Shaders = _shaders;
                        data.DataBlocks = _dataBlocks;

                        data.Redraw = redraw;

                        if (dockHost != null)
                        {
                            Vector2 drawSize = redraw.Region.Size;
                            data.OutColor = desc.CreateTexture(new FrameGraphTextureDesc
                            {
                                Width = (int)drawSize.X,
                                Height = (int)drawSize.Y,
                                Depth = 1,

                                Dimension = FGTextureDimension._2D,
                                Format = RHIFormat.RGB10A2_UNorm,
                                Usage = FGTextureUsage.ShaderResource | FGTextureUsage.RenderTarget | FGTextureUsage.PixelShader,

                                Swizzle = new FGTextureSwizzle(FGSwizzleChannel.Red, FGSwizzleChannel.Green, FGSwizzleChannel.Blue, FGSwizzleChannel.One)
                            }, "UI-RedrawRT");
                        }
                        else
                        {
                            if (host.HostTexture == null)
                                return;

                            data.OutColor = host.HostTexture;
                        }

                        data.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)(Unsafe.SizeOf<UIVertex>() * drawCtx.Mesh.VertexCount),
                            Stride = Unsafe.SizeOf<UIVertex>(),
                            Usage = FGBufferUsage.VertexBuffer | FGBufferUsage.GenericShader
                        }, "UI-VertexBuf");

                        data.IndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)(Unsafe.SizeOf<ushort>() * drawCtx.Mesh.IndexCount),
                            Stride = Unsafe.SizeOf<ushort>(),
                            Usage = FGBufferUsage.IndexBuffer | FGBufferUsage.GenericShader
                        }, "UI-IndexBuf");

                        data.GlobalBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)Unsafe.SizeOf<GlobalBufferData>(),
                            Usage = FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader
                        }, "UI-GlobalData");

                        data.MetadataBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)drawCtx.Builder.Metadata.Length,
                            Usage = FGBufferUsage.Raw | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader
                        }, "UI-Metadata");

                        data.GradientsTexture = gradientsBlackboard?.Gradients ?? FrameGraphTexture.Invalid;

                        desc.UseResource(FGResourceUsage.ReadWrite, data.VertexBuffer);
                        desc.UseResource(FGResourceUsage.ReadWrite, data.IndexBuffer);
                        desc.UseResource(FGResourceUsage.ReadWrite, data.GlobalBuffer);
                        desc.UseResource(FGResourceUsage.ReadWrite, data.MetadataBuffer);

                        if (!data.GradientsTexture.IsNull)
                            desc.UseResource(FGResourceUsage.Read, data.GradientsTexture);

                        if (!data.OutColor.IsExternal)
                            desc.UseRenderTarget(data.OutColor);

                        if (dockHost != null)
                            blackboard.Regions[blackboard.RegionCount++] = new UICompositeRegion(dockHost, data.OutColor, redraw.Region);

                        desc.SetRenderFunction<PassData>(PassFunction);
                    }
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            for (int i = 0; i < data.DataBlocks!.Length; ++i)
            {
                PropertyBlock block = data.DataBlocks[i];

                block.SetResource("cbGlobals", data.GlobalBuffer);
                block.SetResource("baMetadata", data.MetadataBuffer);
                block.SetResource("txGradients", data.GradientsTexture);
            }

            {
                cmd.Upload(data.VertexBuffer, data.Redraw.Context.Mesh.Vertices);
                cmd.Upload(data.IndexBuffer, data.Redraw.Context.Mesh.Indices);
                cmd.Upload(data.MetadataBuffer, data.Redraw.Context.Builder.Metadata);
            }

            cmd.SetVertexBuffer(data.VertexBuffer);
            cmd.SetIndexBuffer(data.IndexBuffer);

            cmd.SetRenderTarget(0, data.OutColor);

            Matrix3x2 globalModel =
                    Matrix3x2.CreateTranslation(Vector2.Truncate(data.Redraw.Window.ClientSize.AsVector2() * -0.5f)) *
                    Matrix3x2.CreateScale(Vector2.One / data.Redraw.Window.ClientSize.AsVector2() * 2.0f) *
                    Matrix3x2.CreateScale(1.0f, -1.0f);

            DrawContext drawCtx = data.Redraw.Context;

            ReadOnlySpan<BuiltDrawGroup> groups = drawCtx.Builder.Groups;
            ReadOnlySpan<BuiltDrawSegment> segments = drawCtx.Builder.Segments;

            int prevMatrixId = int.MinValue + 1;
            int prevClipId = int.MinValue + 1;

            int currentStartIndex = 0;
            for (int i = 0; i < groups.Length; ++i)
            {
                ref readonly BuiltDrawGroup group = ref groups[i];

                if (group.MatrixId != prevMatrixId)
                {
                    GlobalBufferData bufferData = new GlobalBufferData(group.MatrixId != int.MinValue ? drawCtx.Painter.Matricies.Get(group.MatrixId) : globalModel);
                    cmd.Upload(data.GlobalBuffer, bufferData);

                    prevMatrixId = group.MatrixId;
                }

                if (group.ClipId != prevClipId)
                {
                    if (group.ClipId == int.MinValue)
                        cmd.SetScissor(0, null);
                    else
                    {
                        Boundaries boundaries = drawCtx.Painter.ClipRects.Get(group.ClipId);
                        cmd.SetScissor(0, new FGRect((int)boundaries.Minimum.X, (int)boundaries.Minimum.Y, (int)boundaries.Maximum.X, (int)boundaries.Maximum.Y));
                    }

                    prevClipId = group.ClipId;
                }

                if (group.BlendMode != UIBlendMode.Undefined)
                    throw new NotImplementedException("Blend modes not supported yet");

                int nextLimit = currentStartIndex + group.SegmentCount;
                for (int j = currentStartIndex; j < nextLimit; ++j)
                {
                    ref readonly BuiltDrawSegment segment = ref segments[j];
                    int nextOffset = j == segments.Length - 1 ? drawCtx.Mesh.IndexCount : segments[j + 1].IndexOffset;

                    PropertyBlock dataBlock = data.DataBlocks![(int)GetDataBlockTarget(segment.Type)];
                    switch (segment.Type)
                    {
                        case BuiltSegmentType.Image:
                            {
                                if (segment.Value is TextureAsset texture)
                                    dataBlock.SetResource("txImage", texture);
                                else
                                    dataBlock.SetResource("txImage", (RHITexture)segment.Value!);
                                break;
                            }
                        case BuiltSegmentType.Text: dataBlock.SetResource("txFontAtlas", ((UIFontStyle)segment.Value!).AtlasTexture!); break;
                    }

                    cmd.SetPipeline(data.Shaders![(int)segment.Type].GraphicsPipeline!);
                    cmd.SetProperties(dataBlock);

                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc((uint)(nextOffset - segment.IndexOffset), StartIndexLocation: (uint)segment.IndexOffset));
                }

                currentStartIndex += group.SegmentCount;
            }

            static DataBlockTarget GetDataBlockTarget(BuiltSegmentType type) => type switch
            {
                BuiltSegmentType.Points => DataBlockTarget.Default,
                BuiltSegmentType.Lines => DataBlockTarget.Default,
                BuiltSegmentType.Rect => DataBlockTarget.Default,
                BuiltSegmentType.RoundedRect => DataBlockTarget.Default,
                BuiltSegmentType.Circle => DataBlockTarget.Default,
                BuiltSegmentType.Triangle => DataBlockTarget.Default,
                BuiltSegmentType.Image => DataBlockTarget.Image,
                BuiltSegmentType.Text => DataBlockTarget.Text,
                _ => throw new NotImplementedException(),
            };
        }

        private class PassData : IPassData
        {
            public UIRenderer? Renderer;

            public ShaderAsset[]? Shaders;
            public PropertyBlock[]? DataBlocks;

            public UIWindowRedraw Redraw;
            public FrameGraphTexture OutColor;

            public FrameGraphBuffer VertexBuffer;
            public FrameGraphBuffer IndexBuffer;

            public FrameGraphBuffer GlobalBuffer;
            public FrameGraphBuffer MetadataBuffer;

            public FrameGraphTexture GradientsTexture;

            public void Clear()
            {
                Renderer = null;

                Shaders = null;
                DataBlocks = null;

                Redraw = default;
                OutColor = FrameGraphTexture.Invalid;

                VertexBuffer = FrameGraphBuffer.Invalid;
                IndexBuffer = FrameGraphBuffer.Invalid;

                GlobalBuffer = FrameGraphBuffer.Invalid;
                MetadataBuffer = FrameGraphBuffer.Invalid;

                GradientsTexture = FrameGraphTexture.Invalid;
            }
        }

        internal class BlackboardData : IBlackboardData
        {
            public UICompositeRegion[]? Regions;
            public int RegionCount;

            public void Clear()
            {
                if (Regions?.Length > 0)
                    ArrayPool<UICompositeRegion>.Shared.Return(Regions, true);

                Regions = null;
                RegionCount = 0;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly record struct GlobalBufferData
        {
            [FieldOffset(0)]
            public readonly Vector3 M11_21;
            [FieldOffset(16)]
            public readonly Vector3 M22_32;

            public GlobalBufferData(Matrix3x2 model)
            {
                M11_21 = new Vector3(model.M11, model.M21, model.M31);
                M22_32 = new Vector3(model.M12, model.M22, model.M32);
            }
        }

        private enum DataBlockTarget : byte
        {
            Default = 0,
            Image,
            Text
        }
    }

    internal readonly record struct UICompositeRegion(UIDockHost Host, FrameGraphTexture Texture, Boundaries Region);
}
