using Editor.Gui.Elements;
using Primary;
using Primary.Assets;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Rendering.Data;
using Primary.Rendering.Forward;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using RHI = Primary.RHI;

namespace Editor.Gui.Graphics
{
    [RenderPassPriority(true, typeof(FinalBlitPass))]
    internal sealed class GuiRenderPass : IRenderPass
    {
        private GuiCommandBuffer _commandBuffer;

        private RHI.Buffer _cbCanvasBuffer;

        private RHI.Buffer? _vertexBuffer;
        private RHI.Buffer? _indexBuffer;

        private int _vertexBufferSize;
        private int _indexBufferSize;

        private ShaderAsset _imageShader;
        private ShaderAsset _fontShader;

        private ShaderBindGroup _constSbg;
        private ShaderBindGroup _defaultSbg;

        private bool _disposedValue;

        public GuiRenderPass()
        {
            _commandBuffer = new GuiCommandBuffer();

            _cbCanvasBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<CbCanvasData>(),
                Stride = (uint)Unsafe.SizeOf<CbCanvasData>(),
                Memory = RHI.MemoryUsage.Dynamic,
                Usage = RHI.BufferUsage.ConstantBuffer,
                CpuAccessFlags = RHI.CPUAccessFlags.Write
            }, nint.Zero);

            _vertexBuffer = null;
            _indexBuffer = null;

            _vertexBufferSize = 0;
            _indexBufferSize = 0;

            _imageShader = NullableUtility.AlwaysThrowIfNull(AssetManager.LoadAsset<ShaderAsset>("Hidden/Editor/EdGui_Image", true));
            _fontShader = NullableUtility.AlwaysThrowIfNull(AssetManager.LoadAsset<ShaderAsset>("Hidden/Editor/EdGui_Font", true));

            _constSbg = new ShaderBindGroup(_imageShader, "EdGuiConst");
            _defaultSbg = new ShaderBindGroup(_imageShader, null);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _commandBuffer.Dispose();

                    _vertexBuffer?.Dispose();
                    _indexBuffer?.Dispose();

                    _cbCanvasBuffer?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CleanupFrame(IRenderPath path, RenderPassData passData)
        {

        }

        public void ExecutePass(IRenderPath path, RenderPassData passData)
        {
            EditorGuiManager guiManager = EditorGuiManager.Instance;

            BuildGui(guiManager);

            if (!_commandBuffer.IsEmpty)
            {
                CommandBuffer commandBuffer = CommandBufferPool.Get();

                using (new CommandBufferEventScope(commandBuffer, "EditorGui"))
                {
                    UploadGui(commandBuffer);
                    DrawGui(commandBuffer);
                    BlitGui(commandBuffer);
                }

                CommandBufferPool.Return(commandBuffer);
                _commandBuffer.Clear();
            }
        }

        private Queue<Element> _elementDrawQueue = new Queue<Element>();
        private void BuildGui(EditorGuiManager guiManager)
        {
            using (new ProfilingScope("BuildGui"))
            {
                _commandBuffer.Clear();
                _elementDrawQueue.Clear();

                Span<DockingContainer> containers = guiManager.ActiveContainers;
                for (int i = 0; i < containers.Length; i++)
                {
                    DockingContainer container = containers[i];
                    if (true/*TODO: "container.IsVisualInvalid" or smth*/)
                    {
                        _commandBuffer.SetRenderFocus(container);
                        container.DrawBackgroundVisuals(_commandBuffer);

                        if (container.FocusedEditorWindow != null)
                        {
                            _elementDrawQueue.Clear();
                            _elementDrawQueue.Enqueue(container.FocusedEditorWindow.RootElement);

                            while (_elementDrawQueue.TryDequeue(out Element? element))
                            {
                                if (element.DrawVisualInternal(_commandBuffer))
                                {
                                    for (int j = 0; j < element.Children.Count; j++)
                                    {
                                        _elementDrawQueue.Enqueue(element.Children[j]);
                                    }
                                }
                            }
                        }
                    }
                }

                _commandBuffer.End();
            }
        }

        private void UploadGui(CommandBuffer commandBuffer)
        {
            using (new CommandBufferEventScope(commandBuffer, "Upload"))
            {
                Span<GuiVertex> vertices = _commandBuffer.Vertices;
                Span<ushort> indices = _commandBuffer.Indices;

                if (_vertexBuffer == null || _vertexBufferSize < vertices.Length)
                {
                    _vertexBufferSize = (int)BitOperations.RoundUpToPowerOf2((uint)vertices.Length);

                    _vertexBuffer?.Dispose();
                    _vertexBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                    {
                        ByteWidth = (uint)_vertexBufferSize * (uint)Unsafe.SizeOf<GuiVertex>(),
                        Stride = (uint)Unsafe.SizeOf<GuiVertex>(),
                        CpuAccessFlags = RHI.CPUAccessFlags.Write,
                        Memory = RHI.MemoryUsage.Dynamic,
                        Usage = RHI.BufferUsage.VertexBuffer,
                        Mode = RHI.BufferMode.None
                    }, nint.Zero);
                }

                if (_indexBuffer == null || _indexBufferSize < indices.Length)
                {
                    _indexBufferSize = (int)BitOperations.RoundUpToPowerOf2((uint)indices.Length);

                    _indexBuffer?.Dispose();
                    _indexBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                    {
                        ByteWidth = (uint)_indexBufferSize * (uint)Unsafe.SizeOf<ushort>(),
                        Stride = (uint)Unsafe.SizeOf<ushort>(),
                        CpuAccessFlags = RHI.CPUAccessFlags.Write,
                        Memory = RHI.MemoryUsage.Dynamic,
                        Usage = RHI.BufferUsage.IndexBuffer,
                        Mode = RHI.BufferMode.None
                    }, nint.Zero);
                }

                {
                    Span<GuiVertex> vbuffer = commandBuffer.Map<GuiVertex>(_vertexBuffer, RHI.MapIntent.Write, vertices.Length);
                    Span<ushort> ibuffer = commandBuffer.Map<ushort>(_indexBuffer, RHI.MapIntent.Write, indices.Length);

                    vertices.CopyTo(vbuffer);
                    indices.CopyTo(ibuffer);

                    commandBuffer.Unmap(_vertexBuffer);
                    commandBuffer.Unmap(_indexBuffer);
                }
            }
        }

        private void DrawGui(CommandBuffer commandBuffer)
        {
            using (new CommandBufferEventScope(commandBuffer, "Draw"))
            {
                Span<GuiRenderFocusRegion> regions = _commandBuffer.FocusRegions;
                Span<GuiDrawCommand> commands = _commandBuffer.DrawCommands;

                TextureAsset? currentTexture = null;

                commandBuffer.SetBindGroups(_constSbg, _defaultSbg);

                commandBuffer.SetVertexBuffer(0, _vertexBuffer!);
                commandBuffer.SetIndexBuffer(_indexBuffer!);

                _constSbg.SetResource("cbCanvas", _cbCanvasBuffer);

                for (int i = 0; i < regions.Length; i++)
                {
                    ref GuiRenderFocusRegion focusRegion = ref regions[i];

                    if (focusRegion.CommandsEnd - focusRegion.CommandsStart > 0)
                    {
                        if (focusRegion.Container.RenderTarget == null)
                            continue;

                        Vector2 size = focusRegion.Container.ElementSpace.Size;

                        unsafe
                        {
                            CbCanvasData* data = (CbCanvasData*)commandBuffer.Map(_cbCanvasBuffer, RHI.MapIntent.Write);
                            if (data == null)
                                continue;

                            *data = new CbCanvasData
                            {
                                Projection = Matrix4x4.CreateOrthographic(size.X, size.Y, -1.0f, 1.0f) * Matrix4x4.CreateTranslation(-1.0f, -1.0f, 0.0f) * Matrix4x4.CreateScale(1.0f, -1.0f, 1.0f),
                            };
                            commandBuffer.Unmap(_cbCanvasBuffer);
                        }

                        commandBuffer.SetRenderTarget(focusRegion.Container.RenderTarget, true);
                        commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 100000));

                        for (int j = focusRegion.CommandsStart; j < focusRegion.CommandsEnd; j++)
                        {
                            ref GuiDrawCommand drawCommand = ref commands[j];

                            commandBuffer.SetShader(_imageShader);

                            if (drawCommand.Texture != currentTexture)
                            {
                                _defaultSbg.SetResource("txAssignedTexture", drawCommand.Texture);
                                currentTexture = drawCommand.Texture;
                            }

                            commandBuffer.CommitShaderResources();
                            commandBuffer.DrawIndexedInstanced(new RHI.DrawIndexedInstancedArgs(drawCommand.IndexCount, drawCommand.IndexOffset, (int)drawCommand.BaseVertexOffset));
                        }
                    }
                }
            }
        }

        private void BlitGui(CommandBuffer commandBuffer)
        {
            using (new CommandBufferEventScope(commandBuffer, "Blit"))
            {
                Span<GuiRenderFocusRegion> regions = _commandBuffer.FocusRegions;

                for (int i = 0; i < regions.Length; i++)
                {
                    ref GuiRenderFocusRegion focusRegion = ref regions[i];

                    if (focusRegion.Container.Window != null && focusRegion.Container.OwningContainer == null)
                    {
                        RHI.SwapChain? swapChain = Engine.GlobalSingleton.RenderingManager.SwapChainCache.GetIfExists(focusRegion.Container.Window);
                        if (swapChain != null)
                        {
                            commandBuffer.SetRenderTarget(swapChain.BackBuffer);

                            RecursiveBlit(focusRegion.Container, focusRegion.Container.Position, focusRegion.Container.Size);
                        }
                    }

                    void RecursiveBlit(DockingContainer container, Vector2 parentPosition, Vector2 worldSize)
                    {
                        Boundaries boundaries = container.ElementSpace;

                        if (container.RenderTarget != null)
                        {
                            Vector2 size = boundaries.Size;

                            Vector2 scale = (size / worldSize);
                            Vector2 offset = ((container.Position * 2.0f + parentPosition) / worldSize) - (Vector2.One - scale);

                            Vector2 basePosition = parentPosition + container.Position;
                            //commandBuffer.SetScissorRect(new RHI.ScissorRect((int)basePosition.X, (int)basePosition.Y, (int)(basePosition.X + size.X), (int)(basePosition.Y + size.Y)));

                            Blitter.Blit(commandBuffer, container.RenderTarget.ColorTexture!, offset, scale);
                        }

                        parentPosition += container.Position;
                        foreach (DockingContainer docked in container.DockedContainers)
                        {
                            RecursiveBlit(docked, parentPosition, worldSize);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareFrame(IRenderPath path, RenderPassData passData)
        {

        }

        private record struct CbCanvasData(Matrix4x4 Projection);
    }
}
