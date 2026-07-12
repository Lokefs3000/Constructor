using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering.Commands;
using Primary.Rendering.NRD;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.Rendering.Structures;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Primary.Windowing;

using static Primary.Interop.PIX;

using D3D12MemAlloc = Interop.D3D12MemAlloc;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Vortice.Direct3D;

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

        private RenderState _state;

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
        private HashSet<D3D12RHISwapChain> _activeSwapChains;

        private bool _hasPixAvailable;

        private bool _disposedValue;

        internal NRDDevice(RenderingManager manager, RHIDevice device)
        {
            D3D12RHIDeviceNative* native = (D3D12RHIDeviceNative*)device.GetAsNative();

            _manager = manager;
            _gd = device;

            _adapter = native->Adapter;
            _device = native->Device;

            _allocator = native->D3D12MAllocator;

            _graphicsQueue = native->DirectCmdQueue;
            _computeQueue = native->ComputeCmdQueue;
            _copyQueue = native->CopyCmdQueue;

            _allocatorQueue1 = [
                new Queue<CmdListData>(),
                new Queue<CmdListData>(),
                new Queue<CmdListData>()];
            _allocatorQueue2 = [
                new Queue<CmdListData>(),
                new Queue<CmdListData>(),
                new Queue<CmdListData>()];

            _drawCycle = false;

            _state = new RenderState();

            _resourceManager = new ResourceManager(this);
            _resourceUploader = new ResourceUploader(this);
            _barrierManager = new BarrierManager(this);

            _rtvHeap = new CpuDescriptorHeap(this, 128, DescriptorHeapType.Rtv);
            _dsvHeap = new CpuDescriptorHeap(this, 256, DescriptorHeapType.Dsv);

            _gpuHeap = new GpuDescriptorHeap(this, 2048, DescriptorHeapType.CbvSrvUav);
            _samplerHeap = new SamplerDescriptorHeap(this, 2048);

            _directFence = new QueueFence(this);
            _computeFence = new QueueFence(this);
            _copyFence = new QueueFence(this);

            _freeRunningQueues = 0;

            _awaitingPresents = new Queue<RHISwapChain>();
            _activeSwapChains = new HashSet<D3D12RHISwapChain>();

            _hasPixAvailable = CheckForPIXBinaries();
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
            FrameGraphRecorder recorders = manager.Recorder;
            FrameGraphSetup setup = manager.Setup;

            D3D12RHIDevice d3d12 = Unsafe.As<D3D12RHIDevice>(_gd);

            if (_freeRunningQueues > 0)
            {
                using (new ProfilingScope("Fences"))
                {
                    if ((_freeRunningQueues & 0x1) > 0)
                    {
                        using (new ProfilingScope("GraphicsWait"))
                        {
                            _directFence.Wait();
                        }
                    }
                    if ((_freeRunningQueues & 0x2) > 0)
                    {
                        using (new ProfilingScope("ComputeWait"))
                        {
                            _computeFence.Wait();
                        }
                    }
                    if ((_freeRunningQueues & 0x4) > 0)
                    {
                        using (new ProfilingScope("CopyWait"))
                        {
                            _copyFence.Wait();
                        }
                    }

                    _freeRunningQueues = 0;
                }
            }

            using (new ProfilingScope("GdUpdate"))
            {
                _gd.HandlePendingUpdates();
            }

            if (d3d12.HasPendingUploads)
            {
                using (new ProfilingScope("DeviceUpload"))
                {
                    CmdListData listData = GetCommandListData(CommandListType.Direct);
                    d3d12.UploadPendingData(ref listData.Cmds.Get());

                    ExecuteCommandListData(CommandListType.Direct, listData);
                }
            }

            using (new ProfilingScope("Init"))
            {
                _resourceManager.PrepareForExecution(resources);
                _resourceUploader.PrepareUploadBuffers(resources);
                _barrierManager.ClearInternal();

                _state.Clear(this);

                _rtvHeap.ResetForNewFrame();
                _dsvHeap.ResetForNewFrame();

                _gpuHeap.ResetForNewFrame();
                _samplerHeap.ResetForNewFrame();

                _awaitingPresents.Clear();
            }

            using (new ProfilingScope("Execute"))
            {
                int previousPassIndex = -1;
                CmdListData? currentList = null;
                TimelineEventType lastEventType = TimelineEventType.Raster;

                bool hasSwapChainRt = false;
                int pixEventDepth = 0;

                foreach (nint eventPtr in timeline.Events)
                {
                    TimelineEventType eventType = Unsafe.ReadUnaligned<TimelineEventType>(eventPtr.ToPointer());
                    if (eventType == TimelineEventType.Fence)
                    {
                        TimelineFenceEvent eventData = Unsafe.ReadUnaligned<TimelineFenceEvent>(eventPtr.ToPointer());
                        _freeRunningQueues &= (byte)~(1 << (int)eventData.QueueToWait);

                        throw new NotImplementedException();
                    }
                    else
                    {
                        int passIndex = Unsafe.ReadUnaligned<TimelineRasterEvent>(eventPtr.ToPointer()).PassIndex;
                        CommandRecorder recorder = recorders.GetRecorderForPass(passIndex)!;

                        CommandListType listType = GetListTypeForEvent(eventType);
                        _freeRunningQueues |= (byte)(1 << (int)eventType);

                        if (lastEventType != eventType && currentList.HasValue)
                        {
                            if (previousPassIndex != -1)
                            {
                                if (lastEventType == TimelineEventType.Raster)
                                    TransitionSwapChains(currentList.Value);
                                _barrierManager.TransitionToCompatible(ref currentList.Value.Cmds.Get(), listType, recorder);
                            }

                            ExecuteCommandListData(GetListTypeForEvent(lastEventType), currentList.Value);

                            currentList = null;
                            lastEventType = eventType;
                        }

                        CmdListData cmds = currentList.HasValue ? currentList.Value : GetCommandListData(listType);
                        if (!currentList.HasValue)
                        {
                            {
                                SetHeapBundle bundle = new SetHeapBundle(_gpuHeap.GetActiveHeapOrCreateNew(), _samplerHeap.GetActiveHeapOrCreateNew());
                                cmds.Cmds.SetDescriptorHeaps(2, (ID3D12DescriptorHeap**)&bundle);
                            }

                            if (listType == CommandListType.Direct)
                                RestoreGraphicsCmdState(cmds.Cmds);

                            pixEventDepth = 0;
                        }

                        string currentPassName = manager.RenderPass.Passes[timeline.Passes[passIndex]].Name;
                        if (_hasPixAvailable)
                        {
                            uint nameColor = (uint)(currentPassName.GetDjb2HashCode() | 0xff000000);
                            PIXBeginEventOnCommandList((nint)Unsafe.AsPointer(ref cmds.Cmds.Get()), nameColor, currentPassName);
                        }

                        previousPassIndex = passIndex;
                        currentList = cmds;

                        int resourceByteOffsetBackup = -1;

                        using (new ProfilingScope(currentPassName))
                        {
                            int byteOffset = 0;
                            while (byteOffset < recorder.BufferSize)
                            {
                                RecCommandType commandType = recorder.GetCommandTypeAtOffset(byteOffset++);

                                switch (commandType)
                                {
                                    //Shared
                                    case RecCommandType.UploadBuffer:
                                        {
                                            CmdUploadBuffer cmd = recorder.GetCommandAtOffset<CmdUploadBuffer>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdUploadBuffer>();

                                            _resourceUploader.UploadBuffer(
                                                ref cmds.Cmds.Get(),
                                                resources,
                                                cmd.UploadIndex,
                                                cmd.DataPointer,
                                                (int)cmd.DataSize,
                                                (int)cmd.BufferOffset);

                                            break;
                                        }
                                    case RecCommandType.UploadTexture:
                                        {
                                            CmdUploadTexture cmd = recorder.GetCommandAtOffset<CmdUploadTexture>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdUploadTexture>();

                                            _resourceUploader.UploadTexture(
                                                ref cmds.Cmds.Get(),
                                                resources,
                                                cmd.UploadIndex,
                                                cmd.Box,
                                                cmd.SubresourceIndex,
                                                cmd.DataPointer,
                                                (int)cmd.DataSize,
                                                (int)cmd.DataRowPitch);

                                            break;
                                        }
                                    case RecCommandType.CopyBuffer:
                                        {
                                            CmdCopyBuffer cmd = recorder.GetCommandAtOffset<CmdCopyBuffer>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdCopyBuffer>();

                                            NRDResource sourceResource = ResourceUtility.AsNRDResource(cmd.Source);
                                            ID3D12Resource* sourceNative = (ID3D12Resource*)_resourceManager.GetResource(sourceResource);

                                            NRDResource destinationResource = ResourceUtility.AsNRDResource(cmd.Destination);
                                            ID3D12Resource* destinationNative = (ID3D12Resource*)_resourceManager.GetResource(destinationResource);

                                            _barrierManager.AddBufferBarrier(sourceNative, BarrierSync.Copy, BarrierAccess.CopySource);
                                            _barrierManager.AddBufferBarrier(destinationNative, BarrierSync.Copy, BarrierAccess.CopyDest);
                                            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Buffer);

                                            cmds.Cmds.CopyBufferRegion(destinationNative, cmd.DestinationOffset, sourceNative, cmd.SourceOffset, cmd.NumBytes);

                                            break;
                                        }
                                    case RecCommandType.CopyTexture:
                                        {
                                            CmdCopyTexture cmd = recorder.GetCommandAtOffset<CmdCopyTexture>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdCopyTexture>();

                                            NRDResource sourceResource = ResourceUtility.AsNRDResource(cmd.Source.Resource);
                                            ID3D12Resource* sourceNative = (ID3D12Resource*)_resourceManager.GetResource(sourceResource);

                                            PlacedSubresourceFootprint sourcePlacedFootprint;
                                            TextureCopyLocation sourceCopyLocation;

                                            NRDResource destinationResource = ResourceUtility.AsNRDResource(cmd.Destination.Resource);
                                            ID3D12Resource* destinationNative = (ID3D12Resource*)_resourceManager.GetResource(destinationResource);

                                            PlacedSubresourceFootprint destinationPlacedFootprint;
                                            TextureCopyLocation destinationCopyLocation;

                                            if (cmd.Source.Type == CmdDataTextureSourceType.SubresourceIndex)
                                            {
                                                sourceCopyLocation = new TextureCopyLocation(sourceNative, type: TextureCopyType.SubresourceIndex, subresourceIndex: cmd.Source.SubresourceIndex);
                                            }
                                            else
                                            {
                                                ref CmdDataTextureFootprint footprint = ref cmd.Source.Footprint;
                                                sourcePlacedFootprint = new PlacedSubresourceFootprint
                                                {
                                                    Offset = footprint.Offset,
                                                    Footprint = new SubresourceFootprint
                                                    {
                                                        Format = footprint.Format.ToTextureFormat(),

                                                        Width = footprint.Width,
                                                        Height = footprint.Height,
                                                        Depth = footprint.Depth,

                                                        RowPitch = footprint.RowPitch
                                                    }
                                                };
                                                sourceCopyLocation = new TextureCopyLocation(sourceNative, type: TextureCopyType.PlacedFootprint, placedFootprint: sourcePlacedFootprint);
                                            }

                                            if (cmd.Destination.Type == CmdDataTextureSourceType.SubresourceIndex)
                                            {
                                                destinationCopyLocation = new TextureCopyLocation(destinationNative, type: TextureCopyType.SubresourceIndex, subresourceIndex: cmd.Destination.SubresourceIndex);
                                            }
                                            else
                                            {
                                                ref CmdDataTextureFootprint footprint = ref cmd.Destination.Footprint;
                                                destinationPlacedFootprint = new PlacedSubresourceFootprint
                                                {
                                                    Offset = footprint.Offset,
                                                    Footprint = new SubresourceFootprint
                                                    {
                                                        Format = footprint.Format.ToTextureFormat(),

                                                        Width = footprint.Width,
                                                        Height = footprint.Height,
                                                        Depth = footprint.Depth,

                                                        RowPitch = footprint.RowPitch
                                                    }
                                                };
                                                destinationCopyLocation = new TextureCopyLocation(destinationNative, type: TextureCopyType.PlacedFootprint, placedFootprint: destinationPlacedFootprint);
                                            }

                                            Box box = default;
                                            if (cmd.SourceBox.HasValue)
                                            {
                                                FGBox val = cmd.SourceBox.Value;
                                                box = new Box((uint)val.X, (uint)val.Y, (uint)val.Z, (uint)val.Width, (uint)val.Height, (uint)val.Depth);
                                            }

                                            {
                                                if (sourceResource.Id == NRDResourceId.Texture && ResourceUtility.DoesTextureNeedInit(sourceResource, _resourceManager))
                                                    _resourceManager.EnsureInitialized(sourceResource);
                                                if (destinationResource.Id == NRDResourceId.Texture && ResourceUtility.DoesTextureNeedInit(destinationResource, _resourceManager))
                                                    _resourceManager.EnsureInitialized(destinationResource);

                                                _resourceManager.FlushPendingInits(ref cmds.Cmds.Get());
                                            }

                                            {
                                                if (sourceResource.Id == NRDResourceId.Buffer)
                                                    _barrierManager.AddBufferBarrier(sourceNative, BarrierSync.Copy, BarrierAccess.CopySource);
                                                else
                                                    _barrierManager.AddTextureBarrier(sourceNative, BarrierSync.Copy, BarrierAccess.CopySource, BarrierLayout.CopySource);

                                                if (destinationResource.Id == NRDResourceId.Buffer)
                                                    _barrierManager.AddBufferBarrier(destinationResource, BarrierSync.Copy, BarrierAccess.CopyDest);
                                                else
                                                    _barrierManager.AddTextureBarrier(destinationResource, BarrierSync.Copy, BarrierAccess.CopyDest, BarrierLayout.CopyDest);
                                            }

                                            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                            cmds.Cmds.CopyTextureRegion(&destinationCopyLocation, cmd.DstX, cmd.DstY, cmd.DstZ, &sourceCopyLocation, cmd.SourceBox.HasValue ? &box : null);

                                            break;
                                        }
                                    case RecCommandType.SetPipeline:
                                        {
                                            CmdSetPipeline cmd = recorder.GetCommandAtOffset<CmdSetPipeline>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetPipeline>();

                                            if (listType == CommandListType.Direct)
                                            {
                                                D3D12RHIGraphicsPipeline pipeline = Unsafe.As<D3D12RHIGraphicsPipeline>(resources.GetPipelineFromIndex(cmd.Index))!;

                                                ID3D12PipelineState* pipelineState = (ID3D12PipelineState*)Unsafe.AsPointer(ref pipeline.GetPipelineState(_state.RasterState));
                                                if (pipelineState == null)
                                                    throw new NullReferenceException();

                                                cmds.Cmds.SetPipelineState(pipelineState);
                                                cmds.Cmds.SetGraphicsRootSignature((ID3D12RootSignature*)Unsafe.AsPointer(ref pipeline.RootSignature.Get()));

                                                cmds.Cmds.IASetPrimitiveTopology(pipeline.Description.PrimitiveTopologyType switch
                                                {
                                                    RHIPrimitiveTopologyType.Triangle => D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist,
                                                    RHIPrimitiveTopologyType.Line => D3DPrimitiveTopology.D3DPrimitiveTopologyLinelist,
                                                    RHIPrimitiveTopologyType.Point => D3DPrimitiveTopology.D3DPrimitiveTopologyPointlist,
                                                    _ => throw new NotImplementedException(),
                                                });
                                            }
                                            else if (listType == CommandListType.Compute)
                                            {
                                                D3D12RHIComputePipeline pipeline = Unsafe.As<D3D12RHIComputePipeline>(resources.GetPipelineFromIndex(cmd.Index))!;

                                                cmds.Cmds.SetPipelineState(ref pipeline.PipelineState.Get());
                                                cmds.Cmds.SetComputeRootSignature(ref pipeline.RootSignature.Get());
                                            }

                                            break;
                                        }

                                    //Shared - Resources
                                    case RecCommandType.SetResourcesInfo:
                                        {
                                            CmdSetResourcesInfo cmd = recorder.GetCommandAtOffset<CmdSetResourcesInfo>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetResourcesInfo>();

                                            _state.HeaderFlags = cmd.HeaderFlags;
                                            _state.AllocateInternalBuffers(cmd.DataSizeRequired, cmd.ConstantsSize);

                                            break;
                                        }
                                    case RecCommandType.SetRawData:
                                        {
                                            CmdSetRawData cmd = recorder.GetCommandAtOffset<CmdSetRawData>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetRawData>();

                                            NativeMemory.Copy(cmd.DataPointer.ToPointer(), (_state.ResourceData + cmd.DataOffset).ToPointer(), (nuint)cmd.DataSize);

                                            break;
                                        }
                                    case RecCommandType.SetResource:
                                        {
                                            if (resourceByteOffsetBackup == -1)
                                                resourceByteOffsetBackup = byteOffset;

                                            CmdSetResource cmd = recorder.GetCommandAtOffset<CmdSetResource>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetResource>();

                                            if (cmd.Resource.IsNull)
                                            {
                                                if (cmd.Resource.Type == CmdResourceType.Sampler)
                                                {
                                                    uint index = _samplerHeap.GetDescriptorIndex(s_defaultSamplerDesc, out bool changedActiveHeap);
                                                    if (changedActiveHeap)
                                                    {
                                                        Debug.Assert(resourceByteOffsetBackup != -1);
                                                        byteOffset = resourceByteOffsetBackup;
                                                        break;
                                                    }

                                                    *(uint*)(_state.ResourceData + cmd.DataOffset) = index;
                                                }

                                                //*(uint*)(_state.ResourceData + cmd.DataOffset) = _gpuHeap.Nu;
                                            }
                                            else
                                            {
                                                NRDResource resource = ResourceUtility.AsNRDResource(cmd.Resource);
                                                if (resource.Id == NRDResourceId.Sampler)
                                                {
                                                    Debug.Assert(cmd.Resource.IsExternal);

                                                    uint index = _samplerHeap.GetDescriptorIndex(((D3D12RHISamplerNative*)resource.Native)->Base.Description, out bool changedActiveHeap);
                                                    if (changedActiveHeap)
                                                    {
                                                        Debug.Assert(resourceByteOffsetBackup != -1);
                                                        byteOffset = resourceByteOffsetBackup;
                                                        break;
                                                    }

                                                    *(uint*)(_state.ResourceData + cmd.DataOffset) = index;
                                                }
                                                else
                                                {
                                                    uint index = _gpuHeap.GetDescriptorIndex(resource, Flags.HasFlag(cmd.Flags, ShPropertyFlags.ReadWrite), cmd.Intent, out bool changedActiveHeap);
                                                    if (changedActiveHeap)
                                                    {
                                                        Debug.Assert(resourceByteOffsetBackup != -1);

                                                        byteOffset = resourceByteOffsetBackup;
                                                        break;
                                                    }

                                                    if (resource.Id == NRDResourceId.Buffer)
                                                    {
                                                        BarrierManager.GetShaderBufferBarriers(resource, _resourceManager, cmd.Stages, cmd.Flags, out BarrierSync sync, out BarrierAccess access);
                                                        _barrierManager.AddBufferBarrier(resource, sync, access);
                                                    }
                                                    else
                                                    {
                                                        BarrierManager.GetShaderTextureBarriers(resource, _resourceManager, cmd.Stages, cmd.Flags, out BarrierSync sync, out BarrierAccess access, out BarrierLayout layout);
                                                        _barrierManager.AddTextureBarrier(resource, sync, access, layout);
                                                    }

                                                    *(uint*)(_state.ResourceData + cmd.DataOffset) = index;
                                                }
                                            }

                                            break;
                                        }
                                    case RecCommandType.SetConstants:
                                        {
                                            CmdSetConstants cmd = recorder.GetCommandAtOffset<CmdSetConstants>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetConstants>();

                                            if (Flags.HasFlag(_state.HeaderFlags, ShHeaderFlags.HeaderIsBuffer))
                                            {
                                                NativeMemory.Copy(cmd.DataPointer.ToPointer(), _state.ConstantsData.ToPointer(), (nuint)cmd.DataSize);
                                            }
                                            else
                                            {
                                                NativeMemory.Copy(cmd.DataPointer.ToPointer(), _state.ResourceData.ToPointer(), (nuint)cmd.DataSize);
                                            }

                                            break;
                                        }
                                    case RecCommandType.CommitResources:
                                        {
                                            if (listType == CommandListType.Direct)
                                            {
                                                if (Flags.HasFlag(_state.HeaderFlags, ShHeaderFlags.HeaderIsBuffer))
                                                {
                                                    if (_state.ConstantsSize > 0)
                                                        cmds.Cmds.SetGraphicsRoot32BitConstants(0, (uint)(_state.ConstantsSize / sizeof(uint)), _state.ConstantsData.ToPointer(), 0);
                                                    if (_state.ResourceSize > 0)
                                                        throw new NotImplementedException();
                                                }
                                                else
                                                {
                                                    cmds.Cmds.SetGraphicsRoot32BitConstants(0, (uint)(_state.ResourceSize / sizeof(uint)), _state.ResourceData.ToPointer(), 0);
                                                }
                                            }
                                            else if (listType == CommandListType.Compute)
                                            {
                                                if (Flags.HasFlag(_state.HeaderFlags, ShHeaderFlags.HeaderIsBuffer))
                                                {
                                                    if (_state.ConstantsSize > 0)
                                                        cmds.Cmds.SetComputeRoot32BitConstants(0, (uint)(_state.ConstantsSize / sizeof(uint)), _state.ConstantsData.ToPointer(), 0);
                                                    if (_state.ResourceSize > 0)
                                                        throw new NotImplementedException();
                                                }
                                                else
                                                {
                                                    cmds.Cmds.SetComputeRoot32BitConstants(0, (uint)(_state.ResourceSize / sizeof(uint)), _state.ResourceData.ToPointer(), 0);
                                                }
                                            }

                                            break;
                                        }

                                    //Raster
                                    case RecCommandType.SetRenderTarget:
                                        {
                                            CmdSetRenderTarget cmd = recorder.GetCommandAtOffset<CmdSetRenderTarget>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetRenderTarget>();

                                            if (cmd.Texture.IsNull)
                                            {
                                                _state.RenderTargets[cmd.Slot] = _rtvHeap.NullDescriptor;
                                                if (cmd.Slot != 0 || !hasSwapChainRt)
                                                    _state.RasterState.RTVFormats[cmd.Slot] = Format.FormatUnknown;
                                            }
                                            else
                                            {
                                                NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);
                                                _state.RenderTargets[cmd.Slot] = _rtvHeap.GetDescriptorHandle(resource);
                                                _state.RasterState.RTVFormats[cmd.Slot] = ResourceUtility.GetTextureFormat(resource, _resourceManager).ToRenderTargetFormat();

                                                _barrierManager.AddTextureBarrier(resource, BarrierSync.RenderTarget, BarrierAccess.RenderTarget, BarrierLayout.RenderTarget);
                                                _resourceManager.EnsureInitialized(resource);

                                                if (cmd.Slot == 0)
                                                    hasSwapChainRt = false;
                                            }

                                            break;
                                        }
                                    case RecCommandType.CommitRenderTargets:
                                        {
                                            CmdCommitRenderTargets cmd = recorder.GetCommandAtOffset<CmdCommitRenderTargets>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdCommitRenderTargets>();

                                            _state.RenderTargets.Count = cmd.ActiveCount;
                                            if (!hasSwapChainRt)
                                            {
                                                _state.RasterState.RTVFormats.Count = cmd.ActiveCount;

                                                if (!cmd.DeferSetState)
                                                {
                                                    CpuDescriptorHandle dsvHandle = _state.DepthStencil;

                                                    _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Texture);
                                                    fixed (CpuDescriptorHandle* ptr = _state.RenderTargets.Span)
                                                    {
                                                        cmds.Cmds.OMSetRenderTargets(
                                                            (uint)_state.RenderTargets.Count,
                                                            ptr,
                                                            false,
                                                            _state.RasterState.DSVFormat != Format.FormatUnknown ? &dsvHandle : null);
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    case RecCommandType.SetDepthStencil:
                                        {
                                            CmdSetDepthStencil cmd = recorder.GetCommandAtOffset<CmdSetDepthStencil>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetDepthStencil>();

                                            if (!hasSwapChainRt)
                                            {
                                                NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);
                                                if (resource.IsNull)
                                                {
                                                    _state.RasterState.DSVFormat = Format.FormatUnknown;

                                                    fixed (CpuDescriptorHandle* ptr = _state.RenderTargets.Span)
                                                    {
                                                        cmds.Cmds.OMSetRenderTargets(
                                                            (uint)_state.RenderTargets.Count,
                                                            ptr,
                                                            false,
                                                            (CpuDescriptorHandle*)null);
                                                    }
                                                }
                                                else
                                                {
                                                    _state.DepthStencil = _dsvHeap.GetDescriptorHandle(resource);
                                                    _state.RasterState.DSVFormat = ResourceUtility.GetTextureFormat(resource, _resourceManager).ToDepthStencilFormat();
                                                    //TODO: Add fallback for no depth and stencil writes to *_READ instead of always *_WRITE
                                                    _barrierManager.AddTextureBarrier(resource, BarrierSync.DepthStencil, BarrierAccess.DepthStencilWrite, BarrierLayout.DepthStencilWrite);
                                                    _resourceManager.EnsureInitialized(resource);

                                                    CpuDescriptorHandle dsvHandle = _state.DepthStencil;

                                                    _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Texture);
                                                    fixed (CpuDescriptorHandle* ptr = _state.RenderTargets.Span)
                                                    {
                                                        cmds.Cmds.OMSetRenderTargets(
                                                            (uint)_state.RenderTargets.Count,
                                                            ptr,
                                                            false,
                                                            &dsvHandle);
                                                    }
                                                }
                                            }
                                            
                                            break;
                                        }
                                    case RecCommandType.ClearRenderTarget:
                                        {
                                            CmdClearRenderTarget cmd = recorder.GetCommandAtOffset<CmdClearRenderTarget>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdClearRenderTarget>();

                                            NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);

                                            _barrierManager.AddTextureBarrier(resource, BarrierSync.RenderTarget, BarrierAccess.RenderTarget, BarrierLayout.RenderTarget);
                                            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Texture);

                                            Box2D<int> rect = default;
                                            Box2D<int>* ptr = null;

                                            if (cmd.Rect.HasValue)
                                            {
                                                FGRect val = cmd.Rect.Value;
                                                rect = new Box2D<int>(val.Left, val.Top, val.Right, val.Bottom);

                                                ptr = &rect;
                                            }

                                            if (cmd.Color.HasValue)
                                            {
                                                Color color = cmd.Color.Value;
                                                cmds.Cmds.ClearRenderTargetView(_rtvHeap.GetDescriptorHandle(resource), (float*)&color, cmd.Rect.HasValue ? 1u : 0, ptr);
                                            }
                                            else
                                            {
                                                Color color = new Color(0.0f);
                                                cmds.Cmds.ClearRenderTargetView(_rtvHeap.GetDescriptorHandle(resource), (float*)&color, cmd.Rect.HasValue ? 1u : 0, ptr);
                                            }

                                            _resourceManager.SetAsInitialized(resource);

                                            break;
                                        }
                                    case RecCommandType.ClearDepthStencil:
                                        {
                                            CmdClearDepthStencil cmd = recorder.GetCommandAtOffset<CmdClearDepthStencil>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdClearDepthStencil>();

                                            NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);

                                            _barrierManager.AddTextureBarrier(resource, BarrierSync.DepthStencil, BarrierAccess.DepthStencilWrite, BarrierLayout.DepthStencilWrite);
                                            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Texture);

                                            Box2D<int> rect = default;
                                            Box2D<int>* ptr = null;

                                            if (cmd.Rect.HasValue)
                                            {
                                                FGRect val = cmd.Rect.Value;
                                                rect = new Box2D<int>(val.Left, val.Top, val.Right, val.Bottom);

                                                ptr = &rect;
                                            }

                                            ClearFlags flags = (ClearFlags)cmd.ClearFlags;

                                            float depthVal = cmd.Depth.GetValueOrDefault(1.0f);
                                            byte stencilVal = cmd.Stencil.GetValueOrDefault(0xff);

                                            if (cmd.ClearFlags != FGClearFlags.DepthStencil && !_resourceManager.IsInitialized(resource))
                                            {
                                                flags = ClearFlags.Depth | ClearFlags.Stencil;
                                            }

                                            cmds.Cmds.ClearDepthStencilView(_dsvHeap.GetDescriptorHandle(resource), flags, depthVal, stencilVal, cmd.Rect.HasValue ? 1u : 0, ptr);

                                            _resourceManager.SetAsInitialized(resource);

                                            break;
                                        }
                                    case RecCommandType.SetViewport:
                                        {
                                            CmdSetViewport cmd = recorder.GetCommandAtOffset<CmdSetViewport>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetViewport>();

                                            _state.Viewports[cmd.Slot] = new Viewport(cmd.Viewport.TopLeftX, cmd.Viewport.TopLeftY, cmd.Viewport.Width, cmd.Viewport.Height, cmd.Viewport.MinDepth, cmd.Viewport.MaxDepth);

                                            break;
                                        }
                                    case RecCommandType.CommitViewports:
                                        {
                                            CmdCommitViewports cmd = recorder.GetCommandAtOffset<CmdCommitViewports>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdCommitViewports>();

                                            _state.Viewports.Count = cmd.ActiveCount;
                                            fixed (Viewport* ptr = _state.Viewports.Span)
                                            {
                                                cmds.Cmds.RSSetViewports(cmd.ActiveCount, ptr);
                                            }
                                            break;
                                        }
                                    case RecCommandType.SetScissor:
                                        {
                                            CmdSetScissor cmd = recorder.GetCommandAtOffset<CmdSetScissor>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetScissor>();

                                            _state.Scissors[cmd.Slot] = new Box2D<int>(cmd.Scissor.Left, cmd.Scissor.Top, cmd.Scissor.Right, cmd.Scissor.Bottom);

                                            break;
                                        }
                                    case RecCommandType.CommitScissors:
                                        {
                                            CmdCommitScissors cmd = recorder.GetCommandAtOffset<CmdCommitScissors>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdCommitScissors>();

                                            _state.Scissors.Count = cmd.ActiveCount;
                                            fixed (Box2D<int>* ptr = _state.Scissors.Span)
                                            {
                                                cmds.Cmds.RSSetScissorRects(cmd.ActiveCount, ptr);
                                            }
                                            break;
                                        }
                                    case RecCommandType.SetStencilReference:
                                        {
                                            CmdSetStencilRef cmd = recorder.GetCommandAtOffset<CmdSetStencilRef>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetStencilRef>();

                                            cmds.Cmds.OMSetStencilRef(cmd.StencilRef);

                                            break;
                                        }
                                    case RecCommandType.SetVertexBuffer:
                                        {
                                            CmdSetVertexBuffer cmd = recorder.GetCommandAtOffset<CmdSetVertexBuffer>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetVertexBuffer>();

                                            if (cmd.Resource.IsNull)
                                            {
                                                cmds.Cmds.IASetVertexBuffers(0, 0, (VertexBufferView*)null);
                                            }
                                            else
                                            {
                                                NRDResource resource = ResourceUtility.AsNRDResource(cmd.Resource);
                                                ID3D12Resource* native = (ID3D12Resource*)_resourceManager.GetResource(resource);

                                                VertexBufferView vbv = new VertexBufferView
                                                {
                                                    BufferLocation = native->GetGPUVirtualAddress(),
                                                    SizeInBytes = cmd.BufferSize,
                                                    StrideInBytes = cmd.Stride,
                                                };

                                                cmds.Cmds.IASetVertexBuffers(0, 1, &vbv);
                                            }

                                            break;
                                        }
                                    case RecCommandType.SetIndexBuffer:
                                        {
                                            CmdSetIndexBuffer cmd = recorder.GetCommandAtOffset<CmdSetIndexBuffer>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdSetIndexBuffer>();

                                            if (cmd.Resource.IsNull)
                                            {
                                                cmds.Cmds.IASetIndexBuffer((IndexBufferView*)null);
                                            }
                                            else
                                            {
                                                NRDResource resource = ResourceUtility.AsNRDResource(cmd.Resource);
                                                ID3D12Resource* native = (ID3D12Resource*)_resourceManager.GetResource(resource);

                                                IndexBufferView ibv = new IndexBufferView
                                                {
                                                    BufferLocation = native->GetGPUVirtualAddress(),
                                                    SizeInBytes = cmd.BufferSize,
                                                    Format = cmd.Stride switch
                                                    {
                                                        2 => Format.FormatR16Uint,
                                                        4 => Format.FormatR32Uint,
                                                        _ => throw new NotImplementedException(),
                                                    }
                                                };

                                                cmds.Cmds.IASetIndexBuffer(&ibv);
                                            }

                                            break;
                                        }
                                    case RecCommandType.DrawInstanced:
                                        {
                                            CmdDrawInstanced cmd = recorder.GetCommandAtOffset<CmdDrawInstanced>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdDrawInstanced>();

                                            _resourceManager.FlushPendingInits(ref cmds.Cmds.Get());
                                            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                            cmds.Cmds.DrawInstanced(cmd.VertexCount, cmd.InstanceCount, cmd.StartVertex, cmd.StartInstance);

                                            break;
                                        }
                                    case RecCommandType.DrawIndexedInstanced:
                                        {
                                            CmdDrawIndexedInstanced cmd = recorder.GetCommandAtOffset<CmdDrawIndexedInstanced>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdDrawIndexedInstanced>();

                                            _resourceManager.FlushPendingInits(ref cmds.Cmds.Get());
                                            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                            cmds.Cmds.DrawIndexedInstanced(cmd.IndexCount, cmd.InstanceCount, cmd.StartIndex, cmd.BaseVertex, cmd.StartInstance);

                                            break;
                                        }
                                    case RecCommandType.PresentOnWindow:
                                        {
                                            CmdPresentOnWindow cmd = recorder.GetCommandAtOffset<CmdPresentOnWindow>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdPresentOnWindow>();

                                            Window? window = WindowManager.Instance.FindWindow(cmd.WindowId);
                                            if (window != null)
                                            {
                                                D3D12RHISwapChain swapChain = Unsafe.As<D3D12RHISwapChain>(_manager.SwapChainCache.GetForWindow(window)!);
                                                D3D12RHISwapChainNative* native = (D3D12RHISwapChainNative*)swapChain.GetAsNative();

                                                if (swapChain.HasPendingResize)
                                                    swapChain.ResizeBuffersToNewSize();

                                                int activeIndex = native->ActiveBufferIndex;//(int)swapChain.SwapChain.Get()->GetCurrentBackBufferIndex();
                                                ref D3D12RHISwapChainBuffer currentBuffer = ref native->Buffers[activeIndex];

                                                if (!_barrierManager.HasResourceState(ref currentBuffer.Resource.Get()))
                                                    _barrierManager.SetResourceState(ref currentBuffer.Resource.Get(), new NRDResourceState(FGResourceId.Texture, null, currentBuffer.BarrierSync, currentBuffer.BarrierAccess, currentBuffer.BarrierLayout));
                                                _barrierManager.AddTextureBarrier(ref currentBuffer.Resource.Get(), BarrierSync.RenderTarget, BarrierAccess.RenderTarget, BarrierLayout.RenderTarget);

                                                CpuDescriptorHandle rtvHandle = _rtvHeap.GetDescriptorHandleForSwapChain(ref Unsafe.AsRef<D3D12RHISwapChainNative>(native), ref currentBuffer);
                                                cmds.Cmds.OMSetRenderTargets(1, &rtvHandle, false, (CpuDescriptorHandle*)null);

                                                _state.RasterState.DSVFormat = Format.FormatUnknown;

                                                _state.RasterState.RTVFormats.e0 = native->Base.Description.BackBufferFormat.ToRenderTargetFormat();
                                                _state.RasterState.RTVFormats.Count = 1;

                                                hasSwapChainRt = true;

                                                if (_activeSwapChains.Add(swapChain))
                                                    _awaitingPresents.Enqueue(swapChain);
                                            }

                                            break;
                                        }

                                    //Compute
                                    case RecCommandType.Dispatch:
                                        {
                                            CmdDispatch cmd = recorder.GetCommandAtOffset<CmdDispatch>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdDispatch>();

                                            _resourceManager.FlushPendingInits(ref cmds.Cmds.Get());
                                            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                            cmds.Cmds.Dispatch(cmd.ThreadGroupSizeX, cmd.ThreadGroupSizeY, cmd.ThreadGroupSizeZ);

                                            break;
                                        }

                                    // PIX
                                    case RecCommandType.BeginEvent:
                                        {
                                            CmdBeginEvent cmd = recorder.GetCommandAtOffset<CmdBeginEvent>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdBeginEvent>();

#if DEBUG
                                            // skip this command validation header
                                            byteOffset += 4;
#endif

                                            if (_hasPixAvailable)
                                            {
                                                PIXBeginEventOnCommandList((nint)Unsafe.AsPointer(ref cmds.Cmds.Get()), cmd.Color, (byte*)recorder.GetPointerAtOffset(byteOffset));
                                                ++pixEventDepth;
                                            }

                                            byteOffset += cmd.TextLength;
                                            break;
                                        }
                                    case RecCommandType.EndEvent:
                                        {
                                            // event is blank

                                            if (_hasPixAvailable && pixEventDepth > 0)
                                            {
                                                PIXEndEventOnCommandList((nint)Unsafe.AsPointer(ref cmds.Cmds.Get()));
                                                --pixEventDepth;
                                            }

                                            break;
                                        }
                                    case RecCommandType.MarkEvent:
                                        {
                                            CmdMarkEvent cmd = recorder.GetCommandAtOffset<CmdMarkEvent>(byteOffset);
                                            byteOffset += Unsafe.SizeOf<CmdMarkEvent>();

#if DEBUG
                                            // skip this command validation header
                                            byteOffset += 4;
#endif

                                            if (_hasPixAvailable)
                                            {
                                                PIXSetMarkerOnCommandList((nint)Unsafe.AsPointer(ref cmds.Cmds.Get()), cmd.Color, (byte*)recorder.GetPointerAtOffset(byteOffset));
                                                ++pixEventDepth;
                                            }

                                            byteOffset += cmd.TextLength;
                                            break;
                                        }

                                    default: throw new NotSupportedException($"Unknown command: {commandType}");
                                }

#if DEBUG
                                recorder.ValidateCommandAtOffset(byteOffset);
                                byteOffset += 4;
#endif
                            }
                        }

                        if (_hasPixAvailable)
                            PIXEndEventOnCommandList((nint)Unsafe.AsPointer(ref cmds.Cmds.Get()));
                    }
                }

                if (currentList.HasValue)
                {
                    if (lastEventType == TimelineEventType.Raster)
                        TransitionSwapChains(currentList.Value);
                    ExecuteCommandListData(GetListTypeForEvent(lastEventType), currentList.Value);
                }
            }

            Debug.Assert(_activeSwapChains.Count == 0);

            using (new ProfilingScope("Present"))
            {
                while (_awaitingPresents.TryDequeue(out RHISwapChain? @internal))
                {
                    @internal.Present();
                }

                d3d12.CompositionDevice?.CommitSurfaces();
            }

            using (new ProfilingScope("Signal"))
            {
                if ((_freeRunningQueues & 0x1) > 0)
                    _directFence.Signal(_graphicsQueue);
                if ((_freeRunningQueues & 0x2) > 0)
                    _computeFence.Signal(_computeQueue);
                if ((_freeRunningQueues & 0x4) > 0)
                    _copyFence.Signal(_copyQueue);
            }

            _drawCycle = !_drawCycle;
        }

        private void TransitionSwapChains(CmdListData cmds)
        {
            if (_activeSwapChains.Count == 0)
                return;

            foreach (D3D12RHISwapChain swapChain in _activeSwapChains)
            {
                D3D12RHISwapChainNative* native = (D3D12RHISwapChainNative*)swapChain.GetAsNative();

                int activeIndex = native->ActiveBufferIndex;//(int)swapChain.SwapChain.Get()->GetCurrentBackBufferIndex();
                ref D3D12RHISwapChainBuffer currentBuffer = ref native->Buffers[activeIndex];

                _barrierManager.AddTextureBarrier(ref currentBuffer.Resource.Get(), BarrierSync.Draw, BarrierAccess.Common, BarrierLayout.Present);

                currentBuffer.BarrierSync = BarrierSync.Draw;
                currentBuffer.BarrierAccess = BarrierAccess.Common;
                currentBuffer.BarrierLayout = BarrierLayout.Present;
            }

            _barrierManager.FlushBarriers(ref cmds.Cmds.Get(), BarrierFlushTypes.Texture);
            _activeSwapChains.Clear();
        }

        private void RestoreGraphicsCmdState(ID3D12GraphicsCommandList10* cmdList)
        {
            if (_state.RenderTargets.Count > 0 || _state.DepthStencil.Ptr != _dsvHeap.NullDescriptor.Ptr)
            {
                CpuDescriptorHandle dsvHandle = _state.DepthStencil;

                fixed (CpuDescriptorHandle* ptr = _state.RenderTargets.Span)
                {
                    cmdList->OMSetRenderTargets(
                        (uint)_state.RenderTargets.Count,
                        ptr,
                        false,
                        &dsvHandle);
                }
            }

            if (_state.Viewports.Count > 0)
            {
                fixed (Viewport* ptr = _state.Viewports.Span)
                {
                    cmdList->RSSetViewports((uint)_state.Viewports.Count, ptr);
                }
            }

            if (_state.Scissors.Count > 0)
            {
                fixed (Box2D<int>* ptr = _state.Scissors.Span)
                {
                    cmdList->RSSetScissorRects((uint)_state.Scissors.Count, ptr);
                }
            }
        }

        public NRDResourceInfo QueryResourceInfo(FrameGraphResource resource)
        {
            ResourceDesc1 desc = ResourceManager.GetResourceDescription(this, resource);
            ResourceAllocationInfo allocInfo = _device->GetResourceAllocationInfo2(0, 1, &desc, null);

            if (resource.ResourceId == FGResourceId.Buffer && Flags.HasFlag(resource.BufferDesc.Usage, FGBufferUsage.ConstantBuffer))
            {
                allocInfo.Alignment = 256;
                allocInfo.SizeInBytes = (ulong)((long)allocInfo.SizeInBytes + (-(long)allocInfo.SizeInBytes & 255));
            }

            return new NRDResourceInfo((int)allocInfo.SizeInBytes, (int)allocInfo.Alignment);
        }

        public NRDResourceInfo QueryBufferInfo(FrameGraphBuffer buffer, int offset, int size)
        {
            ResourceDesc1 desc = ResourceManager.GetBufferDescription(this, size);
            ResourceAllocationInfo allocInfo = _device->GetResourceAllocationInfo2(0, 1, &desc, null);

            if (Flags.HasFlag(buffer.Description.Usage, FGBufferUsage.ConstantBuffer))
            {
                allocInfo.Alignment = 256;
                allocInfo.SizeInBytes = (ulong)((long)allocInfo.SizeInBytes + (-(long)allocInfo.SizeInBytes & 255));
            }

            return new NRDResourceInfo((int)allocInfo.SizeInBytes, (int)allocInfo.Alignment);
        }

        public NRDResourceInfo QueryTextureInfo(FrameGraphTexture texture, int offset, int size)
        {
            ResourceDesc1 desc = ResourceManager.GetBufferDescription(this, size);
            ResourceAllocationInfo allocInfo = _device->GetResourceAllocationInfo2(0, 1, &desc, null);

            return new NRDResourceInfo((int)allocInfo.SizeInBytes, (int)allocInfo.Alignment);
        }

        private CmdListData GetCommandListData(CommandListType listType)
        {
            int idx = listType switch
            {
                CommandListType.Direct => 0,
                CommandListType.Copy => 1,
                CommandListType.Compute => 2,
                _ => throw new NotImplementedException(),
            };

            Queue<CmdListData> queue = _drawCycle ? _allocatorQueue1[idx] : _allocatorQueue2[idx];
            if (!queue.TryDequeue(out CmdListData data))
            {
                ComPtr<ID3D12GraphicsCommandList10> cmds = new ComPtr<ID3D12GraphicsCommandList10>();
                HResult hr = _device->CreateCommandList1(0, listType, CommandListFlags.None, out cmds);

                if (hr.IsFailure)
                {
                    _gd.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                ComPtr<ID3D12CommandAllocator> allocator = null;
                hr = _device->CreateCommandAllocator(listType, out allocator);

                if (hr.IsFailure)
                {
                    _gd.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                data = new CmdListData(cmds, allocator);
            }

            data.Allocator.Reset();
            data.Cmds.Reset(ref data.Allocator.Get(), null);

            //if (_hasPixAvailable)
            //    PIXBeginEventOnCommandList((nint)data.Cmds, 0xff808080, "NoName");

            return data;
        }

        private void ExecuteCommandListData(CommandListType listType, CmdListData data)
        {
            data.Cmds.Close();

            int idx = listType switch
            {
                CommandListType.Direct => 0,
                CommandListType.Copy => 1,
                CommandListType.Compute => 2,
                _ => throw new NotImplementedException(),
            };

            Queue<CmdListData> queue = !_drawCycle ? _allocatorQueue1[idx] : _allocatorQueue2[idx];
            queue.Enqueue(data);

            ID3D12GraphicsCommandList10* ptr = data.Cmds;
            switch (listType)
            {
                case CommandListType.Direct: _graphicsQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
                case CommandListType.Compute: _computeQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
                case CommandListType.Copy: _copyQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
            }
        }

        private static CommandListType GetListTypeForEvent(TimelineEventType queue) => queue switch
        {
            TimelineEventType.Raster => CommandListType.Direct,
            TimelineEventType.Compute => CommandListType.Compute,
            _ => throw new NotImplementedException(),
        };

        private static bool CheckForPIXBinaries()
        {
            return File.Exists("WinPixEventRuntime.dll") || File.Exists("runtimes/win-x64/native/WinPixEventRuntime.dll");
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

        private static readonly SamplerDesc s_defaultSamplerDesc = new SamplerDesc(new RHISamplerDescription());

        private readonly record struct CmdListData(ComPtr<ID3D12GraphicsCommandList10> Cmds, ComPtr<ID3D12CommandAllocator> Allocator) : IDisposable
        {
            public void Dispose()
            {
                Cmds.Dispose();
                Allocator.Dispose();
            }
        }

        private readonly record struct SetHeapBundle(Ptr<ID3D12DescriptorHeap> RTVHeap, Ptr<ID3D12DescriptorHeap> SamplerHeap);
    }
}
