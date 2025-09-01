using Editor.Rendering.Gui;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Data;
using Primary.Rendering.Forward;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using SDL;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

using RHI = Primary.RHI;

namespace Editor.DearImGui
{
    internal sealed unsafe class DearImGuiStateManager : IDisposable
    {
        private Editor _editor;

        private ImGuiContextPtr _context;

        private bool _disposedValue;

        internal DearImGuiStateManager(Editor editor)
        {
            _editor = editor;
            _context = ImGui.CreateContext();

            ImGuiStylePtr style = ImGui.GetStyle();

            style.FrameRounding = 2.0f;
            style.FrameBorderSize = 1.0f;

            for (int i = 0; i < style.Colors.Length; i++)
            {
                style.Colors[i] = new Vector4(new Vector3(Vector128.Sum(style.Colors[i].AsVector3().AsVector128())) / 3.0f, style.Colors[i].W);
            }

            ImGuiIOPtr io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            editor.RenderingManager.RenderPassManager.AddRenderPass<DearImGuiRenderPass>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _editor.RenderingManager.RenderPassManager.RemoveRenderPass<DearImGuiRenderPass>();

                _editor.EventManager.EventRecieved -= EventRecived;

                ImGuiImplSDL3.Shutdown();
                ImGui.DestroyContext();
                _context = ImGuiContextPtr.Null;

                _disposedValue = true;
            }
        }

        ~DearImGuiStateManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void InitWindow(Window window)
        {
            ImGuiImplSDL3.SetCurrentContext(_context);
            ExceptionUtility.Assert(ImGuiImplSDL3.InitForOther(new SDLWindowPtr((SDLWindow*)window.InternalWindowInterop)));

            _editor.EventManager.EventRecieved += EventRecived;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BeginFrame()
        {
            ImGuiImplSDL3.NewFrame();
            ImGui.NewFrame();

            ImGui.DockSpaceOverViewport(ImGuiDockNodeFlags.PassthruCentralNode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EndFrame()
        {
            //ImGui.ShowStyleEditor();
            //ImGui.ShowDemoWindow();

            ImGui.Render();
        }

        private static void EventRecived(SDL_Event @event) => ImGuiImplSDL3.ProcessEvent(ref Unsafe.As<SDL_Event, SDLEvent>(ref @event));

        [RenderPassPriority(true, typeof(FinalBlitPass))]
        private class DearImGuiRenderPass : IRenderPass
        {
            private RHI.GraphicsDevice _device;

            private ShaderAsset _imguiShader;
            private ShaderBindGroup _bindGroup;

            private RHI.Buffer _cb;

            private RHI.Buffer? _vertexBuffer;
            private RHI.Buffer? _indexBuffer;

            private int _vtxCount;
            private int _idxCount;

            private HashSet<RHI.Texture> _activeTextures;

            public DearImGuiRenderPass()
            {
                RenderingManager manager = Editor.GlobalSingleton.RenderingManager;

                _device = manager.GraphicsDevice;

                _imguiShader = AssetManager.LoadAsset<ShaderAsset>("Hidden/Editor/DearImGui", true)!;
                _bindGroup = _imguiShader.CreateDefaultBindGroup();

                _cb = _device.CreateBuffer(new RHI.BufferDescription
                {
                    ByteWidth = 64u,
                    Stride = 64u,
                    Memory = RHI.MemoryUsage.Dynamic,
                    CpuAccessFlags = RHI.CPUAccessFlags.Write,
                    Mode = RHI.BufferMode.None,
                    Usage = RHI.BufferUsage.ConstantBuffer
                }, nint.Zero);

                _activeTextures = new HashSet<RHI.Texture>();

                ImGuiIOPtr io = ImGui.GetIO();
                io.BackendRendererUserData = null;
                io.BackendRendererName = null;
                io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasTextures;
            }

            public void Dispose()
            {
                ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
                for (int i = 0; i < platformIO.Textures.Size; i++)
                {
                    if (platformIO.Textures[i].RefCount == 1)
                    {
                        RHI.Resource? resource = RHI.Resource.FromIntPtr((nint)platformIO.Textures[i].BackendUserData);
                        ExceptionUtility.Assert(resource != null);

                        resource!.Dispose();
                    }
                }

                _vertexBuffer?.Dispose();
                _indexBuffer?.Dispose();

                ImGuiIOPtr io = ImGui.GetIO();
                io.BackendFlags &= ~(ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasTextures);
            }

            public void ExecutePass(IRenderPath path, RenderPassData passData)
            {
                ImDrawDataPtr drawData = ImGui.GetDrawData();
                if (drawData.IsNull || drawData.TotalVtxCount == 0 || drawData.TotalIdxCount == 0)
                    return;

                CommandBuffer commandBuffer = CommandBufferPool.Get();

                using (new CommandBufferEventScope(commandBuffer, "Dear ImGui"))
                {
                    {
                        RHI.SwapChain sw = Editor.GlobalSingleton.RenderingManager.SwapChainCache.GetOrAddDefault(Editor.GlobalSingleton.RenderingManager.DefaultWindow!);
                        commandBuffer.SetRenderTarget(sw.BackBuffer);
                    }

                    {
                        bool needsNewBuffer = _vertexBuffer == null || drawData.TotalVtxCount > _vtxCount;
                        if (needsNewBuffer)
                        {
                            _vertexBuffer?.Dispose();
                            _vertexBuffer = _device.CreateBuffer(new RHI.BufferDescription
                            {
                                ByteWidth = (uint)(drawData.TotalVtxCount * 2 * sizeof(ImDrawVert)),
                                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                                Memory = RHI.MemoryUsage.Dynamic,
                                Mode = RHI.BufferMode.None,
                                Stride = (uint)sizeof(ImDrawVert),
                                Usage = RHI.BufferUsage.VertexBuffer
                            }, nint.Zero);
                            _vtxCount = drawData.TotalVtxCount * 2;
                        }
                    }

                    {
                        bool needsNewBuffer = _indexBuffer == null || drawData.TotalIdxCount > _idxCount;
                        if (needsNewBuffer)
                        {
                            _indexBuffer?.Dispose();
                            _indexBuffer = _device.CreateBuffer(new RHI.BufferDescription
                            {
                                ByteWidth = (uint)(drawData.TotalIdxCount * 2 * sizeof(ushort)),
                                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                                Memory = RHI.MemoryUsage.Dynamic,
                                Mode = RHI.BufferMode.None,
                                Stride = sizeof(ushort),
                                Usage = RHI.BufferUsage.IndexBuffer
                            }, nint.Zero);
                            _idxCount = drawData.TotalIdxCount * 2;
                        }
                    }

                    {
                        ImDrawVert* vertices = (ImDrawVert*)commandBuffer.Map(_vertexBuffer!, RHI.MapIntent.Write, (ulong)(drawData.TotalVtxCount * sizeof(ImDrawVert)));
                        ushort* indices = (ushort*)commandBuffer.Map(_indexBuffer!, RHI.MapIntent.Write, (ulong)(drawData.TotalIdxCount * sizeof(ushort)));

                        for (int i = 0; i < drawData.CmdLists.Size; i++)
                        {
                            ImDrawListPtr drawList = drawData.CmdLists[i];

                            NativeMemory.Copy(drawList.VtxBuffer.Data, vertices, (nuint)(drawList.VtxBuffer.Size * sizeof(ImDrawVert)));
                            NativeMemory.Copy(drawList.IdxBuffer.Data, indices, (nuint)(drawList.IdxBuffer.Size * sizeof(ushort)));

                            vertices += drawList.VtxBuffer.Size;
                            indices += drawList.IdxBuffer.Size;
                        }

                        commandBuffer.Unmap(_vertexBuffer!);
                        commandBuffer.Unmap(_indexBuffer!);
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

                        Matrix4x4* cbData = (Matrix4x4*)commandBuffer.Map(_cb, RHI.MapIntent.Write, 64u);
                        *cbData = mm;
                        commandBuffer.Unmap(_cb);
                    }

                    _bindGroup.SetResource("cbVertex", _cb);

                    commandBuffer.SetShader(_imguiShader);
                    commandBuffer.SetBindGroups(_bindGroup);

                    commandBuffer.SetVertexBuffer(0, _vertexBuffer!);
                    commandBuffer.SetIndexBuffer(_indexBuffer!);

                    //commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 10000));

                    uint vtxOffset = 0;
                    uint idxOffset = 0;
                    for (int i = 0; i < drawData.CmdLists.Size; i++)
                    {
                        ImDrawListPtr drawList = drawData.CmdLists[i];
                        for (int j = 0; j < drawList.CmdBuffer.Size; j++)
                        {
                            ImDrawCmd cmd = drawList.CmdBuffer[j];
                            if (cmd.TexRef.TexData != null)
                            {
                                if (cmd.TexRef.TexData->BackendUserData == null)
                                    continue;
                                _bindGroup.SetResource("txTexture", RHI.Resource.FromIntPtr((nint)cmd.TexRef.TexData->BackendUserData) as RHI.Texture);
                            }
                            else
                            {
                                if (cmd.TexRef.TexID.Handle == 0)
                                    continue;

                                _bindGroup.SetResource("txTexture", RHI.Resource.FromIntPtr((nint)cmd.TexRef.TexID.Handle) as RHI.Texture);
                            }

                            commandBuffer.SetScissorRect(new RHI.ScissorRect((int)cmd.ClipRect.X, (int)cmd.ClipRect.Y, (int)cmd.ClipRect.Z, (int)cmd.ClipRect.W));

                            commandBuffer.CommitShaderResources();
                            commandBuffer.DrawIndexedInstanced(new RHI.DrawIndexedInstancedArgs(cmd.ElemCount, cmd.IdxOffset + idxOffset, (int)(cmd.VtxOffset + vtxOffset)));
                        }

                        vtxOffset += (uint)drawList.VtxBuffer.Size;
                        idxOffset += (uint)drawList.IdxBuffer.Size;
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
                                    ExceptionUtility.Assert(textureData.TexID == ImTextureID.Null);
                                    ExceptionUtility.Assert(textureData.Format == ImTextureFormat.Rgba32);

                                    uint* pixels = (uint*)textureData.GetPixels();

                                    RHI.Texture texture = _device.CreateTexture(new RHI.TextureDescription
                                    {
                                        Width = (uint)textureData.Width,
                                        Height = (uint)textureData.Height,
                                        Depth = 1,

                                        MipLevels = 1,

                                        Dimension = RHI.TextureDimension.Texture2D,
                                        Format = RHI.TextureFormat.RGBA8un,
                                        Memory = RHI.MemoryUsage.Default,
                                        Usage = RHI.TextureUsage.ShaderResource,
                                        CpuAccessFlags = RHI.CPUAccessFlags.None
                                    }, new Span<nint>(&pixels, 1));
                                    texture.Name = $"DearImGui - Texture [{textureData.UniqueID}]";

                                    _activeTextures.Add(texture);

                                    textureData.SetTexID(texture.Handle);
                                    textureData.SetStatus(ImTextureStatus.Ok);
                                    textureData.BackendUserData = texture.Handle.ToPointer();

                                    ImGuiIOPtr io = ImGui.GetIO();
                                }
                                else if (textureData.Status == ImTextureStatus.WantUpdates)
                                {
                                    RHI.Texture? texture = RHI.Resource.FromIntPtr((nint)textureData.BackendUserData) as RHI.Texture;
                                    ExceptionUtility.Assert(texture != null);

                                    ImTextureRect updateRect = textureData.UpdateRect;

                                    nint dataPointer = commandBuffer.Map(texture, RHI.MapIntent.ReadWrite, new RHI.TextureLocation
                                    {
                                        X = updateRect.X,
                                        Y = updateRect.Y,
                                        Z = 0,
                                        Width = updateRect.W,
                                        Height = updateRect.H,
                                        Depth = 1
                                    }, 0);

                                    ExceptionUtility.Assert(dataPointer != 0);

                                    int sliceSize = updateRect.W * 4;
                                    for (int j = 0; j < updateRect.H; j++)
                                    {
                                        NativeMemory.Copy(textureData.GetPixelsAt(updateRect.X, updateRect.Y + j), (dataPointer + sliceSize * j).ToPointer(), (uint)sliceSize);
                                    }

                                    commandBuffer.Unmap(texture);

                                    textureData.SetStatus(ImTextureStatus.Ok);
                                }
                                else if (textureData.Status == ImTextureStatus.WantDestroy && textureData.UnusedFrames > 0)
                                {
                                    if (textureData.BackendUserData == null)
                                        continue;

                                    RHI.Texture? texture = RHI.Resource.FromIntPtr((nint)textureData.BackendUserData) as RHI.Texture;
                                    ExceptionUtility.Assert(texture != null);

                                    texture.Dispose();

                                    textureData.SetTexID(ImTextureID.Null);
                                    textureData.SetStatus(ImTextureStatus.Destroyed);
                                    textureData.BackendUserData = null;
                                }
                            }
                        }
                    }
                }

                CommandBufferPool.Return(commandBuffer);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrepareFrame(IRenderPath path, RenderPassData passData) { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CleanupFrame(IRenderPath path, RenderPassData passData) { }
        }
    }
}
