using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Interop;
using Primary.RHI.Direct3D12.Descriptors;
using Primary.RHI.Direct3D12.Helpers;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Memory;
using Primary.RHI.Direct3D12.Utility;
using SharpGen.Runtime;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class GraphicsCommandBufferImpl : GraphicsCommandBuffer, ICommandBufferImpl
    {
        private readonly GraphicsDeviceImpl _device;

        private ID3D12CommandAllocator? _allocator;
        private ID3D12GraphicsCommandList7 _commandList;

        private ID3D12Fence _fence;
        private ulong _fenceValue;
        private ManualResetEventSlim _fenceEvent;

        private DynamicUploadHeap _uploadHeap;
        private ResourceBarrierManager _barrierManager;

        private GpuDescriptorHeap _descriptorAllocator;

        private bool _isOpen;
        private bool _isReady;

        private Dictionary<ICommandBufferMappable, SimpleMapInfo> _mappedResources;
        private Dictionary<TextureImpl, TextureMapInfo> _mappedTextures;

        private HashSet<ICommandBufferResource> _referencedResources;

        private PipelineState _currentPipelineState;

        private DirtyStaticCollection<ICommandBufferRT?> _activeRenderTargets;
        private DirtyValue<RenderTargetImpl?> _depthStencilTarget;
        private DirtyStaticCollection<Vortice.Mathematics.Viewport> _viewports;
        private DirtyStaticCollection<RawRect> _scissorRects;
        private DirtyValue<uint> _stencilRef;
        private DirtyCollection<VertexBuffer> _vertexBuffers;
        private DirtyValue<BufferImpl?> _indexBuffer;
        private DirtyValue<GraphicsPipelineImpl?> _pipelineState;
        private DirtyStaticCollection<ResourceLocation> _activeResources;
        private DirtyStaticCollection<uint> _activeConstants;

        private Dictionary<ActiveDescriptorKey, uint> _activeDescriptors;

        private bool _disposedValue;

        internal GraphicsCommandBufferImpl(GraphicsDeviceImpl device)
        {
            const ulong UploadHeapInitialSize = 1048576; //1mb

            _device = device;

            //ResultChecker.ThrowIfUnhandled(_device.D3D12Device.CreateCommandAllocator(CommandListType.Direct, out _allocator!));
            ResultChecker.ThrowIfUnhandled(_device.D3D12Device.CreateCommandList1(CommandListType.Direct, CommandListFlags.None, out _commandList!));

            //_allocator.Name = "DirectAlloc";
            _commandList.Name = "DirectCmd";

            ResultChecker.ThrowIfUnhandled(_device.D3D12Device.CreateFence(0, FenceFlags.None, out _fence!));
            _fenceValue = 0;
            _fenceEvent = new ManualResetEventSlim(false);

            _uploadHeap = new DynamicUploadHeap(device, true, UploadHeapInitialSize);
            _barrierManager = new ResourceBarrierManager();

            _descriptorAllocator = new GpuDescriptorHeap(device, 512, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

            _mappedResources = new Dictionary<ICommandBufferMappable, SimpleMapInfo>();
            _mappedTextures = new Dictionary<TextureImpl, TextureMapInfo>();

            _referencedResources = new HashSet<ICommandBufferResource>();

            _activeRenderTargets = new DirtyStaticCollection<ICommandBufferRT?>(8);
            _depthStencilTarget = new DirtyValue<RenderTargetImpl?>();
            _viewports = new DirtyStaticCollection<Vortice.Mathematics.Viewport>(8);
            _scissorRects = new DirtyStaticCollection<RawRect>(8, RectI.Empty);
            _stencilRef = new DirtyValue<uint>();
            _vertexBuffers = new DirtyCollection<VertexBuffer>(8);
            _indexBuffer = new DirtyValue<BufferImpl?>(null);
            _pipelineState = new DirtyValue<GraphicsPipelineImpl?>(null);
            _activeResources = new DirtyStaticCollection<ResourceLocation>(64);
            _activeConstants = new DirtyStaticCollection<uint>(128 / sizeof(uint));

            _activeDescriptors = new Dictionary<ActiveDescriptorKey, uint>();

            _isOpen = false;
            _isReady = true;

            _device.AddCommandBuffer(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_isOpen)
                    End();

                if (disposing)
                {
                    _uploadHeap.Dispose();
                }

                _device.EnqueueDataFree(() =>
                {
                    _fenceEvent.Dispose();
                    _fence.Dispose();
                    _descriptorAllocator.Dispose();
                    _commandList.Dispose();

                    _device.RemoveCommandBuffer(this);
                });

                _disposedValue = true;
            }
        }

        ~GraphicsCommandBufferImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SubmitToQueueInternal(ID3D12Fence? fenceToWaitFor, ulong valueToWaitFor)
        {
            if (_isOpen)
            {
                GraphicsDeviceImpl.Logger.Warning("Cannot submit open command buffer!");
                return;
            }

            if (_isReady)
            {
                return;
            }

            if (fenceToWaitFor != null)
                _device.DirectCommandQueue.Wait(fenceToWaitFor, valueToWaitFor);

            /*if (_fence.CompletedValue < _fenceValue)
            {
                GraphicsDeviceImpl.Logger.Information("D Waiting on fence: {a} -> {b}", _fence.CompletedValue, _fenceValue);

                ExceptionUtility.Assert(_fenceEvent.Wait(2000));
                _fenceEvent.Reset();
            }*/

            //ResultChecker.PrintIfUnhandled(_fence.SetEventOnCompletion(_fenceValue), _device);

            //GraphicsDeviceImpl.Logger.Information("D Submitting self..");

            _device.DirectCommandQueue.ExecuteCommandList(_commandList);
            ResultChecker.PrintIfUnhandled(_device.DirectCommandQueue.Signal(_fence, ++_fenceValue));

            _isReady = true;
        }

        private void ClearInternalState()
        {
            _referencedResources.Clear();
            _mappedResources.Clear();

            _activeRenderTargets.Clear();
            _viewports.Clear();
            _scissorRects.Clear(RectI.Empty);
            _vertexBuffers.Clear();
            _activeResources.Clear();
            _activeConstants.Clear();
            _activeDescriptors.Clear();

            _depthStencilTarget.Value = default;
            _stencilRef.Value = default;
            _indexBuffer.Value = default;
            _pipelineState.Value = default;

            _barrierManager.ClearPendingTransitions();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetFrameData()
        {
            _descriptorAllocator.ReleaseStaleAllocations();
            _uploadHeap.FinishFrame(_fenceValue + 1, _fence.CompletedValue);
        }

        #region Command buffer default
        public override bool Begin()
        {
            if (_isOpen)
            {
                GraphicsDeviceImpl.Logger.Warning("Command buffer is already open!");
                return false;
            }

            if (!_isReady)
            {
                GraphicsDeviceImpl.Logger.Error("Command buffer is not ready to begin!");
                return false;
            }

            _allocator = _device.GetNewCommandAllocator(CommandListType.Direct);

            try
            {
                _allocator.Reset();
                _commandList.Reset(_allocator);
            }
            catch (SharpGenException ex)
            {
                ResultChecker.PrintIfUnhandled(ex.ResultCode, _device);
                return false;
            }

            ClearInternalState();

            _commandList.SetDescriptorHeaps(_descriptorAllocator.Heap);

            _isOpen = true;
            return true;
        }

        public override void End()
        {
            if (!_isOpen)
            {
                GraphicsDeviceImpl.Logger.Warning("Cannot end closed command list!");
                return;
            }

            _barrierManager.ClearPendingTransitions();

            foreach (ICommandBufferResource resource in _referencedResources)
            {
                resource.EnsureResourceStates(_barrierManager, resource.GenericState);
            }

            _barrierManager.FlushPendingTransitions(_commandList);

            try
            {
                _commandList.Close();
            }
            catch (SharpGenException ex)
            {
                ResultChecker.PrintIfUnhandled(ex.ResultCode, _device);
            }

            ClearInternalState();

            _device.ReturnCommandAllocator(CommandListType.Direct, _allocator!);

            _isOpen = false;
            _isReady = false;
            return;
        }

        public override nint Map(Buffer buffer, MapIntent intent, ulong dataSize, ulong writeOffset)
        {
            PerformSanityCheck();

            ICommandBufferMappable mappable = (ICommandBufferMappable)buffer;
            if (_mappedResources.TryGetValue(mappable, out SimpleMapInfo mapInfo))
                return mapInfo.Allocation.CpuAddress;

            mapInfo = new SimpleMapInfo(_uploadHeap.Allocate(dataSize != 0 ? dataSize : mappable.TotalSizeInBytes), writeOffset);

            if (mapInfo.Allocation.Buffer != null)
            {
                _mappedResources.TryAdd(mappable, mapInfo);
                return mapInfo.Allocation.CpuAddress;
            }
            else
                return nint.Zero;
        }

        public override nint Map(Texture texture, MapIntent intent, TextureLocation location, uint subresource, uint rowPitch)
        {
            PerformSanityCheck();

            TextureImpl impl = (TextureImpl)texture;
            if (_mappedTextures.TryGetValue(impl, out TextureMapInfo mapInfo))
            {
                if (!mapInfo.Location.Equals(location))
                {
                    if (_device.CmdBufferValidation)
                    {
                        GraphicsDeviceImpl.Logger.Error("Map: Texture is already mapped and new location does not match old.");
                    }

                    return nint.Zero;
                }

                if (mapInfo.Subresource != subresource)
                {
                    if (_device.CmdBufferValidation)
                    {
                        //TODO: change in accordance to the logged message
                        GraphicsDeviceImpl.Logger.Error("Map: Texture is already mapped and new subresource does not match old (this is a stupid requirment i need to change).");
                    }

                    return nint.Zero;
                }

                if (mapInfo.RowPitch != rowPitch)
                {
                    if (_device.CmdBufferValidation)
                    {
                        GraphicsDeviceImpl.Logger.Error("Map: Texture is already mapped and new row pitch does not match old.");
                    }

                    return nint.Zero;
                }

                return mapInfo.Allocation.CpuAddress;
            }

            FormatInfo info = FormatStatistics.Query(impl.Description.Format);
            ulong dataSize = (ulong)info.BytesPerPixel * (location.Width * location.Height * location.Depth);

            if (rowPitch == 0)
                rowPitch = (uint)info.BytesPerPixel * location.Width;

            mapInfo = new TextureMapInfo(_uploadHeap.Allocate(dataSize), location, subresource, rowPitch);

            if (mapInfo.Allocation.Buffer != null)
            {
                _mappedTextures.TryAdd(impl, mapInfo);
                return mapInfo.Allocation.CpuAddress;
            }
            else
                return nint.Zero;
        }

        public override void Unmap(Resource resource)
        {
            PerformSanityCheck();

            ICommandBufferMappable mappable = (ICommandBufferMappable)resource;
            if (_mappedResources.TryGetValue(mappable, out SimpleMapInfo allocation))
            {
                if (mappable.CurrentState != ResourceStates.Common)
                    mappable.TransitionImmediate(_commandList, ResourceStates.CopyDest);
                else
                    mappable.SetImplicitResourcePromotion(ResourceStates.CopyDest);

                mappable.MappableCopyDataTo(_commandList, ref allocation);
                _mappedResources.Remove(mappable);
                _referencedResources.Add(mappable);
            }
            else if (mappable is TextureImpl texture && _mappedTextures.TryGetValue(texture, out TextureMapInfo mapInfo))
            {
                if (mappable.CurrentState != ResourceStates.Common)
                    texture.TransitionImmediate(_commandList, ResourceStates.CopyDest);
                else
                    mappable.SetImplicitResourcePromotion(ResourceStates.CopyDest);

                texture.MappableCopyTextureDataTo(_commandList, ref mapInfo);
                _mappedTextures.Remove(texture);
            }
        }

        public override bool CopyBufferRegion(Buffer src, uint srcOffset, Buffer dst, uint dstOffset, uint size)
        {
            PerformSanityCheck();

            if (size > dst.Description.ByteWidth || size > src.Description.ByteWidth)
                return false;
            if (srcOffset + size > src.Description.ByteWidth || dstOffset + size > dst.Description.ByteWidth)
                return false;

            BufferImpl srcImpl = (BufferImpl)src;
            BufferImpl dstImpl = (BufferImpl)dst;

            if (srcImpl.CurrentState != ResourceStates.Common && srcImpl.CurrentState != ResourceStates.CopySource)
                srcImpl.EnsureResourceStates(_barrierManager, ResourceStates.CopySource);
            if (dstImpl.CurrentState != ResourceStates.Common && dstImpl.CurrentState != ResourceStates.CopySource)
                dstImpl.EnsureResourceStates(_barrierManager, ResourceStates.CopySource);

            //srcImpl.EnsureResourceStates(_barrierManager, ResourceStates.CopySource);
            //dstImpl.EnsureResourceStates(_barrierManager, ResourceStates.CopyDest);
            //
            //_barrierManager.FlushPendingTransitions(_commandList);

            /// look at the comment in <see cref="CopyCommandBufferImpl.CopyBufferRegion(Buffer, uint, Buffer, uint, uint)"/>
            /// for more info
            /// a quick note: resources only decay back to "Common" when in a "Copy" queue.

            //this just has to also inform the resources of the transition because this can potentially do more then just copy

            srcImpl.CopyToBuffer(_commandList, dstImpl, srcOffset, dstOffset, size);

            srcImpl.SetImplicitResourcePromotion(ResourceStates.CopySource);
            dstImpl.SetImplicitResourcePromotion(ResourceStates.CopyDest);

            _referencedResources.Add(srcImpl);
            _referencedResources.Add(dstImpl);
            return true;
        }

        //TODO: add validation and rest of types to function
        public override bool CopyTextureRegion(Resource src, TextureLocation srcLoc, uint srcSubRes, Resource dst, TextureLocation dstLoc, uint dstSubRes)
        {
            if (src is ICommandBufferRTView srcRT && dst is ICommandBufferRTView dstRT)
            {
                if (srcRT.CurrentState != ResourceStates.Common && srcRT.CurrentState != ResourceStates.CopySource)
                    srcRT.EnsureResourceStates(_barrierManager, ResourceStates.CopySource);
                if (dstRT.CurrentState != ResourceStates.Common && dstRT.CurrentState != ResourceStates.CopyDest)
                    dstRT.EnsureResourceStates(_barrierManager, ResourceStates.CopyDest);

                _barrierManager.FlushPendingTransitions(_commandList);

                srcRT.CopyTexture(_commandList, dstRT);

                _referencedResources.Add(srcRT);
                _referencedResources.Add(dstRT);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void BeginEvent(Color32 color, ReadOnlySpan<char> name)
        {
            if (_device.IsPixEnabled)
            {
                PIX.PIXBeginEventOnCommandList(_commandList.NativePointer, color.ARGB, name.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void EndEvent()
        {
            if (_device.IsPixEnabled)
            {
                PIX.PIXEndEventOnCommandList(_commandList.NativePointer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetMarker(Color32 color, ReadOnlySpan<char> name)
        {
            if (_device.IsPixEnabled)
            {
                PIX.PIXSetMarkerOnCommandList(_commandList.NativePointer, color.ARGB, name.ToString());
            }
        }
        #endregion
        #region Graphics command buffer
        public override void ClearRenderTarget(RenderTarget rt, Vector4 color)
        {
            PerformSanityCheck();

            ICommandBufferRT impl = (ICommandBufferRT)rt;
            if (impl.ColorTexture != null)
            {
                impl.ColorTexture.TransitionImmediate(_commandList, ResourceStates.RenderTarget);
                //_barrierManager.FlushPendingTransitions(_commandList);
                _commandList.ClearRenderTargetView(impl.ColorTexture.ViewCpuDescriptor, (Color4)color);

                impl.ColorTexture.HasInitialClearCompleted = true;
                _referencedResources.Add(impl.ColorTexture);
            }
        }

        public override void ClearDepthStencil(RenderTarget rt, ClearFlags clear, float depth = 1, byte stencil = 255)
        {
            PerformSanityCheck();

            ICommandBufferRT impl = (ICommandBufferRT)rt;
            if (impl.DepthTexture != null)
            {
                impl.DepthTexture.TransitionImmediate(_commandList, ResourceStates.DepthWrite);
                //_barrierManager.FlushPendingTransitions(_commandList);
                _commandList.ClearDepthStencilView(impl.DepthTexture.ViewCpuDescriptor, (Vortice.Direct3D12.ClearFlags)clear, depth, stencil);

                impl.DepthTexture.HasInitialClearCompleted = true;
                _referencedResources.Add(impl.DepthTexture);
            }
        }

        public override void SetRenderTargets(Span<RenderTarget> renderTargets, bool setFirstToDepth = false)
        {
            PerformSanityCheck();

            if (renderTargets.IsEmpty)
            {
                _activeRenderTargets.ProxyLength = 0;
                return;
            }

            int limit = Math.Min(renderTargets.Length, _activeRenderTargets.Length);

            if (_device.CmdBufferValidation)
            {
                if (renderTargets.Length > _activeRenderTargets.Length)
                {
                    GraphicsDeviceImpl.Logger.Warning("SetRenderTargets: {arg1} goes out of bounds (max: {max}, length: {lth}).", nameof(renderTargets), _activeRenderTargets.Length, renderTargets.Length);
                }

                for (int i = 0; i < limit; i++)
                {
                    ICommandBufferRT impl = (ICommandBufferRT)renderTargets[i];
                    if (impl.RTFormat == RenderTargetFormat.Undefined)
                    {
                        GraphicsDeviceImpl.Logger.Warning("SetRenderTargets: {arg1}[{idx}] does not have a color texture because it was created with \"ColorFormat = RenderTargetFormat.{format}\".", nameof(renderTargets), i, RenderTargetFormat.Undefined);
                    }
                }
            }

            if (renderTargets.Length == 1)
            {
                ICommandBufferRT impl = (ICommandBufferRT)renderTargets[0];
                if (setFirstToDepth)
                {
                    _depthStencilTarget.Value = impl.DepthTexture == null ? null : (RenderTargetImpl)impl;
                }

                _activeRenderTargets[0] = impl.ColorTexture == null ? null : impl;
                _viewports[0] = impl.ColorTexture == null ? default : impl.Viewport;
            }
            else
            {
                if (setFirstToDepth)
                {
                    ICommandBufferRT impl = (ICommandBufferRT)renderTargets[0];
                    _depthStencilTarget.Value = impl.DepthTexture == null ? null : (RenderTargetImpl)impl;
                }

                for (int i = 0; i < limit; i++)
                {
                    ICommandBufferRT impl = (ICommandBufferRT)renderTargets[i];
                    _activeRenderTargets[i] = impl.ColorTexture == null ? null : impl;
                    _viewports[i] = impl.ColorTexture == null ? default : impl.Viewport;
                }
            }

            _activeRenderTargets.ProxyLength = limit;
            _viewports.ProxyLength = limit;
        }

        public override void SetDepthStencil(RenderTarget? renderTarget)
        {
            PerformSanityCheck();

            RenderTargetImpl? impl = (RenderTargetImpl?)renderTarget;

            if (_device.CmdBufferValidation)
            {
                if (impl != null)
                {
                    if (impl.Description.DepthFormat == DepthStencilFormat.Undefined)
                    {
                        GraphicsDeviceImpl.Logger.Warning("SetDepthStencil: {arg1} does not have a depth stencil texture because it was created with \"DepthFormat = DepthStencilFormat.{format}\".", nameof(renderTarget), DepthStencilFormat.Undefined);
                    }
                }
            }

            _depthStencilTarget.Value = impl?.IsDSTNull ?? true ? null : impl;

            if (_activeRenderTargets[0] != null)
            {
                _viewports[0] = impl?.DepthTexture == null ? default : ((ICommandBufferRT)impl).Viewport;
                _viewports.ProxyLength = 1;
            }
        }

        public override void SetViewports(Span<Viewport> viewports)
        {
            PerformSanityCheck();

            if (viewports.IsEmpty)
            {
                _viewports.ProxyLength = 0;
                return;
            }

            int limit = Math.Min(viewports.Length, _viewports.Length);

            if (_device.CmdBufferValidation)
            {
                if (viewports.Length > _viewports.Length)
                {
                    GraphicsDeviceImpl.Logger.Warning("SetViewports: {arg1} goes out of bounds (max: {max}, length: {lth}).", nameof(viewports), _scissorRects.Length, viewports.Length);
                }

                //TODO: validate "viewports" for negative sizes?
            }

            if (viewports.Length == 1)
            {
                ref Viewport rect = ref viewports[0];
                _viewports[0] = new Vortice.Mathematics.Viewport(rect.TopLeftX, rect.TopLeftY, rect.Width, rect.Height, rect.MinDepth, rect.MaxDepth);
            }
            else
            {
                for (int i = 0; i < limit; i++)
                {
                    ref Viewport rect = ref viewports[i];
                    _viewports[i] = new Vortice.Mathematics.Viewport(rect.TopLeftX, rect.TopLeftY, rect.Width, rect.Height, rect.MinDepth, rect.MaxDepth);
                }
            }

            _viewports.ProxyLength = limit;
        }

        public override void SetScissorRects(Span<ScissorRect> rects)
        {
            PerformSanityCheck();

            if (rects.IsEmpty)
            {
                _scissorRects.ProxyLength = 0;
                return;
            }

            int limit = Math.Min(rects.Length, _scissorRects.Length);

            if (_device.CmdBufferValidation)
            {
                if (rects.Length > _scissorRects.Length)
                {
                    GraphicsDeviceImpl.Logger.Warning("SetScissorRects: {arg1} goes out of bounds (max: {max}, length: {lth}).", nameof(rects), _scissorRects.Length, rects.Length);
                }

                //TODO: validate "rects" for negative sizes?
            }

            if (rects.Length == 1)
            {
                ref ScissorRect rect = ref rects[0];
                _scissorRects[0] = new RawRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
            else
            {
                for (int i = 0; i < limit; i++)
                {
                    ref ScissorRect rect = ref rects[i];
                    _scissorRects[i] = new RawRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
                }
            }

            _scissorRects.ProxyLength = limit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetStencilReference(uint stencilRef)
        {
            PerformSanityCheck();

            _stencilRef.Value = stencilRef;
        }

        public override void SetVertexBuffers(int startSlot, Span<Buffer> buffers, Span<uint> strides)
        {
            PerformSanityCheck();

            if (buffers.IsEmpty)
                return;

            int limit = Math.Min(startSlot + buffers.Length, _vertexBuffers.Length);

            if (_device.CmdBufferValidation)
            {
                if (startSlot + buffers.Length > _vertexBuffers.Length)
                {
                    GraphicsDeviceImpl.Logger.Warning("SetVertexBuffers: {arg1} goes out of bounds (max: {max}, startSlot: {slot}, length: {lth}).", nameof(buffers), _vertexBuffers.Length, startSlot, buffers.Length);
                }

                for (int i = 0; i < limit; i++)
                {
                    if (!FlagUtility.HasFlag(buffers[i].Description.Usage, BufferUsage.VertexBuffer))
                    {
                        GraphicsDeviceImpl.Logger.Warning("SetVertexBuffers: {arg1}[{idx}] does not have BufferUsage.{usage} usage flag set.", nameof(buffers), i, BufferUsage.VertexBuffer);
                    }

                    if (strides.Length < i && strides[i] > buffers[i].Description.ByteWidth)
                    {
                        GraphicsDeviceImpl.Logger.Warning("SetVertexBuffers: {arg1}[{idx}] is larger then buffer total width (stride: {st}, width: {wd}).", nameof(strides), i, strides[i], buffers[i].Description.ByteWidth);
                    }
                }
            }

            if (buffers.Length == 1)
            {
                if (startSlot < _vertexBuffers.Length)
                {
                    BufferImpl impl = (BufferImpl)buffers[0];
                    _vertexBuffers[startSlot] = new VertexBuffer(impl, strides.IsEmpty ? impl.Description.Stride : (strides[0] == 0 ? impl.Description.Stride : strides[0]));
                }
            }
            else
            {
                for (int i = startSlot, j = 0; i < limit; i++, j++)
                {
                    BufferImpl impl = (BufferImpl)buffers[j];
                    _vertexBuffers[i] = new VertexBuffer(impl, strides.Length < j ? impl.Description.Stride : (strides[j] == 0 ? impl.Description.Stride : strides[j]));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetIndexBuffer(Buffer? buffer)
        {
            PerformSanityCheck();

            if (_device.CmdBufferValidation)
            {
                if (buffer != null)
                {
                    if (!FlagUtility.HasFlag(buffer.Description.Usage, BufferUsage.IndexBuffer))
                    {
                        GraphicsDeviceImpl.Logger.Warning("SetIndexBuffer: {arg1} does not have BufferUsage.{usage} usage flag set.", nameof(buffer), BufferUsage.IndexBuffer);
                    }
                }
            }

            _indexBuffer.Value = (BufferImpl?)buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetPipeline(GraphicsPipeline pipeline)
        {
            _pipelineState.Value = (GraphicsPipelineImpl)pipeline;
        }

        public override void SetResources(Span<ResourceLocation> resources)
        {
            if (resources.IsEmpty)
            {
                _activeResources.ProxyLength = 0;
                return;
            }

            int limit = Math.Min(resources.Length, _activeResources.Length);

            if (_device.CmdBufferValidation)
            {
                if (_activeResources.Length < limit)
                {
                    GraphicsDeviceImpl.Logger.Warning("SetResources: {arg1} goes out of bounds (max: {max}, length: {lth}).", nameof(resources), _activeResources.Length, resources.Length);
                }

                for (int i = 0; i < limit; i++)
                {
                    ref ResourceLocation location = ref resources[i];
                    if (location.Resource == null)
                    {
                        GraphicsDeviceImpl.Logger.Warning("SetResources: {arg1}[{idx}] is null.", nameof(resources), i);
                        continue;
                    }

                    ICommandBufferResource resource = (ICommandBufferResource)location.Resource;

                    if (!resource.IsShaderVisible)
                    {
                        GraphicsDeviceImpl.Logger.Warning("SetResources: {arg1}[{idx}] is not a shader visible resource (type: {type}, name: {name}).", nameof(resources), i, location.Resource.GetType(), resource.ResourceName);
                    }

                    if (location.Descriptor != null)
                    {
                        if (location.Descriptor.Owner != location.Resource)
                        {
                            GraphicsDeviceImpl.Logger.Warning("SetResources: {arg1}[{idx}] descriptor owner does not match set resource.", nameof(resources), i);
                            continue;
                        }
                    }
                }
            }

            _activeResources.CopyInto(resources, limit);
            _activeResources.ProxyLength = limit;
        }

        public override void SetConstants(Span<uint> constants)
        {
            if (constants.IsEmpty)
            {
                _activeConstants.ProxyLength = 0;
                return;
            }

            int limit = Math.Min(constants.Length, _activeConstants.Length);

            if (_device.CmdBufferValidation)
            {
                if (limit < constants.Length)
                {
                    GraphicsDeviceImpl.Logger.Warning("SetConstants: {arg1} goes out of bounds (max: {max}, length: {lth}).", nameof(constants), _activeConstants.Length, constants.Length);
                }
            }

            _activeConstants.CopyInto(constants, limit);
            _activeConstants.MarkDirty();

            _activeConstants.ProxyLength = limit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void DrawIndexedInstanced(in DrawIndexedInstancedArgs args)
        {
            if (_device.CmdBufferValidation)
                ValidateDrawState(true);
            if (SetupDrawState(true))
                _commandList.DrawIndexedInstanced(args.IndexCountPerInstance, args.InstanceCount, args.StartIndexLocation, args.BaseVertexLocation, args.StartInstanceLocation);
        }

        public override void DrawInstanced(in DrawInstancedArgs args)
        {
            if (_device.CmdBufferValidation)
                ValidateDrawState(false);
            if (SetupDrawState(false))
                _commandList.DrawInstanced(args.VertexCountPerInstance, args.InstanceCount, args.StartVertexLocation, args.StartInstanceLocation);
        }
        #endregion

        #region Draw preperation
        private HashSet<int> _tempValIntHashSet = new HashSet<int>();
        private int[] _tempValResPresentArray = [0, 0, 0];

        private void ValidateDrawState(bool isIndexedDraw)
        {
            if (isIndexedDraw)
            {
                if (_indexBuffer.Value == null)
                {
                    GraphicsDeviceImpl.Logger.Error("ValidateDrawState: No index buffer specified for draw.");
                    return;
                }
            }

            //redundant and broken with check under
            /*if (_vertexBuffers.DirtyLength == 0)
            {
                GraphicsDeviceImpl.Logger.Error("ValidateDrawState: No vertex buffer(s) specified for draw.");
                return;
            }*/

            if (_pipelineState.Value == null)
            {
                GraphicsDeviceImpl.Logger.Error("ValidateDrawState: No graphics pipeline set for draw call.");
                return;
            }

            ref readonly GraphicsPipelineDescription desc = ref _pipelineState.Value.Description;

            _tempValIntHashSet.Clear();
            for (int i = 0; i < desc.InputElements.Length; i++)
            {
                ref InputElementDescription elem = ref desc.InputElements[i];
                if (_vertexBuffers[elem.InputSlot].Buffer == null && !_tempValIntHashSet.Contains(elem.InputSlot))
                {
                    GraphicsDeviceImpl.Logger.Warning("ValidateDrawState: Input element(s) specifies slot: {slot} but no vertex buffer is present.", elem.InputSlot);
                    _tempValIntHashSet.Add(elem.InputSlot);
                }
            }

            if (_activeConstants.ProxyLength != desc.ExpectedConstantsSize)
            {
                GraphicsDeviceImpl.Logger.Error("ValidateDrawState: Set constants length does not match expected length (expected: {exp}, set: {set}).", desc.ExpectedConstantsSize, _activeConstants.ProxyLength);
            }

            int resourceLimit = Math.Min(desc.BoundResources.Length, _activeResources.ProxyLength);
            Span<ResourceLocation> resources = _activeResources.AsSpanProxy();

            if (desc.BoundResources.Length > _activeResources.ProxyLength)
            {
                GraphicsDeviceImpl.Logger.Error("ValidateDrawState: Not all resources used by the shader have been specified (required: {req}, present: {pre}).", desc.BoundResources.Length, _activeResources.ProxyLength);
            }

            Array.Fill(_tempValResPresentArray, 0);
            for (int i = 0; i < desc.BoundResources.Length; i++)
            {
                ref BoundResourceDescription bound = ref desc.BoundResources[i];
                if (bound.Index < resources.Length)
                {
                    ref ResourceLocation resource = ref resources[bound.Index];
                    if (resource.Resource == null)
                    {
                        GraphicsDeviceImpl.Logger.Error("ValidateDrawState: Resource at index: {idx} is null.", i);
                        continue;
                    }

                    ICommandBufferResource impl = (ICommandBufferResource)resource.Resource;

                    if (resource.Descriptor != null)
                    {
                        ICommandDescriptor descriptor = Unsafe.As<ICommandDescriptor>(resource.Descriptor);
                        if (descriptor.BindType != bound.Type)
                        {
                            GraphicsDeviceImpl.Logger.Error("ValidateDrawState: Incorrect descriptor type specified at index: {idx} (expected: {exp}, got: {got}).", bound.Index, bound.Type, descriptor.BindType);
                        }
                        else if (resource.Descriptor.Owner != resource.Resource)
                        {
                            GraphicsDeviceImpl.Logger.Error("ValidateDrawState: Descriptor specified at index: {idx} has a diffent owner than specified resource (owner: {own}, got: {got}).", bound.Index, resource.Descriptor.Owner, resource.Resource);
                        }
                    }
                    else
                    {
                        if (impl.Type != bound.Type)
                        {
                            GraphicsDeviceImpl.Logger.Error("ValidateDrawState: Incorrect resource specified at index: {idx} (expected: {exp}, got: {got}).", bound.Index, bound.Type, bound.Index);
                        }
                    }
                }
                else
                {
                    GraphicsDeviceImpl.Logger.Error("ValidateDrawState: Shader resource: {res} is not present at index: {idx}.", bound.Type, bound.Index);
                }
            }
        }

        private CpuDescriptorHandle[] _tempRTVDescriptors = new CpuDescriptorHandle[8];
        private Viewport[] _tempViewports = new Viewport[8];
        private VertexBufferView[] _tempVBVs = new VertexBufferView[8];
        private uint[] _resourceCpuDescriptors = new uint[64];
        private Stack<UnresolvedResource> _unresolvedResources = new Stack<UnresolvedResource>();

        private bool SetupDrawState(bool isIndexedDraw)
        {
            if (_pipelineState.Value == null)
                return false;
            if (_activeRenderTargets.ProxyLength == 0 && _depthStencilTarget.Value == null)
                return false;
            if (isIndexedDraw ? _indexBuffer.Value == null : false)
                return false;

            _barrierManager.ClearPendingTransitions();

            bool hasChangedState = false;
            bool resetRenderTargets = false;
            CpuDescriptorHandle resetDsv = _device.CpuDSVDescriptors.NullDescriptor;

            if (_activeRenderTargets.IsDirty)
            {
                _currentPipelineState.RTVs.Clear();

                Span<ICommandBufferRT?> renderTargets = _activeRenderTargets.AsSpanProxy();
                if (!renderTargets.IsEmpty)
                {
                    for (int i = 0; i < _activeRenderTargets.ProxyLength; i++)
                    {
                        ICommandBufferRT? renderTarget = renderTargets[i];
                        if (renderTarget != null && renderTarget.ColorTexture != null)
                        {
                            ICommandBufferRTView texture = renderTarget.ColorTexture!;

                            if (!texture.HasInitialClearCompleted)
                            {
                                texture.TransitionImmediate(_commandList, ResourceStates.RenderTarget);
                                _commandList.ClearRenderTargetView(texture.ViewCpuDescriptor, new Color4(0.0f, 0.0f, 0.0f));
                                texture.HasInitialClearCompleted = true;
                            }
                            else
                                texture.EnsureResourceStates(_barrierManager, ResourceStates.RenderTarget);


                            _tempRTVDescriptors[i] = texture.ViewCpuDescriptor;
                            //_tempViewports[i] = renderTarget.Viewport;
                            _currentPipelineState.RTVs[i] = renderTarget.RTFormat;

                            _referencedResources.Add(texture);
                            hasChangedState = true;
                        }
                        else
                        {
                            _tempRTVDescriptors[i] = _device.CpuRTVDescriptors.NullDescriptor;
                        }
                    }
                }

                _activeRenderTargets.Reset();

                CpuDescriptorHandle dsv = _device.CpuDSVDescriptors.NullDescriptor;
                if (_depthStencilTarget.Value != null)
                {
                    ICommandBufferRTView texture = (ICommandBufferRTView)_depthStencilTarget.Value.DepthTexture!;
                    dsv = texture.ViewCpuDescriptor;

                    if (!texture.HasInitialClearCompleted)
                    {
                        texture.TransitionImmediate(_commandList, ResourceStates.DepthWrite);
                        _commandList.ClearDepthStencilView(dsv, Vortice.Direct3D12.ClearFlags.Depth | Vortice.Direct3D12.ClearFlags.Stencil, 1.0f, 0xff);
                        texture.HasInitialClearCompleted = true;
                    }

                    texture.EnsureResourceStates(_barrierManager, ResourceStates.DepthWrite);

                    if (_depthStencilTarget.IsDirty)
                        _currentPipelineState.DSV = _depthStencilTarget.Value.Description.DepthFormat;
                    _depthStencilTarget.Reset();

                    _referencedResources.Add(texture);
                    hasChangedState = true;
                }

                resetRenderTargets = true;
                resetDsv = dsv;
            }

            if (_pipelineState.IsDirty || hasChangedState)
            {
                bool wasDirty = _pipelineState.IsDirty;
                _pipelineState.Reset();

                ID3D12PipelineState? pipelineState = _pipelineState.Value.GetPipelineState(ref _currentPipelineState);
                if (pipelineState == null)
                    return false;

                if (wasDirty)
                {
                    _commandList.SetGraphicsRootSignature(_pipelineState.Value.ID3D12RootSignature);

                    ref readonly GraphicsPipelineDescription miniDesc = ref _pipelineState.Value!.Description;
                    switch (miniDesc.PrimitiveTopology)
                    {
                        case PrimitiveTopologyType.Triangle: _commandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList); break;
                        case PrimitiveTopologyType.Line: _commandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.LineList); break;
                        case PrimitiveTopologyType.Point: _commandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.PointList); break;
                        case PrimitiveTopologyType.Patch: _commandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.PatchListWith10ControlPoints); break;
                    }
                }
                _commandList.SetPipelineState(pipelineState);
            }

            ref readonly GraphicsPipelineDescription pipelineDesc = ref _pipelineState.Value!.Description;

            if (_viewports.IsDirty)
            {
                _commandList.RSSetViewports(_viewports.AsSpanProxy());
                _viewports.Reset();
            }

            if (_scissorRects.IsDirty)
            {
                _commandList.RSSetScissorRects(_scissorRects.AsSpanProxy());
                _scissorRects.Reset();
            }

            if (_stencilRef.IsDirty)
            {
                _commandList.OMSetStencilRef(_stencilRef.Value);
                _stencilRef.Reset();
            }

            if (_vertexBuffers.IsDirty)
            {
                Span<VertexBuffer> buffers = _vertexBuffers.AsSpanWithinDirty();
                if (!buffers.IsEmpty)
                {
                    bool isSettingWithDirty = buffers[0].Buffer == null;
                    int lastIndex = 0;
                    int lastCount = 0;

                    for (int i = 0; i < buffers.Length; i++)
                    {
                        VertexBuffer buffer = buffers[i];
                        bool isNull = buffer.Buffer == null;

                        if (isSettingWithDirty != isNull)
                        {
                            if (lastCount > 0 && isSettingWithDirty)
                            {
                                _commandList.IASetVertexBuffers((uint)(lastIndex + _vertexBuffers.DirtyIndexStart), (uint)lastCount, (VertexBufferView*)null);
                            }
                            else
                            {
                                _commandList.IASetVertexBuffers((uint)(lastIndex + _vertexBuffers.DirtyIndexStart), _tempVBVs.AsSpan(0, lastCount));
                            }

                            isSettingWithDirty = isNull;
                            lastCount = 0;
                            lastIndex = i;
                        }

                        if (!isNull)
                        {
                            buffer.Buffer!.EnsureResourceStates(_barrierManager, ResourceStates.VertexAndConstantBuffer);

                            _tempVBVs[lastCount] = new VertexBufferView
                            {
                                BufferLocation = buffer.Buffer!.GPUVirtualAddress,
                                SizeInBytes = buffer.Buffer!.Description.ByteWidth,
                                StrideInBytes = buffer.Stride,
                            };

                            _referencedResources.Add(buffer.Buffer);
                        }

                        lastCount++;
                    }

                    if (lastCount > 0 && isSettingWithDirty)
                    {
                        _commandList.IASetVertexBuffers((uint)(lastIndex + _vertexBuffers.DirtyIndexStart), (uint)lastCount, (VertexBufferView*)null);
                    }
                    else
                    {
                        _commandList.IASetVertexBuffers((uint)(lastIndex + _vertexBuffers.DirtyIndexStart), _tempVBVs.AsSpan(0, lastCount));
                    }
                }

                _vertexBuffers.Reset();
            }

            if (isIndexedDraw && _indexBuffer.IsDirty)
            {
                BufferImpl? indexBuffer = _indexBuffer.Value;
                if (indexBuffer == null)
                {
                    _commandList.IASetIndexBuffer((IndexBufferView*)null);
                }
                else
                {
                    indexBuffer!.EnsureResourceStates(_barrierManager, ResourceStates.IndexBuffer);

                    _commandList.IASetIndexBuffer(new IndexBufferView
                    {
                        BufferLocation = indexBuffer!.GPUVirtualAddress,
                        SizeInBytes = indexBuffer!.Description.ByteWidth,
                        Format = indexBuffer!.IndexStrideFormat
                    });

                    _referencedResources.Add(indexBuffer);
                }

                _indexBuffer.Reset();
            }

            bool needsToSetNewConstants = _activeConstants.IsDirty;
            if (_activeConstants.IsDirty)
            {
                if (_activeConstants.ProxyLength > 0)
                {
                    Span<uint> constants = _activeConstants.AsSpanProxy();
                    if (constants.Length > pipelineDesc.ExpectedConstantsSize)
                        constants = constants.Slice(0, (int)pipelineDesc.ExpectedConstantsSize);

                    if (_pipelineState.Value.IsUsingConstantBuffer)
                        _commandList.SetGraphicsRoot32BitConstants(1, (uint)constants.Length, Unsafe.AsPointer(ref constants), 0);
                    else
                        constants.CopyTo(_resourceCpuDescriptors);
                }

                _activeConstants.Reset();
            }

            if (_activeResources.IsDirty)
            {
                needsToSetNewConstants = true;

                Span<ResourceLocation> locations = _activeResources.AsSpanProxy();
                if (!locations.IsEmpty)
                {
                    Span<uint> descriptors = _resourceCpuDescriptors.AsSpan((int)pipelineDesc.ExpectedConstantsSize, locations.Length);

                    _unresolvedResources.Clear();

                    for (int i = 0; i < locations.Length; i++)
                    {
                        ref ResourceLocation location = ref locations[i];
                        if (location.Resource == null)
                            return false;

                        ICommandBufferResource impl = (ICommandBufferResource)location.Resource;
                        if (!impl.IsShaderVisible)
                            return false;

                        ICommandDescriptor? descriptor = Unsafe.As<ICommandDescriptor>(location.Descriptor);

                        ActiveDescriptorKey key = new ActiveDescriptorKey(location.Resource.Handle, descriptor);

                        impl.EnsureResourceStates(_barrierManager, ResourceStates.Common, true);

                        if (!(descriptor?.IsDynamic ?? true) && _activeDescriptors.TryGetValue(key, out uint handle))
                        {
                            descriptors[i] = handle;
                        }
                        else
                        {
                            _unresolvedResources.Push(new UnresolvedResource(impl, location.ConstantsOffset, key, location.DescriptorOffset));
                        }

                        _referencedResources.Add(impl);
                    }

                    if (_unresolvedResources.Count > 0)
                    {
                        DescriptorHeapAllocation allocation = _descriptorAllocator.Allocate(_unresolvedResources.Count);

                        while (_unresolvedResources.TryPop(out UnresolvedResource resource))
                        {
                            uint offset = (uint)(resource.DescriptorIndex + allocation.HeapOffset);

                            CpuDescriptorHandle handle = allocation.GetCpuHandle(resource.DescriptorIndex);

                            CpuDescriptorHandle src = CpuDescriptorHandle.Default;
                            if (resource.DictionaryKey.Descriptor == null)
                                src = resource.Resource.CpuDescriptor;
                            else if (resource.DictionaryKey.Descriptor.IsDynamic)
                            {
                                resource.DictionaryKey.Descriptor.AllocateDynamic(resource.DescriptorOffset, handle);
                                descriptors[resource.DescriptorIndex] = offset;
                                continue;
                            }
                            else
                                src = resource.DictionaryKey.Descriptor.CpuDescriptor;

                            _device.D3D12Device.CopyDescriptorsSimple(1, handle, src, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

                            if (_activeDescriptors.TryAdd(resource.DictionaryKey, offset))
                                descriptors[resource.DescriptorIndex] = offset;
                            else
                                descriptors[resource.DescriptorIndex] = _activeDescriptors[resource.DictionaryKey];
                        }

                    }

                    if (_pipelineState.Value.IsUsingConstantBuffer)
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            if (needsToSetNewConstants)
            {
                uint size = pipelineDesc.ExpectedConstantsSize;
                if (!_pipelineState.Value.IsUsingConstantBuffer)
                    size += (uint)(pipelineDesc.BoundResources.Length * 4);

                _commandList.SetGraphicsRoot32BitConstants(0, size, (nint)Unsafe.AsPointer(ref _resourceCpuDescriptors[0]), 0);
            }

            _barrierManager.FlushPendingTransitions(_commandList);

            if (resetRenderTargets)
            {
                int fullLength = _activeRenderTargets.ProxyLength + _activeRenderTargets.ProxyStart;
                _commandList.OMSetRenderTargets(_tempRTVDescriptors.AsSpan(0, fullLength), resetDsv);
                //_commandList.RSSetViewports(_tempViewports.AsSpan(0, fullLength));
            }

            return true;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PerformSanityCheck()
        {
            ExceptionUtility.Assert(_isOpen && _isReady);
        }

        public override string Name { set => _commandList.Name = value; }

        public override bool IsOpen => _isOpen;
        public override bool IsReady => _isReady;

        public override CommandBufferType Type => CommandBufferType.Graphics;

        public ID3D12Fence ExecutionCompleteFence => _fence;
        public ulong FenceValue => _fenceValue;

        private readonly record struct UnresolvedResource(ICommandBufferResource Resource, int DescriptorIndex, ActiveDescriptorKey DictionaryKey, uint DescriptorOffset);
        private readonly record struct VertexBuffer(BufferImpl? Buffer, uint Stride);

        private readonly record struct ActiveDescriptorKey(nint Handle, ICommandDescriptor? Descriptor)
        {
            public override int GetHashCode() => Descriptor == null ? Handle.GetHashCode() : HashCode.Combine(Handle, Descriptor);
        }
    }

    internal record struct DirtyValue<T> : GetWithoutRef<T>
    {
        private bool _isDirty;

        private T _value;

        internal DirtyValue(T initialValue = default!)
        {
            _isDirty = false;

            _value = initialValue;
        }

        internal void Reset()
        {
            _isDirty = false;
        }

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!(_value?.Equals(value) ?? false))
                    _isDirty = true;
                _value = value;
            }
        }

        [UnscopedRef]
        internal ref T ValueRef => ref _value;

        internal bool IsDirty => _isDirty;
    }

    internal interface GetWithoutRef<T>
    {
        public T Value { get; set; }
    }

    internal record struct DirtyStaticCollection<T>
    {
        private bool _isDirty;

        private T[] _collection;

        private int _proxyLength;
        private int _proxyStart;

        internal DirtyStaticCollection(int capacity, T defaultValue = default!)
        {
            _isDirty = false;

            _collection = new T[capacity];

            _proxyLength = 0;
            _proxyStart = 0;

            Array.Fill(_collection, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            _isDirty = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void MarkDirty()
        {
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear(T defaultValue = default!)
        {
            Array.Fill(_collection, defaultValue);

            _proxyLength = 0;
            _proxyStart = 0;

            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyInto(Span<T> values, int length)
        {
            values.Slice(0, length).CopyTo(_collection.AsSpan());
        }

        //refer to the "matching" method in DirtyCollection<T>
        internal T this[int index]
        {
            get
            {
#if DEBUG
                ExceptionUtility.Assert(index >= 0 && index < _collection.Length);
#endif
                return _collection.DangerousGetReferenceAt(index);
            }
            set
            {
#if DEBUG
                ExceptionUtility.Assert(index >= 0 && index < _collection.Length);
#endif
                _collection[index] = value; //TODO: do this without the internal check.
                _isDirty = true;
            }
        }

        internal Span<T> AsSpan() => _collection.AsSpan();
        internal Span<T> AsSpanProxy() => _collection.AsSpan(_proxyStart, _proxyLength);

        internal int Length => _collection.Length;

        internal int ProxyLength { get => _proxyLength; set { if (_proxyLength != value) _isDirty = true; _proxyLength = value; } }
        internal int ProxyStart { get => _proxyStart; set { if (_proxyStart != value) _isDirty = true; _proxyStart = value; } }

        internal bool IsDirty => _isDirty;
    }

    internal record struct DirtyCollection<T>
    {
        private bool _isDirty;
        private byte _dirtyStart;
        private byte _dirtyEnd;

        private T[] _collection;

        internal DirtyCollection(int capacity, T defaultValue = default!)
        {
            _isDirty = false;
            _dirtyStart = 0;
            _dirtyEnd = 0;

            _collection = new T[capacity];

            for (int i = 0; i < _collection.Length; i++)
            {
                _collection[i] = defaultValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            _isDirty = false;
            _dirtyStart = 0;
            _dirtyEnd = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear(T defaultValue = default!)
        {
            Array.Fill(_collection, defaultValue);

            _dirtyStart = 0;
            _dirtyEnd = 0;

            _isDirty = true;
        }

        private void SetDirtyRange(int index)
        {
            if (!_isDirty)
            {
                _dirtyStart = (byte)index;
                _dirtyEnd = (byte)(index + 1);
                _isDirty = true;
            }
            else
            {
                if (_dirtyEnd > index + 1)
                    _dirtyStart = (byte)Math.Min(_dirtyStart, index);
                else
                    _dirtyEnd = (byte)(index + 1);
            }
        }

        //the reason im only asserting in debug is because this class should *NEVER* try to access out of bounds.
        internal T this[int index]
        {
            get
            {
#if DEBUG
                ExceptionUtility.Assert(index >= 0 && index < _collection.Length);
#endif
                return _collection.DangerousGetReferenceAt(index);
            }
            set
            {
#if DEBUG
                ExceptionUtility.Assert(index >= 0 && index < _collection.Length);
#endif
                ref T valueRef = ref _collection.DangerousGetReferenceAt(index);
                if (/*Unsafe.IsNullRef(ref valueRef) ? value != null : !(valueRef?.Equals(value) ?? false)*/true)
                {
                    SetDirtyRange(index);
                    _collection[index] = value; //TODO: do this without the internal check.
                }

            }
        }

        internal Span<T> AsSpan() => _collection.AsSpan();
        internal Span<T> AsSpanWithinDirty() => _collection.AsSpan(_dirtyStart, _dirtyEnd - _dirtyStart);

        internal int Length => _collection.Length;

        internal bool IsDirty => _isDirty;

        internal byte DirtyIndexStart => _dirtyStart;
        internal byte DirtyIndexEnd => _dirtyEnd;

        internal int DirtyLength => _dirtyEnd - _dirtyStart;
    }
}
