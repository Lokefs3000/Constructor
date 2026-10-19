using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Datatypes;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiRenderPass : IRenderPass, IDisposable
    {
        private ShaderAsset _baseShader;
        private PropertyBlock _basePropertyBlock;

        public ImGuiRenderPass()
        {
            _baseShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/ImGui/BaseImGui.shader");
            _basePropertyBlock = _baseShader.CreatePropertyBlock();
        }

        public void Dispose()
        {
            _basePropertyBlock.Dispose();
        }

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            ImGuiContext? imgui = IMGUI.CurrentContext;
            if (imgui != null && !imgui.DrawLists.IsEmpty)
            {
                RenderWindowData windowData = frameGraph.GetContext<RenderWindowData>();
                if (!windowData.Window.IsPrimary)
                    return;

                if (_baseShader.Status != ResourceStatus.Success)
                {
                    imgui.ClearAllDrawLists();
                    return;
                }

                using (RasterPassBuilder desc = frameGraph.AddRasterPass("ImGui", out PassData data))
                {
                    int totalVtxCount = 0;
                    int totalIdxCount = 0;
                    foreach (var kvp in imgui.DrawLists)
                    {
                        totalVtxCount += kvp.Item1.Vertices.Length;
                        totalIdxCount += kvp.Item1.Indices.Length;
                    }

                    if (totalVtxCount == 0 || totalIdxCount == 0)
                    {
                        imgui.ClearAllDrawLists();
                        return;
                    }

                    data.OutColor = windowData.ColorTexture;

                    data.VertexBuffer = desc.CreateBuffer(new FGBufferDesc
                    {
                        ElementCount = totalVtxCount,
                        Stride = Unsafe.SizeOf<ImGuiVertex>(),
                        Target = FGBufferTarget.VertexInput
                    });
                    data.IndexBuffer = desc.CreateBuffer(new FGBufferDesc
                    {
                        ElementCount = totalIdxCount,
                        Stride = Unsafe.SizeOf<ushort>(),
                        Target = FGBufferTarget.IndexInput
                    });

                    data.Shader = _baseShader;
                    data.Properties = _basePropertyBlock;

                    desc.Write(data.OutColor);
                    desc.ReadWrite(data.VertexBuffer);
                    desc.ReadWrite(data.IndexBuffer);

                    desc.SetFunction(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
            ImGuiContext imgui = IMGUI.CurrentContext!;
            Window window = WindowManager.Instance.PrimaryWindow!;

            RasterCommandBuffer cmd = context.CommandBuffer;
            PassData data = context.GetPassData<PassData>()!;

            imgui.SortDrawLists();

            int globalVtxOffset = 0;
            int globalIdxOffset = 0;

            {
                using FGMappedSubresource<ImGuiVertex> vertices = cmd.Map<ImGuiVertex>(data.VertexBuffer);
                using FGMappedSubresource<ushort> indices = cmd.Map<ushort>(data.IndexBuffer);

                foreach (var kvp in imgui.DrawLists)
                {
                    ImGuiDrawList drawList = kvp.Item1;

                    drawList.Vertices.CopyTo(vertices.Span.Slice(globalVtxOffset));
                    drawList.Indices.CopyTo(indices.Span.Slice(globalIdxOffset));

                    globalVtxOffset += drawList.Vertices.Length;
                    globalIdxOffset += drawList.Indices.Length;
                }
            }

            globalVtxOffset = 0;
            globalIdxOffset = 0;

            RHITexture? lastTexture = null;
            Vector4 lastClipRect = Vector4.Zero;

            cmd.SetRenderTarget(0, data.OutColor);

            cmd.SetVertexBuffer(data.VertexBuffer);
            cmd.SetIndexBuffer(data.IndexBuffer);

            cmd.SetShader(data.Shader!);
            cmd.SetProperties(data.Properties!);

            Matrix3x2 matrix = Matrix3x2.Identity;

            matrix = Matrix3x2.CreateTranslation(Vector2.Truncate(window.ClientSize.AsVector2() * -0.5f));
            matrix *= Matrix3x2.CreateScale(new Vector2(2.0f) / window.ClientSize.AsVector2());
            matrix *= Matrix3x2.CreateScale(1.0f, -1.0f);

            cmd.SetConstants(matrix);

            foreach (var kvp in imgui.DrawLists)
            {
                ImGuiDrawList drawList = kvp.Item1;

                ReadOnlySpan<ImGuiDrawCmd> cmds = drawList.Cmds;
                for (int i = 0; i < cmds.Length; i++)
                {
                    ImGuiDrawCmd drawCmd = cmds[i];

                    int indexCount = 0;
                    if (i + 1 == cmds.Length)
                        indexCount = drawList.Indices.Length - drawCmd.IndexOffset;
                    else
                        indexCount = cmds[i + 1].IndexOffset - drawCmd.IndexOffset;

                    if (indexCount == 0)
                        continue;

                    if (lastTexture != drawCmd.Texture)
                    {
                        data.Properties!.SetResource("txTexture", drawCmd.Texture);
                        lastTexture = drawCmd.Texture;
                    }

                    if (lastClipRect != drawCmd.ClipRect)
                    {
                        cmd.SetScissor(0, new FGRect((int)drawCmd.ClipRect.X, (int)drawCmd.ClipRect.Y, (int)drawCmd.ClipRect.Z, (int)drawCmd.ClipRect.W));
                        lastClipRect = drawCmd.ClipRect;
                    }

                    cmd.DrawIndexedInstanced((uint)indexCount, 1, (uint)(globalIdxOffset + drawCmd.IndexOffset), globalVtxOffset, 0);
                }

                globalVtxOffset += drawList.Vertices.Length;
                globalIdxOffset += drawList.Indices.Length;
            }
            imgui.ClearAllDrawLists();

            data.Properties!.ClearResource("txTexture");
        }

        private sealed class PassData : IPassData
        {
            public TextureHandle OutColor;

            public BufferHandle VertexBuffer;
            public BufferHandle IndexBuffer;

            public ShaderAsset? Shader;
            public PropertyBlock? Properties;

            public void Clear()
            {
                OutColor = TextureHandle.Null;

                VertexBuffer = BufferHandle.Null;
                IndexBuffer = BufferHandle.Null;

                Shader = null;
                Properties = null;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly record struct Matrix3x2_Shader
        {
            [FieldOffset(0)]
            public readonly Vector3 M11_21;
            [FieldOffset(16)]
            public readonly Vector3 M22_32;

            public Matrix3x2_Shader(Matrix3x2 model)
            {
                M11_21 = new Vector3(model.M11, model.M21, model.M31);
                M22_32 = new Vector3(model.M12, model.M22, model.M32);
            }
        }
    }
}
