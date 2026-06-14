using Editor.Gui.View;
using Editor.Interaction;
using Primary.Assets;
using Primary.Common;
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

namespace Editor.Rendering.Tools
{
    internal sealed class ToolsRenderPass : IRenderPass
    {
        private ToolDrawData _drawData;

        private ShaderAsset _translateShader;
        private ShaderAsset _handleShader;
        private PropertyBlock _properties;

        public ToolsRenderPass()
        {
            _drawData = new ToolDrawData();

            _translateShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Tools/Translate.shader");
            _handleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Tools/Handles.shader");
            _properties = _translateShader.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            ToolManager tools = ToolManager.Instance;
            if (tools.CurrentTool == null)
                return;

            _drawData.ResetToDefault();
            if (tools.CurrentTool.Render(tools, _drawData))
            {
                RenderCameraData cameraData = context.Get<RenderCameraData>()!;
                using (RasterPassDescription desc = renderPass.SetupRasterPass("Tools", out PassData passData))
                {
                    passData.DrawData = _drawData;
                    passData.OutColor = cameraData.ColorTexture;

                    switch (_drawData.HandleType)
                    {
                        case ToolHandleType.Translate:
                            {
                                passData.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                                {
                                    Width = (uint)(Unsafe.SizeOf<TranslateVertex>() * 15),
                                    Stride = Unsafe.SizeOf<TranslateVertex>(),
                                    Usage = FGBufferUsage.VertexBuffer
                                }, "HandleVertices");

                                passData.IndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                                {
                                    Width = (uint)(Unsafe.SizeOf<ushort>() * 24),
                                    Stride = Unsafe.SizeOf<ushort>(),
                                    Usage = FGBufferUsage.IndexBuffer
                                });

                                passData.SolidShader = _translateShader;
                                passData.LineShader = _handleShader;
                                passData.Properties = _properties;
                                break;
                            }
                    }

                    desc.UseResource(FGResourceUsage.ReadWrite, passData.VertexBuffer);
                    desc.UseResource(FGResourceUsage.ReadWrite, passData.IndexBuffer);
                    desc.UseRenderTarget(passData.OutColor);

                    desc.SetRenderFunction<PassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            EditorCamera camera = EditorCamera.Instance;
            Vector3 camPosition = camera.Position - passData.DrawData!.HandleOrigin;

            if (passData.DrawData!.HandleType == ToolHandleType.Translate)
            {
                RenderCameraData cameraData = context.Container.Get<RenderCameraData>()!;

                float scale = Math.Clamp(Vector3.Distance(camera.Position, passData.DrawData.HandleOrigin) * passData.DrawData.HandleScale * 0.15f, 0.25f, 2.0f);

                Vector3 xAxis = Vector3.Transform(Vector3.UnitX, passData.DrawData.HandleRotation) * scale;
                Vector3 yAxis = Vector3.Transform(Vector3.UnitY, passData.DrawData.HandleRotation) * scale;
                Vector3 zAxis = Vector3.Transform(Vector3.UnitZ, passData.DrawData.HandleRotation) * scale;

                {
                    using FGMappedSubresource<TranslateVertex> vertices = cmd.Map<TranslateVertex>(passData.VertexBuffer);
                    using FGMappedSubresource<ushort> indices = cmd.Map<ushort>(passData.IndexBuffer);

                    Span<TranslateVertex> vSpan = vertices.Span;
                    Span<ushort> iSpan = indices.Span;

                    const float ArrowStep = 0.7f;
                    const float ArrowWidth = 0.15f;

                    Vector3 xColor = passData.DrawData.HandleAxisColors[(int)ToolHandleAxis.X].AsVector3();
                    Vector3 yColor = passData.DrawData.HandleAxisColors[(int)ToolHandleAxis.Y].AsVector3();
                    Vector3 zColor = passData.DrawData.HandleAxisColors[(int)ToolHandleAxis.Z].AsVector3();

                    bool isBesideX = Vector3.Dot(xAxis, camPosition) < 0.0f;
                    bool isBesideY = Vector3.Dot(yAxis, camPosition) < 0.0f;
                    bool isBesideZ = Vector3.Dot(zAxis, camPosition) < 0.0f;

                    // handles
                    {
                        vSpan[0] = new TranslateVertex(passData.DrawData.HandleOrigin, xColor);
                        vSpan[1] = new TranslateVertex(passData.DrawData.HandleOrigin, yColor);
                        vSpan[2] = new TranslateVertex(passData.DrawData.HandleOrigin, zColor);

                        vSpan[3] = new TranslateVertex(passData.DrawData.HandleOrigin + (isBesideX ? -xAxis : xAxis), xColor);
                        vSpan[4] = new TranslateVertex(passData.DrawData.HandleOrigin + (isBesideY ? -yAxis : yAxis), yColor);
                        vSpan[5] = new TranslateVertex(passData.DrawData.HandleOrigin + (isBesideZ ? -zAxis : zAxis), zColor);

                        iSpan[0] = 0;
                        iSpan[1] = 3;

                        iSpan[2] = 1;
                        iSpan[3] = 4;

                        iSpan[4] = 2;
                        iSpan[5] = 5;
                    }

                    // x arrow
                    {
                        bool isBelow = Vector3.Dot(yAxis, camPosition) < 0.0f;
                        bool isBehind = Vector3.Dot(zAxis, camPosition) >= 0.0f;

                        Vector3 stepped = Vector3.Lerp(passData.DrawData.HandleOrigin, passData.DrawData.HandleOrigin + (isBesideX ? -xAxis : xAxis), ArrowStep);

                        vSpan[6] = new TranslateVertex(stepped, xColor);
                        vSpan[7] = new TranslateVertex(isBelow ? (stepped - yAxis * ArrowWidth) : (stepped + yAxis * ArrowWidth), xColor);
                        vSpan[8] = new TranslateVertex(isBehind ? (stepped + zAxis * ArrowWidth) : (stepped - zAxis * ArrowWidth), xColor);

                        iSpan[6] = 3;
                        iSpan[7] = 6;
                        iSpan[8] = 7;

                        iSpan[9] = 3;
                        iSpan[10] = 6;
                        iSpan[11] = 8;
                    }

                    // y arrow
                    {
                        bool isBelow = Vector3.Dot(xAxis, camPosition) >= 0.0f;
                        bool isBehind = Vector3.Dot(zAxis, camPosition) >= 0.0f;

                        Vector3 stepped = Vector3.Lerp(passData.DrawData.HandleOrigin, passData.DrawData.HandleOrigin + (isBesideY ? -yAxis : yAxis), ArrowStep);

                        vSpan[9] = new TranslateVertex(stepped, yColor);
                        vSpan[10] = new TranslateVertex(isBelow ? (stepped + xAxis * ArrowWidth) : (stepped - xAxis * ArrowWidth), yColor);
                        vSpan[11] = new TranslateVertex(isBehind ? (stepped + zAxis * ArrowWidth) : (stepped - zAxis * ArrowWidth), yColor);

                        iSpan[12] = 4;
                        iSpan[13] = 9;
                        iSpan[14] = 10;

                        iSpan[15] = 4;
                        iSpan[16] = 9;
                        iSpan[17] = 11;
                    }

                    // z arrow
                    {
                        bool isBelow = Vector3.Dot(xAxis, camPosition) >= 0.0f;
                        bool isBehind = Vector3.Dot(yAxis, camPosition) < 0.0f;

                        Vector3 stepped = Vector3.Lerp(passData.DrawData.HandleOrigin, passData.DrawData.HandleOrigin + (isBesideZ ? -zAxis : zAxis), ArrowStep);

                        vSpan[12] = new TranslateVertex(stepped, zColor);
                        vSpan[13] = new TranslateVertex(isBelow ? (stepped + xAxis * ArrowWidth) : (stepped - xAxis * ArrowWidth), zColor);
                        vSpan[14] = new TranslateVertex(isBehind ? (stepped - yAxis * ArrowWidth) : (stepped + yAxis * ArrowWidth), zColor);

                        iSpan[18] = 5;
                        iSpan[19] = 12;
                        iSpan[20] = 13;

                        iSpan[21] = 5;
                        iSpan[22] = 12;
                        iSpan[23] = 14;
                    }
                }

                {
                    cmd.SetRenderTarget(0, passData.OutColor);

                    cmd.SetVertexBuffer(passData.VertexBuffer);
                    cmd.SetIndexBuffer(passData.IndexBuffer);

                    cmd.SetProperties(passData.Properties!);

                    cmd.SetPipeline(passData.LineShader!);
                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(6));

                    cmd.SetPipeline(passData.SolidShader!);
                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(18, StartIndexLocation: 6));
                }
            }
        }

        private sealed class PassData : IPassData
        {
            public ToolDrawData? DrawData;

            public ShaderAsset? SolidShader;
            public ShaderAsset? LineShader;
            public PropertyBlock? Properties;

            public FrameGraphTexture OutColor;
            public FrameGraphBuffer VertexBuffer;
            public FrameGraphBuffer IndexBuffer;

            public void Clear()
            {
                DrawData = null;

                SolidShader = null;
                LineShader = null;
                Properties = null;

                OutColor = FrameGraphTexture.Invalid;
                VertexBuffer = FrameGraphBuffer.Invalid;
                IndexBuffer = FrameGraphBuffer.Invalid;
            }
        }

        private readonly record struct TranslateVertex(Vector3 Position, Vector3 Color);
    }
}
