using Editor.LegacyGui;
using Editor.LegacyGui.Data;
using Editor.LegacyGui.Elements;
using Editor.Memory;
using Primary.Assets;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Rendering.Data;
using Primary.Rendering.Forward;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using RHI = Primary.RHI;

namespace Editor.Rendering.Gui
{
    //[RenderPassPriority(true, typeof(FinalBlitPass))]
    //internal sealed class GuiRenderer : IRenderPass
    //{
    //    private bool _disposedValue;
    //
    //    private GuiFont _defaultFont;
    //
    //    private ShaderAsset _shaderSolid;
    //    private ShaderAsset _shaderFont;
    //
    //    private ShaderBindGroup _shaderConstBinds;
    //    private ShaderBindGroup _shaderFontBinds;
    //
    //    private RHI.Buffer? _vertexBuffer;
    //    private RHI.Buffer? _indexBuffer;
    //
    //    private RHI.Buffer _cbCanvasBuffer;
    //
    //    private GuiCmdBuffers[] _commandBuffers;
    //    private GuiDrawBatcher _drawBatcher;
    //    private GuiMeshBuilder _meshBuilder;
    //
    //    private Queue<Element> _visualDrawElementQueue;
    //
    //    public GuiRenderer()
    //    {
    //        _defaultFont = EditorGuiManager.Instance.DefaultFont;
    //
    //        _shaderSolid = AssetManager.LoadAsset<ShaderAsset>("Hidden/Editor/EdGui_Solid") ?? throw new Exception("Resource must exist!");
    //        _shaderFont = AssetManager.LoadAsset<ShaderAsset>("Hidden/Editor/EdGui_Font") ?? throw new Exception("Resource must exist!");
    //
    //        _shaderConstBinds = new ShaderBindGroup("EdGuiConst", new ShaderBindGroupVariable(ShaderVariableType.ConstantBuffer, "cbCanvas"));
    //        _shaderFontBinds = _shaderFont.CreateDefaultBindGroup();
    //
    //        _cbCanvasBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
    //        {
    //            ByteWidth = (uint)Unsafe.SizeOf<cbCanvasDataStruct>(),
    //            Stride = (uint)Unsafe.SizeOf<cbCanvasDataStruct>(),
    //            CpuAccessFlags = RHI.CPUAccessFlags.Write,
    //            Memory = RHI.MemoryUsage.Dynamic,
    //            Mode = RHI.BufferMode.None,
    //            Usage = RHI.BufferUsage.ConstantBuffer
    //        }, nint.Zero);
    //
    //        _cbCanvasBuffer.Name = "EdGui - cbCanvas";
    //
    //        _commandBuffers = Array.Empty<GuiCmdBuffers>();
    //        _drawBatcher = new GuiDrawBatcher(_defaultFont);
    //        _meshBuilder = new GuiMeshBuilder();
    //
    //        _visualDrawElementQueue = new Queue<Element>();
    //    }
    //
    //    private void Dispose(bool disposing)
    //    {
    //        if (!_disposedValue)
    //        {
    //            if (disposing)
    //            {
    //                _vertexBuffer?.Dispose();
    //                _indexBuffer?.Dispose();
    //
    //                _cbCanvasBuffer?.Dispose();
    //            }
    //
    //            _disposedValue = true;
    //        }
    //    }
    //
    //    public void Dispose()
    //    {
    //        Dispose(disposing: true);
    //        GC.SuppressFinalize(this);
    //    }
    //
    //    public void PrepareFrame(IRenderPath path, RenderPassData passData)
    //    {
    //        using (new ProfilingScope("BuildUI"))
    //        {
    //            HashSet<DockSpace> dockSpaces = new HashSet<DockSpace>(); //Editor.GlobalSingleton.GuiManager.DockingManager.ActiveDockSpaces;
    //
    //            if (_commandBuffers.Length < dockSpaces.Count)
    //            {
    //                GuiCmdBuffers[] newArray = new GuiCmdBuffers[BitOperations.RoundUpToPowerOf2((uint)dockSpaces.Count)];
    //                for (int i = _commandBuffers.Length; i < newArray.Length; i++)
    //                {
    //                    LinearAllocator allocator = new LinearAllocator(4096);
    //                    newArray[i] = new GuiCmdBuffers(allocator, new GuiCommandBuffer(allocator));
    //                }
    //
    //                Array.Copy(_commandBuffers, newArray, _commandBuffers.Length);
    //                _commandBuffers = newArray;
    //            }
    //
    //            _meshBuilder.Clear();
    //
    //            int k = 0;
    //            foreach (DockSpace dockSpace in dockSpaces)
    //            {
    //                ref GuiCmdBuffers buffers = ref _commandBuffers[k++];
    //
    //                buffers.Allocator.Reset();
    //                buffers.CommandBuffer.Clear();
    //
    //                dockSpace.DrawVisual(buffers.CommandBuffer);
    //
    //                _drawBatcher.Build(dockSpace, buffers.CommandBuffer, _meshBuilder);
    //            }
    //        }
    //    }
    //
    //    public void ExecutePass(IRenderPath path, RenderPassData passData)
    //    {
    //        CommandBuffer commandBuffer = CommandBufferPool.Get();
    //
    //        using (new CommandBufferEventScope(commandBuffer, "GuiRenderer"))
    //        {
    //            if (!_meshBuilder.IsEmpty)
    //            {
    //                UploadBuffers(commandBuffer);
    //                ExecuteDraw(commandBuffer);
    //            }
    //
    //            BlitActiveUIWindows(commandBuffer, passData.Get<RenderPassViewportData>()!);
    //        }
    //
    //        CommandBufferPool.Return(commandBuffer);
    //
    //        _drawBatcher.Clear();
    //    }
    //
    //    private void UploadBuffers(CommandBuffer commandBuffer)
    //    {
    //        unsafe
    //        {
    //            {
    //                ulong dataSize = (ulong)_meshBuilder.VertexCount * (ulong)sizeof(GuiVertex);
    //                bool needsToRecreateBuffer = _vertexBuffer == null || _vertexBuffer.Description.ByteWidth < dataSize;
    //                if (needsToRecreateBuffer)
    //                {
    //                    _vertexBuffer?.Dispose();
    //                    _vertexBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
    //                    {
    //                        ByteWidth = (uint)dataSize,
    //                        CpuAccessFlags = RHI.CPUAccessFlags.Write,
    //                        Memory = RHI.MemoryUsage.Default,
    //                        Mode = RHI.BufferMode.None,
    //                        Stride = (uint)sizeof(GuiVertex),
    //                        Usage = RHI.BufferUsage.VertexBuffer
    //                    }, nint.Zero);
    //
    //                    _vertexBuffer.Name = "EdGui - VertexBuffer";
    //                }
    //
    //                nint vertexDataPointer = commandBuffer.Map(_vertexBuffer!, RHI.MapIntent.Write, dataSize);
    //                if (vertexDataPointer != nint.Zero)
    //                {
    //                    NativeMemory.Copy(Unsafe.AsPointer(ref _meshBuilder.Vertices[0]), vertexDataPointer.ToPointer(), (nuint)dataSize);
    //                }
    //                commandBuffer.Unmap(_vertexBuffer!);
    //            }
    //
    //            {
    //                ulong dataSize = (ulong)_meshBuilder.IndexCount * sizeof(ushort);
    //                bool needsToRecreateBuffer = _indexBuffer == null || _indexBuffer.Description.ByteWidth < dataSize;
    //                if (needsToRecreateBuffer)
    //                {
    //                    _indexBuffer?.Dispose();
    //                    _indexBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
    //                    {
    //                        ByteWidth = (uint)dataSize,
    //                        CpuAccessFlags = RHI.CPUAccessFlags.Write,
    //                        Memory = RHI.MemoryUsage.Default,
    //                        Mode = RHI.BufferMode.None,
    //                        Stride = (uint)sizeof(ushort),
    //                        Usage = RHI.BufferUsage.IndexBuffer
    //                    }, nint.Zero);
    //
    //                    _indexBuffer.Name = "EdGui - IndexBuffer";
    //                }
    //
    //                nint indexDataPointer = commandBuffer.Map(_indexBuffer!, RHI.MapIntent.Write, dataSize);
    //                if (indexDataPointer != nint.Zero)
    //                {
    //                    NativeMemory.Copy(Unsafe.AsPointer(ref _meshBuilder.Indices[0]), indexDataPointer.ToPointer(), (nuint)dataSize);
    //                }
    //                commandBuffer.Unmap(_indexBuffer!);
    //            }
    //        }
    //    }
    //
    //    private void ExecuteDraw(CommandBuffer commandBuffer)
    //    {
    //        _shaderConstBinds.SetResource("cbCanvas", _cbCanvasBuffer);
    //
    //        commandBuffer.SetVertexBuffer(0, _vertexBuffer!);
    //        commandBuffer.SetIndexBuffer(_indexBuffer);
    //
    //        Span<UIBatchContainer> containers = _drawBatcher.BatchContainers;
    //        for (int j = 0; j < containers.Length; j++)
    //        {
    //            using (new CommandBufferEventScope(commandBuffer, "DockSpace"))
    //            {
    //                UIBatchContainer container = containers[j];
    //                Span<UIBatch> batches = container.Batches;
    //
    //                SetupStateForRender(container.TargetDockSpace!, commandBuffer);
    //
    //                for (int i = 0; i < batches.Length - 1; i++)
    //                {
    //                    ref UIBatch batch = ref batches[i];
    //
    //                    switch (batch.Type)
    //                    {
    //                        case UIBatchType.Solid:
    //                            {
    //                                commandBuffer.ClearConstants();
    //
    //                                commandBuffer.SetShader(_shaderSolid);
    //                                commandBuffer.SetBindGroups(_shaderConstBinds);
    //                                break;
    //                            }
    //                        case UIBatchType.Text:
    //                            {
    //                                _shaderFontBinds.SetResource("txFontAtlas", (TextureAsset)batch.Value!);
    //
    //                                commandBuffer.SetShader(_shaderFont);
    //                                commandBuffer.SetBindGroups(_shaderConstBinds, _shaderFontBinds);
    //                                break;
    //                            }
    //                        default: continue;
    //                    }
    //                    commandBuffer.CommitShaderResources();
    //
    //                    uint indexCount = (i == batches.Length - 1) ? (uint)_meshBuilder.IndexCount - batch.IndexOffset : (batches[i + 1].IndexOffset - batch.IndexOffset);
    //                    commandBuffer.DrawIndexedInstanced(new RHI.DrawIndexedInstancedArgs(indexCount, batch.IndexOffset));
    //                }
    //            }
    //        }
    //    }
    //
    //    private void SetupStateForRender(DockSpace dockSpace, CommandBuffer commandBuffer)
    //    {
    //        Matrix4x4 additional = Matrix4x4.Identity; //dockSpace.Window == null ? Matrix4x4.Identity : Matrix4x4.CreateTranslation(new Vector3(-dockSpace.Position, 0.0f));
    //        WriteUpload(commandBuffer, _cbCanvasBuffer, new cbCanvasDataStruct
    //        {
    //            Projection = additional * (Matrix4x4.CreateOrthographic(dockSpace.Size.X, dockSpace.Size.Y, -1.0f, 1.0f) * Matrix4x4.CreateTranslation(-1.0f, -1.0f, 0.0f) * Matrix4x4.CreateScale(1.0f, -1.0f, 1.0f))
    //        });
    //
    //        commandBuffer.ClearRenderTarget(dockSpace.RenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
    //        commandBuffer.SetRenderTarget(dockSpace.RenderTarget);
    //        commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 100000));
    //    }
    //
    //    private void BlitActiveUIWindows(CommandBuffer commandBuffer, RenderPassViewportData viewportData)
    //    {
    //        HashSet<DockSpace> dockSpaces = new HashSet<DockSpace>(); //Editor.GlobalSingleton.GuiManager.DockingManager.ActiveDockSpaces;
    //
    //        foreach (DockSpace dockSpace in dockSpaces)
    //        {
    //            if (dockSpace.Parent != null)
    //                continue;
    //
    //            RHI.RenderTarget rt = dockSpace.SwapChain?.BackBuffer ?? viewportData.BackBufferRenderTarget;
    //            commandBuffer.SetRenderTarget(rt);
    //
    //            commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 10000, 10000));
    //            Blitter.Blit(commandBuffer, dockSpace.RenderTarget.ColorTexture!);
    //
    //            ReadOnlySpan<Element> children = dockSpace.Children;
    //            for (int i = 0; i < children.Length; i++)
    //            {
    //                BlitOnto((DockSpace)children[i], commandBuffer);
    //            }
    //        }
    //
    //        foreach (DockSpace dockSpace in dockSpaces)
    //        {
    //            dockSpace.SwapChain?.Present(RHI.PresentParameters.None);
    //        }
    //
    //        static void BlitOnto(DockSpace dockSpace, CommandBuffer commandBuffer)
    //        {
    //            Vector2 dimensions = dockSpace.Parent!.Size * 0.5f;
    //            Vector2 extents = dockSpace.Position + dockSpace.Size;
    //
    //            commandBuffer.SetScissorRect(new RHI.ScissorRect((int)dockSpace.Position.X, (int)dockSpace.Position.Y, (int)extents.X, (int)extents.Y));
    //
    //            Vector2 screenSize = dockSpace.Size / dimensions * 0.5f;
    //            Blitter.Blit(commandBuffer, dockSpace.RenderTarget.ColorTexture!, new Vector2(dockSpace.Position.X, -dockSpace.Position.Y) / dimensions + new Vector2(-1.0f + screenSize.X, 1.0f - screenSize.Y), screenSize);
    //
    //            ReadOnlySpan<Element> children = dockSpace.Children;
    //            for (int i = 0; i < children.Length; i++)
    //            {
    //                BlitOnto((DockSpace)children[i], commandBuffer);
    //            }
    //        }
    //    }
    //
    //    public void CleanupFrame(IRenderPath path, RenderPassData passData)
    //    {
    //
    //    }
    //
    //    private static unsafe void WriteUpload<T>(CommandBuffer commandBuffer, RHI.Buffer buffer, T data) where T : unmanaged
    //    {
    //        T* dataPointer = (T*)commandBuffer.Map(buffer, RHI.MapIntent.Write, (ulong)sizeof(T));
    //        if (dataPointer == null)
    //            return;
    //
    //        *dataPointer = data;
    //        commandBuffer.Unmap(buffer);
    //    }
    //
    //    private readonly record struct GuiCmdBuffers
    //    {
    //        public readonly LinearAllocator Allocator;
    //        public readonly GuiCommandBuffer CommandBuffer;
    //
    //        public GuiCmdBuffers(LinearAllocator allocator, GuiCommandBuffer commandBuffer)
    //        {
    //            Allocator = allocator;
    //            CommandBuffer = commandBuffer;
    //        }
    //    }
    //
    //    private struct cbCanvasDataStruct
    //    {
    //        public Matrix4x4 Projection;
    //    }
    //}
}
