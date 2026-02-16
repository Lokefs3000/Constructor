using Editor.UI.Assets;
using Primary.Assets;
using Primary.Common;
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
using System.Text;

namespace Editor.UI.Visual.Passes
{
    internal sealed class UIWindowRenderPass : IRenderPass
    {
        private ShaderAsset[] _shaders;
        private PropertyBlock[] _dataBlocks;

        public UIWindowRenderPass()
        {
            _shaders = new ShaderAsset[Enum.GetValues<ShaderPolyType>().Length];
            _dataBlocks = new PropertyBlock[Enum.GetValues<DataBlockPolyType>().Length];

            _shaders[(int)ShaderPolyType.Rectangle] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Rectangle.shader");
            _shaders[(int)ShaderPolyType.Traingle] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Triangle.shader");
            _shaders[(int)ShaderPolyType.Circle] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Circle.shader");
            _shaders[(int)ShaderPolyType.Text] = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EdGui/Primitives/Text.shader");

            _dataBlocks[(int)DataBlockPolyType.Default] = _shaders[(int)ShaderPolyType.Rectangle].WaitIfNotLoaded().CreatePropertyBlock()!;
            _dataBlocks[(int)DataBlockPolyType.Text] = _shaders[(int)ShaderPolyType.Text].WaitIfNotLoaded().CreatePropertyBlock()!;
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            UIRenderer renderer = UIManager.Instance.Renderer;
            if (renderer.AreAnyWindowsQueued)
            {
                BlackboardData blackboard = renderPass.Blackboard.Add<BlackboardData>()!;

                UIGenGradientsRenderPass.BlackboardData? gradientsBlackboard = renderPass.Blackboard.Get<UIGenGradientsRenderPass.BlackboardData>();

                using (RasterPassDescription desc = renderPass.SetupRasterPass("UI-DrawWnds", out PassData data))
                {
                    data.Renderer = renderer;

                    data.Shaders = _shaders;
                    data.DataBlocks = _dataBlocks;

                    data.Redraws = ArrayPool<WindowRedrawData>.Shared.Rent(renderer.WindowQueueSize);
                    data.RedrawCount = renderer.WindowQueueSize;

                    blackboard.Regions = ArrayPool<UICompositeRegion>.Shared.Rent(renderer.WindowQueueSize);
                    blackboard.RegionCount = renderer.WindowQueueSize;

                    int vtxCount = 0;
                    int idxCount = 0;

                    int metadataSize = 0;

                    int i = 0;
                    while (renderer.TryDequeueQueuedWindow(out UIWindowRedraw redraw))
                    {
                        Vector2 size = redraw.Region.Size;
                        Debug.Assert(size != Vector2.Zero);

                        FrameGraphTexture texture = desc.CreateTexture(new FrameGraphTextureDesc
                        {
                            Width = (int)size.X,
                            Height = (int)size.Y,
                            Depth = 1,

                            Dimension = FGTextureDimension._2D,
                            Format = RHIFormat.RGB10A2_UNorm,
                            Usage = FGTextureUsage.ShaderResource | FGTextureUsage.RenderTarget | FGTextureUsage.PixelShader,

                            Swizzle = new FrameGraphTextureSwizzle(FGTextureSwizzleChannel.Red, FGTextureSwizzleChannel.Green, FGTextureSwizzleChannel.Blue, FGTextureSwizzleChannel.One)
                        }, "UI-RedrawRT");

                        data.Redraws[i] = new WindowRedrawData(redraw.Window, redraw.CommandBuffer, redraw.Region, texture);
                        blackboard.Regions[i] = new UICompositeRegion(redraw.Window.ParentHost!, texture, redraw.Region);

                        ++i;

                        desc.UseResource(FGResourceUsage.Read, texture);
                        desc.UseRenderTarget(texture);

                        vtxCount += redraw.CommandBuffer.Vertices.Length;
                        idxCount += redraw.CommandBuffer.Indices.Length;

                        metadataSize += redraw.CommandBuffer.Metadata.Length;
                    }

                    data.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)(Unsafe.SizeOf<UIDrawVertex>() * vtxCount),
                        Stride = Unsafe.SizeOf<UIDrawVertex>(),
                        Usage = FGBufferUsage.VertexBuffer | FGBufferUsage.GenericShader
                    }, "UI-VertexBuf");

                    data.IndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)(Unsafe.SizeOf<ushort>() * idxCount),
                        Stride = Unsafe.SizeOf<ushort>(),
                        Usage = FGBufferUsage.IndexBuffer | FGBufferUsage.GenericShader
                    }, "UI-IndexBuf");

                    data.GlobalBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)Unsafe.SizeOf<GlobalBufferData>(),
                        Usage = FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader
                    }, "UI-GlobalData");

                    data.MetadataBuffer = metadataSize == 0 ? FrameGraphBuffer.Invalid : desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)metadataSize,
                        Usage = FGBufferUsage.Raw | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader
                    }, "UI-Metadata");

                    data.GradientsTexture = gradientsBlackboard?.Gradients ?? FrameGraphTexture.Invalid;

                    desc.UseResource(FGResourceUsage.ReadWrite, data.VertexBuffer);
                    desc.UseResource(FGResourceUsage.ReadWrite, data.IndexBuffer);
                    desc.UseResource(FGResourceUsage.ReadWrite, data.GlobalBuffer);
                    if (!data.MetadataBuffer.IsNull)
                        desc.UseResource(FGResourceUsage.ReadWrite, data.MetadataBuffer);
                    if (!data.GradientsTexture.IsNull)
                        desc.UseResource(FGResourceUsage.Read, data.GradientsTexture);

                    desc.SetRenderFunction<PassData>(PassFunction);
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
                int localVtxOffset = 0;
                int localIdxOffset = 0;

                int localMetadataOffset = 0;

                for (int i = 0; i < data.RedrawCount; i++)
                {
                    WindowRedrawData redrawData = data.Redraws![i];
                    UIBakedCommandBuffer bakedCmd = redrawData.CommandBuffer;

                    cmd.Upload(new FGBufferUploadDesc(data.VertexBuffer, (uint)localVtxOffset), bakedCmd.Vertices);
                    cmd.Upload(new FGBufferUploadDesc(data.IndexBuffer, (uint)localIdxOffset), bakedCmd.Indices);

                    if (!data.MetadataBuffer.IsNull)
                        cmd.Upload(new FGBufferUploadDesc(data.MetadataBuffer, (uint)localMetadataOffset), bakedCmd.Metadata);

                    localVtxOffset += bakedCmd.Vertices.Length * Unsafe.SizeOf<UIDrawVertex>();
                    localIdxOffset += bakedCmd.Indices.Length * Unsafe.SizeOf<ushort>();

                    localMetadataOffset += bakedCmd.Metadata.Length;
                }
            }

            cmd.SetVertexBuffer(data.VertexBuffer);
            cmd.SetIndexBuffer(data.IndexBuffer);

            int globalVtxOffset = 0;
            int globalIdxOffset = 0;

            int globalMetadataOffset = 0;

            for (int i = 0; i < data.RedrawCount; i++)
            {
                WindowRedrawData redrawData = data.Redraws![i];
                UIBakedCommandBuffer bakedCmd = redrawData.CommandBuffer;

                {
                    Matrix4x4 model =
                        Matrix4x4.CreateTranslation(new Vector3(-redrawData.Window.ClientSize * 0.5f, 0.0f)) *
                        Matrix4x4.CreateScale(1.0f, -1.0f, 1.0f) *
                        Matrix4x4.CreateOrthographic(redrawData.Window.ClientSize.X, redrawData.Window.ClientSize.Y, -1.0f, 1.0f);

                    GlobalBufferData bufferData = new GlobalBufferData(model, (uint)globalMetadataOffset);
                    cmd.Upload(data.GlobalBuffer, bufferData);
                }

                cmd.SetRenderTarget(0, redrawData.Texture);

                UIDrawSection lastDrawSection = new UIDrawSection(unchecked((UIDrawType)(-1)), 0, 0, 0, null);
                foreach (UIDrawSection section in bakedCmd.Sections)
                {
                    if (!section.Equals(lastDrawSection))
                    {
                        ShaderPolyType polyType = section.Type switch
                        {
                            UIDrawType.Rectangle => ShaderPolyType.Rectangle,
                            UIDrawType.Triangle => ShaderPolyType.Traingle,
                            UIDrawType.Circle => ShaderPolyType.Circle,

                            UIDrawType.ShapedText => ShaderPolyType.Text,
                            UIDrawType.SimpleText => ShaderPolyType.Text,

                            _ => throw new NotImplementedException(),
                        };

                        DataBlockPolyType blockType = section.Type switch
                        {
                            UIDrawType.Rectangle => DataBlockPolyType.Default,
                            UIDrawType.Triangle => DataBlockPolyType.Default,
                            UIDrawType.Circle => DataBlockPolyType.Default,

                            UIDrawType.ShapedText => DataBlockPolyType.Text,
                            UIDrawType.SimpleText => DataBlockPolyType.Text,

                            _ => throw new NotImplementedException(),
                        };

                        switch (blockType)
                        {
                            case DataBlockPolyType.Text:
                                {
                                    PropertyBlock block = data.DataBlocks[(int)DataBlockPolyType.Text];
                                    UIFontStyle style = Unsafe.As<UIFontStyle>(section.Aux!);

                                    block.SetResource("txFontAtlas", style.AtlasTexture!);
                                    break;
                                }
                        }

                        cmd.SetPipeline(data.Shaders![(int)polyType].GraphicsPipeline!);
                        cmd.SetProperties(data.DataBlocks![(int)blockType]);

                        lastDrawSection = section;
                    }

                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc((uint)section.IndexCount, 1, (uint)(section.IndexOffset + globalIdxOffset), section.BaseVertex + globalVtxOffset));
                }

                globalVtxOffset += bakedCmd.Vertices.Length;
                globalIdxOffset += bakedCmd.Indices.Length;

                globalMetadataOffset += bakedCmd.Metadata.Length;
            }
        }

        private class PassData : IPassData
        {
            public UIRenderer? Renderer;

            public ShaderAsset[]? Shaders;
            public PropertyBlock[]? DataBlocks;

            public WindowRedrawData[]? Redraws;
            public int RedrawCount;

            public FrameGraphBuffer VertexBuffer;
            public FrameGraphBuffer IndexBuffer;

            public FrameGraphBuffer GlobalBuffer;
            public FrameGraphBuffer MetadataBuffer;

            public FrameGraphTexture GradientsTexture;

            public void Clear()
            {
                if (Redraws?.Length > 0)
                    ArrayPool<WindowRedrawData>.Shared.Return(Redraws, true);

                Renderer = null;

                Shaders = null;
                DataBlocks = null;

                Redraws = null;
                RedrawCount = 0;

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

        private record struct WindowRedrawData(UIWindow Window, UIBakedCommandBuffer CommandBuffer, Boundaries Region, FrameGraphTexture Texture);
        private readonly record struct GlobalBufferData(Matrix4x4 Model, uint MetadataOffset);

        private enum ShaderPolyType : byte
        {
            Rectangle = 0,
            Traingle,
            Circle,
            Text
        }

        private enum DataBlockPolyType : byte
        {
            Default = 0,
            Text
        }
    }

    internal readonly record struct UICompositeRegion(UIDockHost Host, FrameGraphTexture Texture, Boundaries Region);
}
