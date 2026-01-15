using Arch.LowLevel;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering.NRD;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_TYPE;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class NRDDevice : INativeRenderDispatcher
    {
        private readonly RenderingManager _manager;
        private readonly RHIDevice _gd;

        private readonly IDXGIAdapter4* _adapter;
        private readonly ID3D12Device14* _device;

        private readonly D3D12MemAlloc.Allocator* _allocator;

        private readonly ID3D12CommandQueue* _graphicsQueue;
        private readonly ID3D12CommandQueue* _computeQueue;
        private readonly ID3D12CommandQueue* _copyQueue;

        private Queue<CmdListData>[] _allocatorQueue1;
        private Queue<CmdListData>[] _allocatorQueue2;

        private bool _drawCycle;

        private RasterState _rasterState;
        private ComputeState _computeState;

        private ResourceManager _resourceManager;
        private ResourceUploader _resourceUploader;
        private BarrierManager _barrierManager;

        private CpuDescriptorHeap _rtvHeap;
        private CpuDescriptorHeap _dsvHeap;

        private GpuDescriptorHeap _gpuHeap;
        private SamplerDescriptorHeap _samplerHeap;

        private QueueFence _directFence;
        private QueueFence _computeFence;
        private QueueFence _copyFence;

        private byte _freeRunningQueues;

        private int _rasterIndex;
        private int _computeIndex;

        private Queue<RHISwapChain> _awaitingPresents;

        private bool _disposedValue;

        internal NRDDevice(RenderingManager manager, RHIDevice device)
        {
            D3D12RHIDeviceNative* native = (D3D12RHIDeviceNative*)device.GetAsNative();

            _manager = manager;
            _gd = device;

            _adapter = native->Adapter->Get();
            _device = native->Device->Get();

            _allocator = native->D3D12MAllocator;

            _graphicsQueue = native->DirectCmdQueue->Get();
            _computeQueue = native->ComputeCmdQueue->Get();
            _copyQueue = native->CopyCmdQueue->Get();

            _allocatorQueue1 = [
                new Queue<CmdListData>(),
                new Queue<CmdListData>(),
                new Queue<CmdListData>()];
            _allocatorQueue2 = [
                new Queue<CmdListData>(),
                new Queue<CmdListData>(),
                new Queue<CmdListData>()];

            _drawCycle = false;

            _rasterState = new RasterState(this);
            _computeState = new ComputeState(this);

            _resourceManager = new ResourceManager(this);
            _resourceUploader = new ResourceUploader(this);
            _barrierManager = new BarrierManager(this);

            _rtvHeap = new CpuDescriptorHeap(this, 128, D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
            _dsvHeap = new CpuDescriptorHeap(this, 256, D3D12_DESCRIPTOR_HEAP_TYPE_DSV);

            _gpuHeap = new GpuDescriptorHeap(this, 2048, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
            _samplerHeap = new SamplerDescriptorHeap(this, 2048);

            _directFence = new QueueFence(this);
            _computeFence = new QueueFence(this);
            _copyFence = new QueueFence(this);

            _freeRunningQueues = 0;

            _awaitingPresents = new Queue<RHISwapChain>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if ((_freeRunningQueues & 0x1) > 0)
                    _directFence.Wait();
                if ((_freeRunningQueues & 0x2) > 0)
                    _computeFence.Wait();
                if ((_freeRunningQueues & 0x4) > 0)
                    _copyFence.Wait();
                _gd.HandlePendingUpdates();

                for (int i = 0; i < _allocatorQueue1.Length; i++)
                    while (_allocatorQueue1[i].TryDequeue(out CmdListData data))
                        data.Dispose();
                for (int i = 0; i < _allocatorQueue2.Length; i++)
                    while (_allocatorQueue1[i].TryDequeue(out CmdListData data))
                        data.Dispose();

                if (disposing)
                {
                    _copyFence.Dispose();
                    _computeFence.Dispose();
                    _directFence.Dispose();

                    _samplerHeap.Dispose();
                    _gpuHeap.Dispose();

                    _dsvHeap.Dispose();
                    _rtvHeap.Dispose();

                    _resourceUploader.Dispose();
                    _resourceManager.Dispose();
                    _rasterState.Dispose();
                }

                _disposedValue = true;
            }
        }

        ~NRDDevice()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Dispatch(RenderPassManager manager)
        {
            FrameGraphTimeline timeline = manager.Timeline;
            FrameGraphResources resources = manager.Resources;
            FrameGraphRecorder recorder = manager.Recorder;
            FrameGraphSetup setup = manager.Setup;

            if ((_freeRunningQueues & 0x1) > 0)
                _directFence.Wait();
            if ((_freeRunningQueues & 0x2) > 0)
                _computeFence.Wait();
            if ((_freeRunningQueues & 0x4) > 0)
                _copyFence.Wait();

            _freeRunningQueues = 0;

            _gd.HandlePendingUpdates();

            using (new ProfilingScope("Init"))
            {
                _resourceManager.PrepareForExecution(resources);
                _resourceUploader.PrepareUploadBuffers(resources);
                _barrierManager.ClearInternal();

                _rtvHeap.ResetForNewFrame();
                _dsvHeap.ResetForNewFrame();

                _gpuHeap.ResetForNewFrame();
                _samplerHeap.ResetForNewFrame();

                _awaitingPresents.Clear();
            }

            using (new ProfilingScope("Execute"))
            {
                foreach (nint eventPtr in timeline.Events)
                {
                    TimelineEventType eventType = Unsafe.ReadUnaligned<TimelineEventType>(eventPtr.ToPointer());
                    switch (eventType)
                    {
                        case TimelineEventType.Raster: DispatchRaster(Unsafe.ReadUnaligned<TimelineRasterEvent>(eventPtr.ToPointer()), timeline, resources, recorder); break;
                        case TimelineEventType.Compute: DispatchCompute(Unsafe.ReadUnaligned<TimelineComputeEvent>(eventPtr.ToPointer()), timeline, resources, recorder); break;
                        case TimelineEventType.Fence: DispatchFence(Unsafe.ReadUnaligned<TimelineFenceEvent>(eventPtr.ToPointer()), timeline, recorder); break;
                    }
                }

                CopyAndPresentOutput(setup, Unsafe.ReadUnaligned<TimelineEventType>(timeline.Events[timeline.Events.Length - 1].ToPointer()));
            }

            using (new ProfilingScope("Cleanup"))
            {
                _rasterState.ResetInternal();
            }

            while (_awaitingPresents.TryDequeue(out RHISwapChain? @internal))
                @internal.Present();

            if ((_freeRunningQueues & 0x1) > 0)
                _directFence.Signal(_graphicsQueue);
            if ((_freeRunningQueues & 0x2) > 0)
                _computeFence.Signal(_computeQueue);
            if ((_freeRunningQueues & 0x4) > 0)
                _copyFence.Signal(_copyQueue);

            _drawCycle = !_drawCycle;
        }

        private void DispatchRaster(TimelineRasterEvent rasterEvent, FrameGraphTimeline timeline, FrameGraphResources resources, FrameGraphRecorder graphRecorder)
        {
            _rasterState.ResetInternal();
            _barrierManager.ClearPreviousBarriers();

            CommandRecorder recorder = graphRecorder.GetRecorderForPass(rasterEvent.PassIndex)!;

            CmdListData cmdData = GetCommandListData(D3D12_COMMAND_LIST_TYPE_DIRECT);

            {
                SetHeapBundle bundle = new SetHeapBundle(_gpuHeap.GetActiveHeapOrCreateNew(), _samplerHeap.GetActiveHeapOrCreateNew());
                cmdData.CmdList.Pointer->SetDescriptorHeaps(2, (ID3D12DescriptorHeap**)&bundle);
            }

            //_resourceManager.CheckoutResourcesForPass(cmdData.CmdList.Pointer, rasterEvent.PassIndex);

            //EngLog.NRD.Information("{x}", rasterEvent.PassIndex);

            int offset = 0;
            while (offset < recorder.BufferSize)
            {
                RecCommandType commandType = recorder.GetCommandTypeAtOffset(offset);
                offset += Unsafe.SizeOf<RecCommandType>();

                //EngLog.NRD.Information("{x}", commandType);

                switch (commandType)
                {
                    case RecCommandType.Dummy:
                        {
                            offset -= Unsafe.SizeOf<RecCommandType>();

                            ExecutionCommandMeta meta = recorder.GetCommandAtOffset<ExecutionCommandMeta>(offset);
                            offset += Unsafe.SizeOf<ExecutionCommandMeta>();

                            UCDummy dummy = recorder.GetCommandAtOffset<UCDummy>(offset);
                            offset = int.MaxValue;

                            break;
                        }
                    case RecCommandType.SetRenderTarget:
                        {
                            UCSetRenderTarget cmd = recorder.GetCommandAtOffset<UCSetRenderTarget>(offset);
                            offset += Unsafe.SizeOf<UCSetRenderTarget>();

                            _rasterState.SetRenderTarget(cmd.Slot, ResourceUtility.GetNRDTextureResource(cmd.Texture, cmd.IsExternal));
                            break;
                        }
                    case RecCommandType.SetDepthStencil:
                        {
                            UCSetDepthStencil cmd = recorder.GetCommandAtOffset<UCSetDepthStencil>(offset);
                            offset += Unsafe.SizeOf<UCSetDepthStencil>();

                            _rasterState.SetDepthStencil(ResourceUtility.GetNRDTextureResource(cmd.DepthStencil, false));
                            break;
                        }
                    case RecCommandType.ClearRenderTarget:
                        {
                            UCClearRenderTarget cmd = recorder.GetCommandAtOffset<UCClearRenderTarget>(offset);
                            offset += Unsafe.SizeOf<UCClearRenderTarget>();

                            NRDResource resource = ResourceUtility.GetNRDTextureResource(cmd.RenderTarget, false);

                            _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
                            _barrierManager.FlushBarriers(cmdData.CmdList.Pointer, BarrierFlushTypes.Texture);

                            if (cmd.Rect.HasValue)
                            {
                                RECT rect = new RECT(cmd.Rect.Value.Left, cmd.Rect.Value.Top, cmd.Rect.Value.Right, cmd.Rect.Value.Bottom);
                                cmdData.CmdList.Pointer->ClearRenderTargetView(_rtvHeap.GetDescriptorHandle(resource), null, 1, &rect);
                            }
                            else
                                cmdData.CmdList.Pointer->ClearRenderTargetView(_rtvHeap.GetDescriptorHandle(resource), null, 0, null);

                            _resourceManager.SetAsInitialized(resource);
                            break;
                        }
                    case RecCommandType.ClearDepthStencil:
                        {
                            UCClearDepthStencil cmd = recorder.GetCommandAtOffset<UCClearDepthStencil>(offset);
                            offset += Unsafe.SizeOf<UCClearDepthStencil>();

                            NRDResource resource = ResourceUtility.GetNRDTextureResource(cmd.DepthStencil, false);

                            _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
                            _barrierManager.FlushBarriers(cmdData.CmdList.Pointer, BarrierFlushTypes.Texture);

                            if (cmd.Rect.HasValue)
                            {
                                RECT rect = new RECT(cmd.Rect.Value.Left, cmd.Rect.Value.Top, cmd.Rect.Value.Right, cmd.Rect.Value.Bottom);
                                cmdData.CmdList.Pointer->ClearDepthStencilView(_dsvHeap.GetDescriptorHandle(resource), (D3D12_CLEAR_FLAGS)cmd.ClearFlags, 1.0f, 0xff, 1, &rect);
                            }
                            else
                                cmdData.CmdList.Pointer->ClearDepthStencilView(_dsvHeap.GetDescriptorHandle(resource), (D3D12_CLEAR_FLAGS)cmd.ClearFlags, 1.0f, 0xff, 0, null);

                            _resourceManager.SetAsInitialized(resource);
                            break;
                        }
                    case RecCommandType.ClearRenderTargetCustom: throw new NotImplementedException();
                    case RecCommandType.ClearDepthStencilCustom: throw new NotImplementedException();
                    case RecCommandType.SetViewport:
                        {
                            UCSetViewport cmd = recorder.GetCommandAtOffset<UCSetViewport>(offset);
                            offset += Unsafe.SizeOf<UCSetViewport>();

                            _rasterState.SetViewport(cmd.Slot, cmd.Viewport);
                            break;
                        }
                    case RecCommandType.SetScissor:
                        {
                            UCSetScissor cmd = recorder.GetCommandAtOffset<UCSetScissor>(offset);
                            offset += Unsafe.SizeOf<UCSetScissor>();

                            _rasterState.SetScissor(cmd.Slot, cmd.Scissor);
                            break;
                        }
                    case RecCommandType.SetStencilReference:
                        {
                            UCSetStencilRef cmd = recorder.GetCommandAtOffset<UCSetStencilRef>(offset);
                            offset += Unsafe.SizeOf<UCSetStencilRef>();

                            _rasterState.SetStencilRef(cmd.StencilRef);
                            break;
                        }
                    case RecCommandType.SetBuffer:
                        {
                            UCSetBuffer cmd = recorder.GetCommandAtOffset<UCSetBuffer>(offset);
                            offset += Unsafe.SizeOf<UCSetBuffer>();

                            NRDResource resource = ResourceUtility.GetNRDBufferResource(cmd.Buffer, cmd.IsExternal);

                            switch (cmd.Location)
                            {
                                case Structures.FGSetBufferLocation.VertexBuffer: _rasterState.SetVertexBuffer(resource, cmd.Stride); break;
                                case Structures.FGSetBufferLocation.IndexBuffer: _rasterState.SetIndexBuffer(resource, cmd.Stride); break;
                            }
                            break;
                        }
                    case RecCommandType.SetProperties:
                        {
                            UCSetProperties cmd = recorder.GetCommandAtOffset<UCSetProperties>(offset);
                            offset += Unsafe.SizeOf<UCSetProperties>();

                            _rasterState.ClearPropertyData(cmd.ResourceCount, cmd.UseBufferForHeader);

                            if (cmd.DataBlockSize > 0)
                            {
                                _rasterState.SetPropertyRawData(recorder.GetPointerAtOffset(offset), cmd.DataBlockSize);
                                offset += cmd.DataBlockSize;
                            }

                            for (int i = 0; i < cmd.ResourceCount; i++)
                            {
                                UnmanagedPropertyData data = recorder.GetCommandAtOffset<UnmanagedPropertyData>(offset);
                                offset += Unsafe.SizeOf<UnmanagedPropertyData>();

                                switch (data.Meta.Type)
                                {
                                    case SetPropertyType.Buffer: _rasterState.SetPropertyResource(i, new NRDResource((int)data.Resource, NRDResourceId.Buffer), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.Texture: _rasterState.SetPropertyResource(i, new NRDResource((int)data.Resource, NRDResourceId.Texture), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.RHIBuffer: _rasterState.SetPropertyResource(i, new NRDResource((D3D12RHIBufferNative*)data.Resource.ToPointer()), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.RHITexture: _rasterState.SetPropertyResource(i, new NRDResource((D3D12RHITextureNative*)data.Resource.ToPointer()), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.RHISampler: _rasterState.SetPropertyResource(i, new NRDResource((D3D12RHISamplerNative*)data.Resource.ToPointer()), data.Meta.Target, data.Meta.Flags); break;
                                }
                            }

                            break;
                        }
                    case RecCommandType.SetConstants:
                        {
                            UCSetConstants cmd = recorder.GetCommandAtOffset<UCSetConstants>(offset);
                            offset += Unsafe.SizeOf<UCSetConstants>();

                            _rasterState.SetConstants(cmd.DataPointer, cmd.ConstantsDataSize);
                            break;
                        }
                    case RecCommandType.UploadBuffer:
                        {
                            UCUploadBuffer cmd = recorder.GetCommandAtOffset<UCUploadBuffer>(offset);
                            offset += Unsafe.SizeOf<UCUploadBuffer>();

                            _resourceUploader.UploadBuffer(cmdData.CmdList.Pointer, resources, cmd.BufferUploadIndex, cmd.DataPointer, (int)cmd.DataSize, (int)cmd.BufferOffset);
                            break;
                        }
                    case RecCommandType.UploadTexture:
                        {
                            UCUploadTexture cmd = recorder.GetCommandAtOffset<UCUploadTexture>(offset);
                            offset += Unsafe.SizeOf<UCUploadTexture>();

                            _resourceUploader.UploadTexture(cmdData.CmdList.Pointer, resources, cmd.TextureUploadIndex, cmd.DestinationBox, cmd.SubresourceIndex, cmd.DataPointer, (int)cmd.DataSize, (int)cmd.DataRowPitch);
                            break;
                        }
                    case RecCommandType.CopyBuffer: throw new NotImplementedException();
                    case RecCommandType.CopyTexture:
                        {
                            UCCopyTexture cmd = recorder.GetCommandAtOffset<UCCopyTexture>(offset);
                            offset += Unsafe.SizeOf<UCCopyTexture>();

                            NRDResource src = cmd.Source.ResourceType == FGResourceId.Texture ?
                                    ResourceUtility.GetNRDTextureResource(cmd.Source.Resource, cmd.Source.IsExternal) :
                                    ResourceUtility.GetNRDBufferResource(cmd.Source.Resource, cmd.Source.IsExternal);

                            NRDResource dst = cmd.Destination.ResourceType == FGResourceId.Texture ?
                                    ResourceUtility.GetNRDTextureResource(cmd.Destination.Resource, cmd.Destination.IsExternal) :
                                    ResourceUtility.GetNRDBufferResource(cmd.Destination.Resource, cmd.Destination.IsExternal);

                            if (src.Id == NRDResourceId.Texture)
                                _resourceManager.EnsureInitialized(src);
                            if (dst.Id == NRDResourceId.Texture)
                                _resourceManager.EnsureInitialized(dst);

                            //counter intuative but if they are uninitialized they cannot be used
                            _resourceManager.FlushPendingInits(cmdData.CmdList.Pointer);

                            D3D12_PLACED_SUBRESOURCE_FOOTPRINT footprint1 = default;
                            D3D12_PLACED_SUBRESOURCE_FOOTPRINT footprint2 = default;

                            D3D12_TEXTURE_COPY_LOCATION srcCopyLoc = default;
                            D3D12_TEXTURE_COPY_LOCATION dstCopyLoc = default;

                            if (cmd.Source.Type == UCCopyTexture.CopySourceType.SubresourceIndex)
                            {
                                srcCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(src), cmd.Source.SubresourceIndex);
                                _barrierManager.AddTextureBarrier(srcCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE, D3D12_BARRIER_LAYOUT_COPY_SOURCE, new D3D12_BARRIER_SUBRESOURCE_RANGE(cmd.Source.SubresourceIndex));
                            }
                            else
                            {
                                UCCopyTexture.Footprint footprint = cmd.Source.Footprint;

                                footprint1 = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
                                {
                                    Offset = footprint.Offset,
                                    Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                                    {
                                        Format = footprint.Format.ToTextureFormat(),
                                        Width = footprint.Width,
                                        Height = footprint.Height,
                                        Depth = footprint.Depth,
                                        RowPitch = footprint.RowPitch
                                    }
                                };

                                srcCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(src), &footprint1);
                            
                                if (src.Id == NRDResourceId.Buffer)
                                    _barrierManager.AddBufferBarrier(srcCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE);
                                else
                                    _barrierManager.AddTextureBarrier(srcCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE, D3D12_BARRIER_LAYOUT_COPY_SOURCE);
                            }

                            if (cmd.Destination.Type == UCCopyTexture.CopySourceType.SubresourceIndex)
                            {
                                dstCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(dst), cmd.Destination.SubresourceIndex);
                                _barrierManager.AddTextureBarrier(dstCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST, new D3D12_BARRIER_SUBRESOURCE_RANGE(cmd.Source.SubresourceIndex));
                            }
                            else
                            {
                                UCCopyTexture.Footprint footprint = cmd.Destination.Footprint;

                                footprint2 = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
                                {
                                    Offset = footprint.Offset,
                                    Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                                    {
                                        Format = footprint.Format.ToTextureFormat(),
                                        Width = footprint.Width,
                                        Height = footprint.Height,
                                        Depth = footprint.Depth,
                                        RowPitch = footprint.RowPitch
                                    }
                                };

                                dstCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(dst), &footprint2);

                                if (src.Id == NRDResourceId.Buffer)
                                    _barrierManager.AddBufferBarrier(dstCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST);
                                else
                                    _barrierManager.AddTextureBarrier(dstCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST);
                            }

                            D3D12_BOX box = default;
                            if (cmd.SourceBox.HasValue)
                            {
                                FGBox val = cmd.SourceBox.Value;
                                box = new D3D12_BOX(val.X, val.Y, val.Z, val.Width, val.Height, val.Depth);
                            }

                            _barrierManager.FlushBarriers(cmdData.CmdList.Pointer, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                            cmdData.CmdList.Pointer->CopyTextureRegion(&dstCopyLoc, cmd.DstX, cmd.DstY, cmd.DstZ, &srcCopyLoc, cmd.SourceBox.HasValue ? &box : null);
                            break;
                        }
                    case RecCommandType.DrawInstanced:
                        {
                            offset -= Unsafe.SizeOf<RecCommandType>();

                            ExecutionCommandMeta meta = recorder.GetCommandAtOffset<ExecutionCommandMeta>(offset);
                            offset += Unsafe.SizeOf<ExecutionCommandMeta>();

                            UCDrawInstanced cmd = recorder.GetCommandAtOffset<UCDrawInstanced>(offset);
                            offset += Unsafe.SizeOf<UCDrawInstanced>();

                            if (_rasterState.FlushState(cmdData.CmdList.Pointer))
                                cmdData.CmdList.Pointer->DrawInstanced(cmd.VertexCount, cmd.InstanceCount, cmd.StartVertex, cmd.StartInstance);

                            break;
                        }
                    case RecCommandType.DrawIndexedInstanced:
                        {
                            offset -= Unsafe.SizeOf<RecCommandType>();

                            ExecutionCommandMeta meta = recorder.GetCommandAtOffset<ExecutionCommandMeta>(offset);
                            offset += Unsafe.SizeOf<ExecutionCommandMeta>();

                            UCDrawIndexedInstanced cmd = recorder.GetCommandAtOffset<UCDrawIndexedInstanced>(offset);
                            offset += Unsafe.SizeOf<UCDrawIndexedInstanced>();

                            if (_rasterState.FlushState(cmdData.CmdList.Pointer))
                                cmdData.CmdList.Pointer->DrawIndexedInstanced(cmd.IndexCount, cmd.InstanceCount, cmd.StartIndex, cmd.BaseVertex, cmd.StartInstance);

                            break;
                        }
                    case RecCommandType.SetPipeline:
                        {
                            UCSetPipeline cmd = recorder.GetCommandAtOffset<UCSetPipeline>(offset);
                            offset += Unsafe.SizeOf<UCSetPipeline>();

                            _rasterState.SetPipeline(Unsafe.As<RHIGraphicsPipeline>(resources.GetPipelineFromIndex((int)cmd.Pipeline)));
                            break;
                        }
                    case RecCommandType.PresentOnWindow:
                        {
                            offset -= Unsafe.SizeOf<RecCommandType>();

                            ExecutionCommandMeta meta = recorder.GetCommandAtOffset<ExecutionCommandMeta>(offset);
                            offset += Unsafe.SizeOf<ExecutionCommandMeta>();

                            UCPresentOnWindow cmd = recorder.GetCommandAtOffset<UCPresentOnWindow>(offset);
                            offset += Unsafe.SizeOf<UCPresentOnWindow>();

                            Window? window = WindowManager.Instance.FindWindow((SDL.SDL_WindowID)cmd.WindowId);
                            if (window != null)
                            {
                                D3D12RHISwapChain swapChain = Unsafe.As<D3D12RHISwapChain>(_manager.SwapChainCache.GetForWindow(window));
                                D3D12RHISwapChainNative* native = (D3D12RHISwapChainNative*)swapChain.GetAsNative();

                                int activeIndex = native->ActiveBufferIndex;//(int)swapChain.SwapChain.Get()->GetCurrentBackBufferIndex();

                                ref D3D12RHISwapChainBuffer currentBuffer = ref native->Buffers[activeIndex];
                                NRDResource resource = ResourceUtility.GetNRDTextureResource(cmd.Texture, cmd.IsExternal);

                                _barrierManager.SetResourceState((ID3D12Resource*)currentBuffer.Resource.Get(), new NRDResourceState(FGResourceId.Texture, currentBuffer.BarrierSync, currentBuffer.BarrierAccess, currentBuffer.BarrierLayout));

                                _barrierManager.AddTextureBarrier((ID3D12Resource*)currentBuffer.Resource.Get(), D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST);
                                _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE, D3D12_BARRIER_LAYOUT_COPY_SOURCE);
                                _barrierManager.FlushBarriers(cmdData.CmdList.Pointer, BarrierFlushTypes.Texture);

                                D3D12_TEXTURE_COPY_LOCATION dst = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)currentBuffer.Resource.Get());
                                D3D12_TEXTURE_COPY_LOCATION src = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)resource.GetNativeResource(_resourceManager));

                                cmdData.CmdList.Pointer->CopyTextureRegion(&dst, 0, 0, 0, &src, null);

                                _barrierManager.AddTextureBarrier((ID3D12Resource*)currentBuffer.Resource.Get(), D3D12_BARRIER_SYNC_DRAW, D3D12_BARRIER_ACCESS_COMMON, D3D12_BARRIER_LAYOUT_PRESENT);
                                _barrierManager.FlushBarriers(cmdData.CmdList.Pointer, BarrierFlushTypes.Texture);

                                _awaitingPresents.Enqueue(swapChain);

                                currentBuffer.BarrierSync = D3D12_BARRIER_SYNC_DRAW;
                                currentBuffer.BarrierAccess = D3D12_BARRIER_ACCESS_COMMON;
                                currentBuffer.BarrierLayout = D3D12_BARRIER_LAYOUT_PRESENT;
                            }

                            break;
                        }
                    default: throw new Exception("Unrecognized command: {c}" + commandType);
                }
            }

            ExecuteCommandListData(D3D12_COMMAND_LIST_TYPE_DIRECT, cmdData);
            _freeRunningQueues |= 0x1;
        }

        private void DispatchCompute(TimelineComputeEvent computeEvent, FrameGraphTimeline timeline, FrameGraphResources resources, FrameGraphRecorder graphRecorder)
        {
            _computeState.ResetInternal();
            _barrierManager.ClearPreviousBarriers();

            CommandRecorder recorder = graphRecorder.GetRecorderForPass(computeEvent.PassIndex)!;

            CmdListData cmdData = GetCommandListData(D3D12_COMMAND_LIST_TYPE_COMPUTE);

            {
                SetHeapBundle bundle = new SetHeapBundle(_gpuHeap.GetActiveHeapOrCreateNew(), _samplerHeap.GetActiveHeapOrCreateNew());
                cmdData.CmdList.Pointer->SetDescriptorHeaps(2, (ID3D12DescriptorHeap**)&bundle);
            }

            //_resourceManager.CheckoutResourcesForPass(cmdData.CmdList.Pointer, computeEvent.PassIndex);

            //EngLog.NRD.Information("{x}", rasterEvent.PassIndex);

            int offset = 0;
            while (offset < recorder.BufferSize)
            {
                RecCommandType commandType = recorder.GetCommandTypeAtOffset(offset);
                offset += Unsafe.SizeOf<RecCommandType>();

                //EngLog.NRD.Information("{x}", commandType);

                switch (commandType)
                {
                    case RecCommandType.Dummy:
                        {
                            offset -= Unsafe.SizeOf<RecCommandType>();

                            ExecutionCommandMeta meta = recorder.GetCommandAtOffset<ExecutionCommandMeta>(offset);
                            offset += Unsafe.SizeOf<ExecutionCommandMeta>();

                            UCDummy dummy = recorder.GetCommandAtOffset<UCDummy>(offset);
                            offset = int.MaxValue;

                            break;
                        }
                    case RecCommandType.SetProperties:
                        {
                            UCSetProperties cmd = recorder.GetCommandAtOffset<UCSetProperties>(offset);
                            offset += Unsafe.SizeOf<UCSetProperties>();

                            _computeState.ClearPropertyData(cmd.ResourceCount, cmd.UseBufferForHeader);

                            if (cmd.DataBlockSize > 0)
                            {
                                _computeState.SetPropertyRawData(recorder.GetPointerAtOffset(offset), cmd.DataBlockSize);
                                offset += cmd.DataBlockSize;
                            }

                            for (int i = 0; i < cmd.ResourceCount; i++)
                            {
                                UnmanagedPropertyData data = recorder.GetCommandAtOffset<UnmanagedPropertyData>(offset);
                                offset += Unsafe.SizeOf<UnmanagedPropertyData>();

                                switch (data.Meta.Type)
                                {
                                    case SetPropertyType.Buffer: _computeState.SetPropertyResource(i, new NRDResource((int)data.Resource, NRDResourceId.Buffer), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.Texture: _computeState.SetPropertyResource(i, new NRDResource((int)data.Resource, NRDResourceId.Texture), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.RHIBuffer: _computeState.SetPropertyResource(i, new NRDResource((D3D12RHIBufferNative*)data.Resource.ToPointer()), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.RHITexture: _computeState.SetPropertyResource(i, new NRDResource((D3D12RHITextureNative*)data.Resource.ToPointer()), data.Meta.Target, data.Meta.Flags); break;
                                    case SetPropertyType.RHISampler: _computeState.SetPropertyResource(i, new NRDResource((D3D12RHISamplerNative*)data.Resource.ToPointer()), data.Meta.Target, data.Meta.Flags); break;
                                }
                            }

                            break;
                        }
                    case RecCommandType.SetConstants:
                        {
                            UCSetConstants cmd = recorder.GetCommandAtOffset<UCSetConstants>(offset);
                            offset += Unsafe.SizeOf<UCSetConstants>();

                            _computeState.SetConstants(cmd.DataPointer, cmd.ConstantsDataSize);
                            break;
                        }
                    case RecCommandType.UploadBuffer:
                        {
                            UCUploadBuffer cmd = recorder.GetCommandAtOffset<UCUploadBuffer>(offset);
                            offset += Unsafe.SizeOf<UCUploadBuffer>();

                            _resourceUploader.UploadBuffer(cmdData.CmdList.Pointer, resources, cmd.BufferUploadIndex, cmd.DataPointer, (int)cmd.DataSize, (int)cmd.BufferOffset);
                            break;
                        }
                    case RecCommandType.UploadTexture:
                        {
                            UCUploadTexture cmd = recorder.GetCommandAtOffset<UCUploadTexture>(offset);
                            offset += Unsafe.SizeOf<UCUploadTexture>();

                            _resourceUploader.UploadTexture(cmdData.CmdList.Pointer, resources, cmd.TextureUploadIndex, cmd.DestinationBox, cmd.SubresourceIndex, cmd.DataPointer, (int)cmd.DataSize, (int)cmd.DataRowPitch);
                            break;
                        }
                    case RecCommandType.CopyBuffer: throw new NotImplementedException();
                    case RecCommandType.CopyTexture:
                        {
                            UCCopyTexture cmd = recorder.GetCommandAtOffset<UCCopyTexture>(offset);
                            offset += Unsafe.SizeOf<UCCopyTexture>();

                            NRDResource src = cmd.Source.ResourceType == FGResourceId.Texture ?
                                    ResourceUtility.GetNRDTextureResource(cmd.Source.Resource, cmd.Source.IsExternal) :
                                    ResourceUtility.GetNRDBufferResource(cmd.Source.Resource, cmd.Source.IsExternal);

                            NRDResource dst = cmd.Destination.ResourceType == FGResourceId.Texture ?
                                    ResourceUtility.GetNRDTextureResource(cmd.Destination.Resource, cmd.Destination.IsExternal) :
                                    ResourceUtility.GetNRDBufferResource(cmd.Destination.Resource, cmd.Destination.IsExternal);

                            if (src.Id == NRDResourceId.Texture)
                                _resourceManager.EnsureInitialized(src);
                            if (dst.Id == NRDResourceId.Texture)
                                _resourceManager.EnsureInitialized(dst);

                            //counter intuative but if they are uninitialized they cannot be used
                            _resourceManager.FlushPendingInits(cmdData.CmdList.Pointer);

                            D3D12_PLACED_SUBRESOURCE_FOOTPRINT footprint1 = default;
                            D3D12_PLACED_SUBRESOURCE_FOOTPRINT footprint2 = default;

                            D3D12_TEXTURE_COPY_LOCATION srcCopyLoc = default;
                            D3D12_TEXTURE_COPY_LOCATION dstCopyLoc = default;

                            if (cmd.Source.Type == UCCopyTexture.CopySourceType.SubresourceIndex)
                            {
                                srcCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(src), cmd.Source.SubresourceIndex);
                                _barrierManager.AddTextureBarrier(srcCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE, D3D12_BARRIER_LAYOUT_COPY_SOURCE, new D3D12_BARRIER_SUBRESOURCE_RANGE(cmd.Source.SubresourceIndex));
                            }
                            else
                            {
                                UCCopyTexture.Footprint footprint = cmd.Source.Footprint;

                                footprint1 = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
                                {
                                    Offset = footprint.Offset,
                                    Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                                    {
                                        Format = footprint.Format.ToTextureFormat(),
                                        Width = footprint.Width,
                                        Height = footprint.Height,
                                        Depth = footprint.Depth,
                                        RowPitch = footprint.RowPitch
                                    }
                                };

                                srcCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(src), &footprint1);

                                if (src.Id == NRDResourceId.Buffer)
                                    _barrierManager.AddBufferBarrier(srcCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE);
                                else
                                    _barrierManager.AddTextureBarrier(srcCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE, D3D12_BARRIER_LAYOUT_COPY_SOURCE);
                            }

                            if (cmd.Destination.Type == UCCopyTexture.CopySourceType.SubresourceIndex)
                            {
                                dstCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(dst), cmd.Destination.SubresourceIndex);
                                _barrierManager.AddTextureBarrier(dstCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST, new D3D12_BARRIER_SUBRESOURCE_RANGE(cmd.Source.SubresourceIndex));
                            }
                            else
                            {
                                UCCopyTexture.Footprint footprint = cmd.Destination.Footprint;

                                footprint2 = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
                                {
                                    Offset = footprint.Offset,
                                    Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                                    {
                                        Format = footprint.Format.ToTextureFormat(),
                                        Width = footprint.Width,
                                        Height = footprint.Height,
                                        Depth = footprint.Depth,
                                        RowPitch = footprint.RowPitch
                                    }
                                };

                                dstCopyLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_resourceManager.GetResource(dst), &footprint2);

                                if (src.Id == NRDResourceId.Buffer)
                                    _barrierManager.AddBufferBarrier(dstCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST);
                                else
                                    _barrierManager.AddTextureBarrier(dstCopyLoc.pResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST);
                            }

                            D3D12_BOX box = default;
                            if (cmd.SourceBox.HasValue)
                            {
                                FGBox val = cmd.SourceBox.Value;
                                box = new D3D12_BOX(val.X, val.Y, val.Z, val.Width, val.Height, val.Depth);
                            }

                            _barrierManager.FlushBarriers(cmdData.CmdList.Pointer, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                            cmdData.CmdList.Pointer->CopyTextureRegion(&dstCopyLoc, cmd.DstX, cmd.DstY, cmd.DstZ, &srcCopyLoc, cmd.SourceBox.HasValue ? &box : null);
                            break;
                        }
                    case RecCommandType.Dispatch:
                        {
                            offset -= Unsafe.SizeOf<RecCommandType>();

                            ExecutionCommandMeta meta = recorder.GetCommandAtOffset<ExecutionCommandMeta>(offset);
                            offset += Unsafe.SizeOf<ExecutionCommandMeta>();

                            UCDispatch cmd = recorder.GetCommandAtOffset<UCDispatch>(offset);
                            offset += Unsafe.SizeOf<UCDispatch>();

                            if (_computeState.FlushState(cmdData.CmdList.Pointer))
                                cmdData.CmdList.Pointer->Dispatch(cmd.ThreadGroupCountX, cmd.ThreadGroupCountY, cmd.ThreadGroupCountZ);
                            break;
                        }
                    case RecCommandType.SetPipeline:
                        {
                            UCSetPipeline cmd = recorder.GetCommandAtOffset<UCSetPipeline>(offset);
                            offset += Unsafe.SizeOf<UCSetPipeline>();

                            _computeState.SetPipeline(Unsafe.As<D3D12RHIComputePipeline>(resources.GetPipelineFromIndex((int)cmd.Pipeline)));
                            break;
                        }
                    default: throw new Exception("Unrecognized command: {c}" + commandType);
                }
            }

            ExecuteCommandListData(D3D12_COMMAND_LIST_TYPE_COMPUTE, cmdData);
            _freeRunningQueues |= 0x2;
        }

        private void DispatchFence(TimelineFenceEvent fenceEvent, FrameGraphTimeline timeline, FrameGraphRecorder graphRecorder)
        {

        }

        private void CopyAndPresentOutput(FrameGraphSetup setup, TimelineEventType lastEventType)
        {

        }

        public NRDResourceInfo QueryResourceInfo(FrameGraphResource resource)
        {
            D3D12_RESOURCE_DESC1 desc = ResourceManager.GetResourceDescription(resource);
            D3D12_RESOURCE_ALLOCATION_INFO allocInfo = _device->GetResourceAllocationInfo2(0, 1, &desc, null);

            if (resource.ResourceId == FGResourceId.Buffer && FlagUtility.HasFlag(resource.BufferDesc.Usage, FGBufferUsage.ConstantBuffer))
            {
                allocInfo.Alignment = 256;
                allocInfo.SizeInBytes = (ulong)((long)allocInfo.SizeInBytes + (-(long)allocInfo.SizeInBytes & 255));
            }

            return new NRDResourceInfo((int)allocInfo.SizeInBytes, (int)allocInfo.Alignment);
        }

        public NRDResourceInfo QueryBufferInfo(FrameGraphBuffer buffer, int offset, int size)
        {
            D3D12_RESOURCE_DESC1 desc = ResourceManager.GetBufferDescription(size);
            D3D12_RESOURCE_ALLOCATION_INFO allocInfo = _device->GetResourceAllocationInfo2(0, 1, &desc, null);

            if (FlagUtility.HasFlag(buffer.Description.Usage, FGBufferUsage.ConstantBuffer))
            {
                allocInfo.Alignment = 256;
                allocInfo.SizeInBytes = (ulong)((long)allocInfo.SizeInBytes + (-(long)allocInfo.SizeInBytes & 255));
            }

            return new NRDResourceInfo((int)allocInfo.SizeInBytes, (int)allocInfo.Alignment);
        }

        public NRDResourceInfo QueryTextureInfo(FrameGraphTexture texture, int offset, int size)
        {
            D3D12_RESOURCE_DESC1 desc = ResourceManager.GetBufferDescription(size);
            D3D12_RESOURCE_ALLOCATION_INFO allocInfo = _device->GetResourceAllocationInfo2(0, 1, &desc, null);

            return new NRDResourceInfo((int)allocInfo.SizeInBytes, (int)allocInfo.Alignment);
        }

        private CmdListData GetCommandListData(D3D12_COMMAND_LIST_TYPE listType)
        {
            int idx = listType switch
            {
                D3D12_COMMAND_LIST_TYPE_DIRECT => 0,
                D3D12_COMMAND_LIST_TYPE_COPY => 1,
                D3D12_COMMAND_LIST_TYPE_COMPUTE => 2,
            };

            Queue<CmdListData> queue = _drawCycle ? _allocatorQueue1[idx] : _allocatorQueue2[idx];
            if (!queue.TryDequeue(out CmdListData data))
            {
                ID3D12GraphicsCommandList10* ptr = null;
                HRESULT hr = _device->CreateCommandList1(0, listType, D3D12_COMMAND_LIST_FLAG_NONE, UuidOf.Get<ID3D12GraphicsCommandList10>(), (void**)&ptr);

                if (hr.FAILED)
                {
                    _gd.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                ID3D12CommandAllocator* ptr2 = null;
                hr = _device->CreateCommandAllocator(listType, UuidOf.Get<ID3D12CommandAllocator>(), (void**)&ptr2);

                if (hr.FAILED)
                {
                    _gd.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                data = new CmdListData(ptr, ptr2);
            }

            data.Allocator.Pointer->Reset();
            data.CmdList.Pointer->Reset(data.Allocator.Pointer, null);

            return data;
        }

        private void ExecuteCommandListData(D3D12_COMMAND_LIST_TYPE listType, CmdListData data)
        {
            data.CmdList.Pointer->Close();

            int idx = listType switch
            {
                D3D12_COMMAND_LIST_TYPE_DIRECT => 0,
                D3D12_COMMAND_LIST_TYPE_COPY => 1,
                D3D12_COMMAND_LIST_TYPE_COMPUTE => 2,
            };

            Queue<CmdListData> queue = !_drawCycle ? _allocatorQueue1[idx] : _allocatorQueue2[idx];
            queue.Enqueue(data);

            ID3D12GraphicsCommandList10* ptr = data.CmdList.Pointer;
            switch (listType)
            {
                case D3D12_COMMAND_LIST_TYPE_DIRECT: _graphicsQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
                case D3D12_COMMAND_LIST_TYPE_COMPUTE: _computeQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
                case D3D12_COMMAND_LIST_TYPE_COPY: _copyQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
            }
        }

        internal RHIDevice RHIDevice => _gd;

        internal ID3D12Device14* Device => _device;

        internal D3D12MemAlloc.Allocator* Allocator => _allocator;

        internal ResourceManager ResourceManager => _resourceManager;
        internal BarrierManager BarrierManager => _barrierManager;

        internal CpuDescriptorHeap RTVDescriptorHeap => _rtvHeap;
        internal CpuDescriptorHeap DSVDescriptorHeap => _dsvHeap;

        internal GpuDescriptorHeap GPUDescriptorHeap => _gpuHeap;
        internal SamplerDescriptorHeap SamplerDescriptorHeap => _samplerHeap;

        private readonly record struct CmdListData(Ptr<ID3D12GraphicsCommandList10> CmdList, Ptr<ID3D12CommandAllocator> Allocator) : IDisposable
        {
            public void Dispose()
            {
                CmdList.Pointer->Release();
                Allocator.Pointer->Release();
            }
        }
    }
}
