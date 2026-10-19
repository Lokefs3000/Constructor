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
using Primary.Rendering.Datatypes;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI;

namespace VoxelizationDemo.Editor.Rendering
{
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

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            if (!_dearImGuiShader.IsLoaded)
                return;

            RenderWindowData windowData = frameGraph.GetContext<RenderWindowData>();
            ImDrawDataPtr drawData = ImGui.GetDrawData();

            if (!drawData.Valid || drawData.TotalVtxCount <= 0 || drawData.TotalIdxCount <= 0 || !windowData.Window.IsPrimary)
                return;

            using (RasterPassBuilder desc = frameGraph.AddRasterPass("DearImGui", out PassData passData))
            {
                passData.OutColor = windowData.ColorTexture;

                passData.VertexBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "DearImGui-Vtx",
                    ElementCount = drawData.TotalVtxCount,
                    Stride = Unsafe.SizeOf<ImDrawVert>(),
                    Target = FGBufferTarget.VertexInput
                });
                passData.IndexBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "DearImGui-Idx",
                    ElementCount = drawData.TotalIdxCount,
                    Stride = Unsafe.SizeOf<ushort>(),
                    Target = FGBufferTarget.IndexInput
                });

                passData.VertexData = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "DearImGui-Cb",
                    ElementCount = 1,
                    Stride = Unsafe.SizeOf<Matrix4x4>(),
                    Target = FGBufferTarget.Constant
                });

                passData.Shader = _dearImGuiShader;
                passData.Block = _propertyBlock;

                foreach (var (key, texture) in _activeTextures)
                {
                    passData.ActiveTextures.Add(key, desc.ImportTexture(texture));
                }

                passData.SourceActiveTextures = _activeTextures;

                desc.ReadWrite(passData.VertexBuffer);
                desc.ReadWrite(passData.IndexBuffer);
                desc.ReadWrite(passData.VertexData);

                desc.Write(passData.OutColor);
                desc.SetFunction(RenderFunction);
            }
        }

        private static unsafe void RenderFunction(RasterPassContext context)
        {
            PassData passData = context.GetPassData<PassData>()!;
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

                cmd.UploadBuffer(passData.VertexData, 0, mm);
            }

            cmd.SetRenderTarget(0, passData.OutColor);

            cmd.SetVertexBuffer(passData.VertexBuffer);
            cmd.SetIndexBuffer(passData.IndexBuffer);

            cmd.SetShader(passData.Shader!);

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

                    cmd.DrawIndexedInstanced(draw.ElemCount, 1, (uint)(draw.IdxOffset + idxOffset), (int)(draw.VtxOffset + vtxOffset), 0);
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

                                passData.SourceActiveTextures!.Add((nint)native, texture);

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
                            TextureHandle texture = passData.ActiveTextures![(nint)native];

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

                            cmd.UploadTexture(texture, 0, new FGBox(updateRect.X, updateRect.Y, 0, updateRect.X + updateRect.W, updateRect.Y + updateRect.H, 1), memory);

                            textureData.SetStatus(ImTextureStatus.Ok);
                        }
                        else if (textureData.Status == ImTextureStatus.WantDestroy && textureData.UnusedFrames > 0)
                        {
                            if (textureData.BackendUserData == null)
                                continue;

                            RHITextureNative* native = (RHITextureNative*)textureData.BackendUserData;
                            RHITexture texture = passData.SourceActiveTextures![(nint)native];

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
            public TextureHandle OutColor;

            public BufferHandle VertexBuffer;
            public BufferHandle IndexBuffer;

            public BufferHandle VertexData;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public Dictionary<nint, RHITexture>? SourceActiveTextures;
            public Dictionary<nint, TextureHandle> ActiveTextures;

            public PassData()
            {
                ActiveTextures = new Dictionary<nint, TextureHandle>();
            }

            public void Clear()
            {
                OutColor = TextureHandle.Null;

                VertexBuffer = BufferHandle.Null;
                IndexBuffer = BufferHandle.Null;

                VertexData = BufferHandle.Null;

                Shader = null;
                Block = null;

                SourceActiveTextures = null;
                ActiveTextures.Clear();
            }
        }
    }
}
