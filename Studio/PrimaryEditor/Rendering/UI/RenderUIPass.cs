using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using EditorUI.Assets;
using EditorUI.Built;
using EditorUI.Text;
using EditorUI.Text.Visual;
using EditorUI.Visual.Built;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using PrimaryEditor.Rendering.UI;

namespace EditorUI.Visual.Passes
{
    [RenderPassSetup(RunContext = RenderPassRunContext.PerWindow)]
    internal sealed class RenderUIPass : IRenderPass
    {
        private ShaderPack _shaderPack;

        private PropertyBlock _genericProperties;
        private PropertyBlock _fontProperties;
        private PropertyBlock _imageProperties;

        public RenderUIPass()
        {
            _shaderPack = new ShaderPack(
                Points: AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EditorUI/Standard/Points.shader"),
                Lines: AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EditorUI/Standard/Lines.shader"),
                Rectangle: AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EditorUI/Standard/Rectangle.shader"),
                Circle: AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EditorUI/Standard/Circle.shader"),
                Triangle: AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EditorUI/Standard/Triangle.shader"),
                Text: AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EditorUI/Standard/Text.shader"),
                Image: AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/EditorUI/Standard/Image.shader"));

            _genericProperties = _shaderPack.Points.CreatePropertyBlock();
            _fontProperties = _shaderPack.Text.CreatePropertyBlock();
            _imageProperties = _shaderPack.Image.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            VisualManager visual = UIManager.Instance.VisualManager;
            if (visual.HasPendingPaints)
            {
                RenderWindowData windowData = context.Get<RenderWindowData>()!;

                int incrIndex = 0;
                while (visual.TryGetPaintDataForWindow(windowData.Window, ref incrIndex, out Painter? painter))
                {
                    MeshBuilder meshBuilder = painter.MeshBuilder;

                    using (RasterPassDescription desc = renderPass.SetupRasterPass("RenderUI", out PassData passData))
                    {
                        passData.OutColor = windowData.ColorTexture;

                        // passData.Depth = desc.CreateTexture(new FrameGraphTextureDesc(passData.OutColor.Description)
                        // {
                        //     Format = RHIFormat.D24_UNorm_S8_UInt,
                        //     Usage = FGTextureUsage.DepthStencil
                        // }, "UIDepthTexture");

                        passData.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)(Unsafe.SizeOf<UIVertex>() * painter.VertexCount),
                            Stride = Unsafe.SizeOf<UIVertex>(),
                            Usage = FGBufferUsage.VertexBuffer
                        }, "UIVertexInput");

                        passData.IndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)(Unsafe.SizeOf<ushort>() * painter.IndexCount),
                            Stride = Unsafe.SizeOf<ushort>(),
                            Usage = FGBufferUsage.IndexBuffer
                        }, "UIIndexInput");

                        passData.GlobalsBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)Unsafe.SizeOf<GlobalDataBuffer>(),
                            Stride = Unsafe.SizeOf<GlobalDataBuffer>(),
                            Usage = FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader
                        }, "UIGlobalData");

                        passData.DataBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)meshBuilder.DataBuffer.Length,
                            Stride = 1,
                            Usage = FGBufferUsage.Raw | FGBufferUsage.PixelShader
                        }, "UIRawData");

                        passData.Shaders = _shaderPack;

                        passData.Properties = _genericProperties;
                        passData.FontProperties = _fontProperties;
                        passData.ImageProperties = _imageProperties;

                        passData.Painter = painter;

                        desc.UseRenderTarget(passData.OutColor);
                        // desc.UseDepthStencil(passData.Depth);

                        desc.UseResource(FGResourceUsage.ReadWrite, passData.VertexBuffer);
                        desc.UseResource(FGResourceUsage.ReadWrite, passData.IndexBuffer);

                        desc.UseResource(FGResourceUsage.ReadWrite, passData.GlobalsBuffer);
                        desc.UseResource(FGResourceUsage.ReadWrite, passData.DataBuffer);

                        desc.SetRenderFunction<PassData>(PassFunction);
                    }
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            RenderWindowData windowData = context.Container.Get<RenderWindowData>()!;

            MeshBuilder meshBuilder = passData.Painter!.MeshBuilder;

            // upload
            {
                cmd.Upload(passData.VertexBuffer, meshBuilder.Vertices[..passData.Painter.VertexCount]);
                cmd.Upload(passData.IndexBuffer, meshBuilder.Indices[..passData.Painter.IndexCount]);

                cmd.Upload(passData.DataBuffer, meshBuilder.DataBuffer);

                // Matrix3x2 model =
                //     Matrix3x2.CreateTranslation(Vector2.Truncate(passData.BuiltData.Window!.ClientSize.AsVector2() * -0.5f)) *
                //     Matrix3x2.CreateScale(Vector2.One / passData.BuiltData.Window.ClientSize.AsVector2() * 2.0f) *
                //     Matrix3x2.CreateScale(1.0f, -1.0f);
                Matrix4x4 model =
                    Matrix4x4.CreateOrthographic(windowData.Window.ClientSize.X, windowData.Window.ClientSize.Y, -1.0f, 1.0f) *
                    Matrix4x4.CreateTranslation(-1.0f, -1.0f, 0.0f) *
                    Matrix4x4.CreateScale(1.0f, -1.0f, 1.0f);

                GlobalDataBuffer globalData = new GlobalDataBuffer(model);
                cmd.Upload(passData.GlobalsBuffer, globalData);
            }

            // setup
            {
                // cmd.ClearDepthStencil(passData.Depth, FGClearFlags.Depth, null);

                cmd.SetRenderTarget(0, passData.OutColor);
                // cmd.SetDepthStencil(passData.Depth);

                cmd.SetVertexBuffer(passData.VertexBuffer);
                cmd.SetIndexBuffer(passData.IndexBuffer);

                passData.Properties!.SetResource("cbGlobalData", passData.GlobalsBuffer);
                passData.Properties!.SetResource("baDataBuffer", passData.DataBuffer);

                passData.FontProperties!.SetResource("cbGlobalData", passData.GlobalsBuffer);
                passData.FontProperties!.SetResource("baDataBuffer", passData.DataBuffer);

                passData.ImageProperties!.SetResource("cbGlobalData", passData.GlobalsBuffer);
                passData.ImageProperties!.SetResource("baDataBuffer", passData.DataBuffer);
            }

            // draw
            {
                ROList<MeshDrawSegment> segments = meshBuilder.Segments;
                for (int i = 0; i < segments.Count; ++i)
                {
                    MeshDrawSegment segment = segments[i];

                    int indexCount = (i == segments.Count - 1 ? passData.Painter.IndexCount : segments[i + 1].IndexStart) - segment.IndexStart;
                    if (indexCount == 0)
                        continue;

                    switch (segment.CmdType)
                    {
                        case PaintCmdType.Points:
                            {
                                cmd.SetPipeline(passData.Shaders.Points);
                                cmd.SetProperties(passData.Properties!);
                                break;
                            }
                        case PaintCmdType.Lines:
                            {
                                cmd.SetPipeline(passData.Shaders.Lines);
                                cmd.SetProperties(passData.Properties!);
                                break;
                            }
                        case PaintCmdType.Rectangle:
                            {
                                cmd.SetPipeline(passData.Shaders.Rectangle);
                                cmd.SetProperties(passData.Properties!);
                                break;
                            }
                        case PaintCmdType.Circle:
                            {
                                cmd.SetPipeline(passData.Shaders.Circle);
                                cmd.SetProperties(passData.Properties!);
                                break;
                            }
                        case PaintCmdType.Triangle:
                            {
                                cmd.SetPipeline(passData.Shaders.Triangle);
                                cmd.SetProperties(passData.Properties!);
                                break;
                            }
                        case PaintCmdType.Text:
                            {
                                IFontTexture? texture = ((FontStyleData)passData.Painter.ObjectList[segment.Argument]!).GlyphAtlas.FontTexture;
                                if (texture == null)
                                    continue;

                                cmd.SetPipeline(passData.Shaders.Text);
                                cmd.SetProperties(passData.FontProperties!);

                                passData.FontProperties!.SetResource("txFontAtlas", ((FontTextureFactory.FontTexture)texture).RawTexture);
                                break;
                            }
                        case PaintCmdType.Image:
                            {
                                cmd.SetPipeline(passData.Shaders.Image);
                                cmd.SetProperties(passData.ImageProperties!);

                                object? arg = passData.Painter.ObjectList[segment.Argument];
                                if (arg is TextureAsset texture)
                                    passData.ImageProperties!.SetResource("txImage", texture);
                                else
                                    passData.ImageProperties!.SetResource("txImage", (RHITexture)arg!);
                                break;
                            }
                    }

                    cmd.SetScissor(0, new FGRect(passData.Painter.ScissorList[segment.ClipIndex]));
                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc((uint)indexCount, StartIndexLocation: (uint)segment.IndexStart));
                }
            }

            passData.FontProperties!.ClearResource("txFontAtlas");
            passData.ImageProperties!.ClearResource("txImage");
        }

        private sealed class PassData : IPassData
        {
            public FrameGraphTexture OutColor;

            public FrameGraphTexture Depth;

            public FrameGraphBuffer VertexBuffer;
            public FrameGraphBuffer IndexBuffer;

            public FrameGraphBuffer GlobalsBuffer;
            public FrameGraphBuffer DataBuffer;

            public ShaderPack Shaders;

            public PropertyBlock? Properties;
            public PropertyBlock? FontProperties;
            public PropertyBlock? ImageProperties;

            public Painter? Painter;

            public void Clear()
            {
                OutColor = FrameGraphTexture.Invalid;

                Depth = FrameGraphTexture.Invalid;

                VertexBuffer = FrameGraphBuffer.Invalid;
                IndexBuffer = FrameGraphBuffer.Invalid;

                GlobalsBuffer = FrameGraphBuffer.Invalid;
                DataBuffer = FrameGraphBuffer.Invalid;

                Shaders = default;

                Properties = null;
                FontProperties = null;
                ImageProperties = null;

                Painter = null;
            }
        }

        private readonly record struct ShaderPack(ShaderAsset Points, ShaderAsset Lines, ShaderAsset Rectangle, ShaderAsset Circle, ShaderAsset Triangle, ShaderAsset Text, ShaderAsset Image);
        
        [StructLayout(LayoutKind.Explicit)]
        private readonly record struct GlobalDataBuffer
        {
            [FieldOffset(0)]
            public readonly Matrix4x4 Model;

            public GlobalDataBuffer(Matrix4x4 model)
            {
                Model = model;
            }

            // [FieldOffset(0)]
            // public readonly Vector3 M11_21;
            // [FieldOffset(16)]
            // public readonly Vector3 M22_32;
            // 
            // public GlobalDataBuffer(Matrix3x2 model)
            // {
            //     M11_21 = new Vector3(model.M11, model.M21, model.M31);
            //     M22_32 = new Vector3(model.M12, model.M22, model.M32);
            // }
        }
    }
}
