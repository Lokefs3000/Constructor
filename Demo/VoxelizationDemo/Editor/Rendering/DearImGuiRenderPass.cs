using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Common;
using Primary.Memory.Native;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;

namespace VoxelizationDemo.Editor.Rendering
{
    [RenderPassSetup(RunContext = RenderPassRunContext.PerWindow)]
    internal sealed class DearImGuiRenderPass : IRenderPass, IDisposable
    {
        private ShaderAsset _dearImGuiShader;
        private PropertyBlock _propertyBlock;

        private Dictionary<nint, RHITexture> _activeTextures;
        private RHITexture? _primaryTexture;

        public DearImGuiRenderPass()
        {
            _dearImGuiShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Editor/DearImGui.shader");
            _propertyBlock = _dearImGuiShader.CreatePropertyBlock();

            _activeTextures = new Dictionary<nint, RHITexture>();
            _primaryTexture = null;

            unsafe
            {
                ImGuiIOPtr io = ImGui.GetIO();
                io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures | ImGuiBackendFlags.RendererHasVtxOffset;
            }
        }

        public void Dispose()
        {
            foreach (var (_, texture) in _activeTextures)
            {
                texture.Dispose();
            }

            _primaryTexture?.Dispose();
            _primaryTexture = null;
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            if (!_dearImGuiShader.IsLoaded)
                return;

            RenderWindowData windowData = context.Get<RenderWindowData>()!;
            ImDrawDataPtr drawData = ImGui.GetDrawData();

            if (!drawData.Valid || drawData.TotalVtxCount <= 0 || drawData.TotalIdxCount <= 0 || !windowData.Window.IsPrimary)
                return;

            using (RasterPassDescription desc = renderPass.SetupRasterPass("DearImGui", out PassData passData))
            {
                passData.OutColor = windowData.ColorTexture;

                passData.VertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>()),
                    Stride = Unsafe.SizeOf<ImDrawVert>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.VertexBuffer
                }, "DearImGui-Vtx");
                passData.IndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(drawData.TotalIdxCount * Unsafe.SizeOf<ushort>()),
                    Stride = Unsafe.SizeOf<ushort>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.IndexBuffer
                }, "DearImGui-Idx");

                passData.VertexData = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)Unsafe.SizeOf<Matrix4x4>(),
                    Stride = Unsafe.SizeOf<Matrix4x4>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer
                }, "DearImGui-Cb");

                passData.Shader = _dearImGuiShader;
                passData.Block = _propertyBlock;

                passData.ActiveTextures = _activeTextures;

                desc.UseResource(FGResourceUsage.ReadWrite, passData.VertexBuffer);
                desc.UseResource(FGResourceUsage.ReadWrite, passData.IndexBuffer);
                desc.UseResource(FGResourceUsage.ReadWrite, passData.VertexData);

                desc.UseRenderTarget(passData.OutColor);
                desc.SetRenderFunction<PassData>(RenderFunction);
            }
        }

        private static unsafe void RenderFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;
            ImDrawDataPtr drawData = ImGui.GetDrawData();

            {
                using FGMappedSubresource<ImDrawVert> vertices = cmd.Map<ImDrawVert>(passData.VertexBuffer);
                using FGMappedSubresource<ushort> indices = cmd.Map<ushort>(passData.IndexBuffer);

                int offsetVtx = 0;
                int offsetIdx = 0;

                fixed (ImDrawVert* ptr1 = vertices.Span)
                fixed (ushort* ptr2 = indices.Span)
                {
                    for (int i = 0; i < drawData.CmdListsCount; i++)
                    {
                        ImDrawListPtr list = drawData.CmdLists[i];

                        Debug.Assert(offsetVtx + list.VtxBuffer.Size <= vertices.Span.Length);
                        Debug.Assert(offsetIdx + list.IdxBuffer.Size <= indices.Span.Length);

                        NativeMemory.Copy(list.VtxBuffer.Data, ptr1 + offsetVtx, (nuint)(list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));
                        NativeMemory.Copy(list.IdxBuffer.Data, ptr2 + offsetIdx, (nuint)(list.IdxBuffer.Size * Unsafe.SizeOf<ushort>()));

                        offsetVtx += list.VtxBuffer.Size;
                        offsetIdx += list.IdxBuffer.Size;
                    }
                }
            }

            {
                float L = drawData.DisplayPos.X;
                float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
                float T = drawData.DisplayPos.Y;
                float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

                Matrix4x4 mm = new Matrix4x4(
                    2.0f / (R - L), 0.0f, 0.0f, 0.0f,
                    0.0f, 2.0f / (T - B), 0.0f, 0.0f,
                    0.0f, 0.0f, 0.5f, 0.0f,
                    (R + L) / (L - R), (T + B) / (B - T), 0.5f, 1.0f);

                cmd.Upload(passData.VertexData, mm);
            }

            cmd.SetRenderTarget(0, passData.OutColor);

            cmd.SetVertexBuffer(passData.VertexBuffer);
            cmd.SetIndexBuffer(passData.IndexBuffer);

            cmd.SetPipeline(passData.Shader!);

            passData.Block!.SetResource("cbVertex", passData.VertexData);

            int vtxOffset = 0;
            int idxOffset = 0;

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr list = drawData.CmdLists[i];
                for (int j = 0; j < list.CmdBuffer.Size; j++)
                {
                    ImDrawCmd draw = list.CmdBuffer[j];
                    if (draw.TexRef.TexData != null)
                    {
                        if (draw.TexRef.TexData->BackendUserData == null)
                            continue;
                        passData.Block!.SetResource("txTexture", passData.ActiveTextures![(nint)draw.TexRef.TexData->BackendUserData]);
                    }
                    else
                    {
                        if (draw.TexRef.TexID.Handle == 0)
                            continue;

                        nint native = (nint)draw.TexRef.TexID.Handle;
                        if ((native & HandleAllocator.IsGCHandleBit) != 0)
                        {
                            GCHandle handle = GCHandle.FromIntPtr(native & ~HandleAllocator.IsGCHandleBit);
                            object? target = handle.Target;

                            if (target is TextureAsset texture)
                                passData.Block!.SetResource("txTexture", texture);
                            else if (target is RHITexture rhi)
                                passData.Block!.SetResource("txTexture", rhi);
                            else
                                passData.Block!.ClearResource("txTexture");
                        }
                        else
                        {
                            passData.Block!.SetResource("txTexture", passData.ActiveTextures![native]);
                        }
                    }

                    cmd.SetScissor(0, new FGRect((int)draw.ClipRect.X, (int)draw.ClipRect.Y, (int)draw.ClipRect.Z, (int)draw.ClipRect.W));
                    cmd.SetProperties(passData.Block!);

                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(draw.ElemCount, 1, (uint)(draw.IdxOffset + idxOffset), (int)(draw.VtxOffset + vtxOffset)));
                }

                vtxOffset += list.VtxBuffer.Size;
                idxOffset += list.IdxBuffer.Size;
            }

            ImVector<ImTextureDataPtr> textures = *drawData.Handle->Textures;
            if (textures.Size > 0)
            {
                for (int i = 0; i < textures.Size; i++)
                {
                    ImTextureDataPtr textureData = textures[i];

                    if (textureData.Status != ImTextureStatus.Ok)
                    {
                        if (textureData.Status == ImTextureStatus.WantCreate)
                        {
                            if (textureData.Width > 0 && textureData.Height > 0)
                            {
                                ExceptionUtility.Assert(textureData.TexID == ImTextureID.Null);
                                ExceptionUtility.Assert(textureData.Format == ImTextureFormat.Rgba32);

                                ArrayPtr<byte> pixels = new ArrayPtr<byte>((byte*)textureData.GetPixels(), textureData.Width * textureData.Height * 4);

                                RHITexture texture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                                {
                                    Width = textureData.Width,
                                    Height = textureData.Height,
                                    Depth = 1,

                                    MipLevels = 1,

                                    Dimension = RHIDimension.Texture2D,
                                    Format = RHIFormat.RGBA8_UNorm,
                                    Usage = RHIResourceUsage.ShaderResource,

                                    Swizzle = RHISwizzle.RGBA,
                                }, new Span<ArrayPtr<byte>>(ref pixels), $"DearImGui - Texture [{textureData.UniqueID}]")!;

                                RHITextureNative* native = texture.GetAsNative();

                                passData.ActiveTextures!.Add((nint)native, texture);

                                textureData.SetTexID(native);
                                textureData.SetStatus(ImTextureStatus.Ok);
                                textureData.BackendUserData = native;

                                ImGuiIOPtr io = ImGui.GetIO();
                            }
                            else
                            {
                                textureData.SetTexID(ImTextureID.Null);
                                textureData.BackendUserData = null;
                            }
                        }
                        else if (textureData.Status == ImTextureStatus.WantUpdates)
                        {
                            RHITextureNative* native = (RHITextureNative*)textureData.BackendUserData;
                            RHITexture texture = passData.ActiveTextures![(nint)native];

                            ImTextureRect updateRect = textureData.UpdateRect;

                            int paddedRowSlice = updateRect.W * 4;//(updateRect.W + (-updateRect.W & 255)) * 4;
                            byte[] memory = new byte[paddedRowSlice * updateRect.H * 4];

                            fixed (byte* dataPointer = memory)
                            {
                                int sliceSize = updateRect.W * 4;
                                for (int j = 0; j < updateRect.H; j++)
                                {
                                    NativeMemory.Copy(textureData.GetPixelsAt(updateRect.X, updateRect.Y + j), (dataPointer + paddedRowSlice * j), (uint)sliceSize);
                                }
                            }

                            cmd.Upload(new FGTextureUploadDesc(texture, new FGBox(updateRect.X, updateRect.Y, 0, updateRect.W, updateRect.H, 1), 0, paddedRowSlice), memory);

                            textureData.SetStatus(ImTextureStatus.Ok);
                        }
                        else if (textureData.Status == ImTextureStatus.WantDestroy && textureData.UnusedFrames > 0)
                        {
                            if (textureData.BackendUserData == null)
                                continue;

                            RHITextureNative* native = (RHITextureNative*)textureData.BackendUserData;
                            RHITexture texture = passData.ActiveTextures![(nint)native];

                            texture?.Dispose();

                            textureData.SetTexID(ImTextureID.Null);
                            textureData.SetStatus(ImTextureStatus.Destroyed);
                            textureData.BackendUserData = null;
                        }
                    }
                }
            }
        }

        private class PassData : IPassData
        {
            public FrameGraphTexture OutColor;

            public FrameGraphBuffer VertexBuffer;
            public FrameGraphBuffer IndexBuffer;

            public FrameGraphBuffer VertexData;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public Dictionary<nint, RHITexture>? ActiveTextures;

            public void Clear()
            {
                OutColor = FrameGraphTexture.Invalid;

                VertexBuffer = FrameGraphBuffer.Invalid;
                IndexBuffer = FrameGraphBuffer.Invalid;

                VertexData = FrameGraphBuffer.Invalid;

                Shader = null;
                Block = null;

                ActiveTextures = null;
            }
        }
    }
}
