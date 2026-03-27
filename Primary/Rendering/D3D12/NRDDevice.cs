using Arch.LowLevel;
using Primary.Assets;
using Primary.Common;
using Primary.Interop;
using Primary.Profiling;
using Primary.Rendering.NRD;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.Rendering.Structures;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static Primary.Interop.PIX;
using static TerraFX.Interop.DirectX.D3D_PRIMITIVE_TOPOLOGY;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_TYPE;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
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

            _rtvHeap = new CpuDescriptorHeap(this, 128, D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
            _dsvHeap = new CpuDescriptorHeap(this, 256, D3D12_DESCRIPTOR_HEAP_TYPE_DSV);

            _gpuHeap = new GpuDescriptorHeap(this, 2048, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
            _samplerHeap = new SamplerDescriptorHeap(this, 2048);

            _directFence = new QueueFence(this);
            _computeFence = new QueueFence(this);
            _copyFence = new QueueFence(this);

            _freeRunningQueues = 0;

            _awaitingPresents = new Queue<RHISwapChain>();

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
                    CmdListData listData = GetCommandListData(D3D12_COMMAND_LIST_TYPE_DIRECT);
                    d3d12.UploadPendingData(listData.Cmds);

                    ExecuteCommandListData(D3D12_COMMAND_LIST_TYPE_DIRECT, listData);
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

                        D3D12_COMMAND_LIST_TYPE listType = GetListTypeForEvent(eventType);
                        _freeRunningQueues |= (byte)(1 << (int)eventType);

                        if (lastEventType != eventType && currentList.HasValue)
                        {
                            if (previousPassIndex != -1)
                                _barrierManager.TransitionToCompatible(currentList.Value.Cmds, listType, recorder);

                            ExecuteCommandListData(GetListTypeForEvent(lastEventType), currentList.Value);

                            currentList = null;
                            lastEventType = eventType;
                        }

                        CmdListData cmds = currentList.HasValue ? currentList.Value : GetCommandListData(listType);
                        if (!currentList.HasValue)
                        {
                            {
                                SetHeapBundle bundle = new SetHeapBundle(_gpuHeap.GetActiveHeapOrCreateNew(), _samplerHeap.GetActiveHeapOrCreateNew());
                                cmds.Cmds->SetDescriptorHeaps(2, (ID3D12DescriptorHeap**)&bundle);
                            }

                            if (listType == D3D12_COMMAND_LIST_TYPE_DIRECT)
                                RestoreGraphicsCmdState(cmds.Cmds);
                        }

                        previousPassIndex = passIndex;
                        currentList = cmds;

                        int resourceByteOffsetBackup = -1;

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
                                            cmds.Cmds,
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
                                            cmds.Cmds,
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

                                        _barrierManager.AddBufferBarrier(sourceNative, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE);
                                        _barrierManager.AddBufferBarrier(destinationNative, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST);
                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Buffer);

                                        cmds.Cmds->CopyBufferRegion(destinationNative, cmd.DestinationOffset, sourceNative, cmd.SourceOffset, cmd.NumBytes);

                                        break;
                                    }
                                case RecCommandType.CopyTexture:
                                    {
                                        CmdCopyTexture cmd = recorder.GetCommandAtOffset<CmdCopyTexture>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdCopyTexture>();

                                        NRDResource sourceResource = ResourceUtility.AsNRDResource(cmd.Source.Resource);
                                        ID3D12Resource* sourceNative = (ID3D12Resource*)_resourceManager.GetResource(sourceResource);

                                        D3D12_PLACED_SUBRESOURCE_FOOTPRINT sourcePlacedFootprint;
                                        D3D12_TEXTURE_COPY_LOCATION sourceCopyLocation;

                                        NRDResource destinationResource = ResourceUtility.AsNRDResource(cmd.Destination.Resource);
                                        ID3D12Resource* destinationNative = (ID3D12Resource*)_resourceManager.GetResource(destinationResource);

                                        D3D12_PLACED_SUBRESOURCE_FOOTPRINT destinationPlacedFootprint;
                                        D3D12_TEXTURE_COPY_LOCATION destinationCopyLocation;

                                        if (cmd.Source.Type == CmdDataTextureSourceType.SubresourceIndex)
                                        {
                                            sourceCopyLocation = new D3D12_TEXTURE_COPY_LOCATION(sourceNative, cmd.Source.SubresourceIndex);
                                        }
                                        else
                                        {
                                            ref CmdDataTextureFootprint footprint = ref cmd.Source.Footprint;
                                            sourcePlacedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
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
                                            sourceCopyLocation = new D3D12_TEXTURE_COPY_LOCATION(sourceNative, &sourcePlacedFootprint);
                                        }

                                        if (cmd.Destination.Type == CmdDataTextureSourceType.SubresourceIndex)
                                        {
                                            destinationCopyLocation = new D3D12_TEXTURE_COPY_LOCATION(destinationNative, cmd.Destination.SubresourceIndex);
                                        }
                                        else
                                        {
                                            ref CmdDataTextureFootprint footprint = ref cmd.Destination.Footprint;
                                            destinationPlacedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
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
                                            destinationCopyLocation = new D3D12_TEXTURE_COPY_LOCATION(destinationNative, &destinationPlacedFootprint);
                                        }

                                        D3D12_BOX box = default;
                                        if (cmd.SourceBox.HasValue)
                                        {
                                            FGBox val = cmd.SourceBox.Value;
                                            box = new D3D12_BOX(val.X, val.Y, val.Z, val.Width, val.Height, val.Depth);
                                        }

                                        if (sourceResource.Id == NRDResourceId.Buffer)
                                            _barrierManager.AddBufferBarrier(sourceNative, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE);
                                        else
                                            _barrierManager.AddTextureBarrier(sourceNative, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE, D3D12_BARRIER_LAYOUT_COPY_SOURCE);

                                        if (destinationResource.Id == NRDResourceId.Buffer)
                                            _barrierManager.AddBufferBarrier(destinationResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST);
                                        else
                                            _barrierManager.AddTextureBarrier(destinationResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST);

                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                        cmds.Cmds->CopyTextureRegion(&destinationCopyLocation, cmd.DstX, cmd.DstY, cmd.DstZ, &sourceCopyLocation, cmd.SourceBox.HasValue ? &box : null);

                                        break;
                                    }
                                case RecCommandType.SetPipeline:
                                    {
                                        CmdSetPipeline cmd = recorder.GetCommandAtOffset<CmdSetPipeline>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdSetPipeline>();

                                        if (listType == D3D12_COMMAND_LIST_TYPE_DIRECT)
                                        {
                                            D3D12RHIGraphicsPipeline pipeline = Unsafe.As<D3D12RHIGraphicsPipeline>(resources.GetPipelineFromIndex(cmd.Index))!;

                                            cmds.Cmds->SetPipelineState(pipeline.GetPipelineState(_state.RasterState));
                                            cmds.Cmds->SetGraphicsRootSignature(pipeline.RootSignature.Get());

                                            cmds.Cmds->IASetPrimitiveTopology(pipeline.Description.PrimitiveTopologyType switch
                                            {
                                                RHIPrimitiveTopologyType.Triangle => D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST,
                                                RHIPrimitiveTopologyType.Line => D3D_PRIMITIVE_TOPOLOGY_LINELIST,
                                                RHIPrimitiveTopologyType.Point => D3D_PRIMITIVE_TOPOLOGY_POINTLIST,
                                                _ => throw new NotImplementedException(),
                                            });
                                        }
                                        else if (listType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
                                        {
                                            D3D12RHIComputePipeline pipeline = Unsafe.As<D3D12RHIComputePipeline>(resources.GetPipelineFromIndex(cmd.Index))!;

                                            cmds.Cmds->SetPipelineState(pipeline.PipelineState.Get());
                                            cmds.Cmds->SetComputeRootSignature(pipeline.RootSignature.Get());
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
                                                uint index = _gpuHeap.GetDescriptorIndex(resource, Flags.HasFlag(cmd.Flags, ShPropertyFlags.ReadWrite), out bool changedActiveHeap);
                                                if (changedActiveHeap)
                                                {
                                                    Debug.Assert(resourceByteOffsetBackup != -1);

                                                    byteOffset = resourceByteOffsetBackup;
                                                    break;
                                                }

                                                if (resource.Id == NRDResourceId.Buffer)
                                                {
                                                    BarrierManager.GetShaderBufferBarriers(resource, _resourceManager, cmd.Stages, cmd.Flags, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access);
                                                    _barrierManager.AddBufferBarrier(resource, sync, access);
                                                }
                                                else
                                                {
                                                    BarrierManager.GetShaderTextureBarriers(resource, _resourceManager, cmd.Stages, cmd.Flags, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout);
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
                                        if (listType == D3D12_COMMAND_LIST_TYPE_DIRECT)
                                        {
                                            if (Flags.HasFlag(_state.HeaderFlags, ShHeaderFlags.HeaderIsBuffer))
                                            {
                                                if (_state.ConstantsSize > 0)
                                                    cmds.Cmds->SetGraphicsRoot32BitConstants(0, (uint)(_state.ConstantsSize / sizeof(uint)), _state.ConstantsData.ToPointer(), 0);
                                                if (_state.ResourceSize > 0)
                                                    throw new NotImplementedException();
                                            }
                                            else
                                            {
                                                cmds.Cmds->SetGraphicsRoot32BitConstants(0, (uint)(_state.ResourceSize / sizeof(uint)), _state.ResourceData.ToPointer(), 0);
                                            }
                                        }
                                        else if (listType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
                                        {
                                            if (Flags.HasFlag(_state.HeaderFlags, ShHeaderFlags.HeaderIsBuffer))
                                            {
                                                if (_state.ConstantsSize > 0)
                                                    cmds.Cmds->SetComputeRoot32BitConstants(0, (uint)(_state.ConstantsSize / sizeof(uint)), _state.ConstantsData.ToPointer(), 0);
                                                if (_state.ResourceSize > 0)
                                                    throw new NotImplementedException();
                                            }
                                            else
                                            {
                                                cmds.Cmds->SetComputeRoot32BitConstants(0, (uint)(_state.ResourceSize / sizeof(uint)), _state.ResourceData.ToPointer(), 0);
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
                                            _state.RasterState.RTVFormats[cmd.Slot] = DXGI_FORMAT_UNKNOWN;
                                        }
                                        else
                                        {
                                            NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);
                                            _state.RenderTargets[cmd.Slot] = _rtvHeap.GetDescriptorHandle(resource);
                                            _state.RasterState.RTVFormats[cmd.Slot] = ResourceUtility.GetTextureFormat(resource, _resourceManager).ToRenderTargetFormat();

                                            _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
                                            _resourceManager.EnsureInitialized(resource);
                                        }

                                        break;
                                    }
                                case RecCommandType.CommitRenderTargets:
                                    {
                                        CmdCommitRenderTargets cmd = recorder.GetCommandAtOffset<CmdCommitRenderTargets>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdCommitRenderTargets>();

                                        _state.RenderTargets.Count = cmd.ActiveCount;
                                        _state.RasterState.RTVFormats.Count = cmd.ActiveCount;

                                        if (!cmd.DeferSetState)
                                        {
                                            D3D12_CPU_DESCRIPTOR_HANDLE dsvHandle = _state.DepthStencil;

                                            _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Texture);
                                            fixed (D3D12_CPU_DESCRIPTOR_HANDLE* ptr = _state.RenderTargets.Span)
                                            {
                                                cmds.Cmds->OMSetRenderTargets(
                                                    (uint)_state.RenderTargets.Count,
                                                    ptr,
                                                    false,
                                                    &dsvHandle);
                                            }
                                        }

                                        break;
                                    }
                                case RecCommandType.SetDepthStencil:
                                    {
                                        CmdSetDepthStencil cmd = recorder.GetCommandAtOffset<CmdSetDepthStencil>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdSetDepthStencil>();

                                        NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);
                                        _state.DepthStencil = _dsvHeap.GetDescriptorHandle(resource);
                                        _state.RasterState.DSVFormat = ResourceUtility.GetTextureFormat(resource, _resourceManager).ToDepthStencilFormat();
                                        //TODO: Add fallback for no depth and stencil writes to *_READ instead of always *_WRITE
                                        _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
                                        _resourceManager.EnsureInitialized(resource);

                                        D3D12_CPU_DESCRIPTOR_HANDLE dsvHandle = _state.DepthStencil;

                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Texture);
                                        fixed (D3D12_CPU_DESCRIPTOR_HANDLE* ptr = _state.RenderTargets.Span)
                                        {
                                            cmds.Cmds->OMSetRenderTargets(
                                                (uint)_state.RenderTargets.Count,
                                                ptr,
                                                false,
                                                &dsvHandle);
                                        }
                                        break;
                                    }
                                case RecCommandType.ClearRenderTarget:
                                    {
                                        CmdClearRenderTarget cmd = recorder.GetCommandAtOffset<CmdClearRenderTarget>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdClearRenderTarget>();

                                        NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);

                                        _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Texture);

                                        RECT rect = default;
                                        if (cmd.Rect.HasValue)
                                        {
                                            FGRect val = cmd.Rect.Value;
                                            rect = new RECT(val.Left, val.Top, val.Right, val.Bottom);
                                        }

                                        if (cmd.Color.HasValue)
                                        {
                                            Color color = cmd.Color.Value;
                                            cmds.Cmds->ClearRenderTargetView(_rtvHeap.GetDescriptorHandle(resource), (float*)&color, cmd.Rect.HasValue ? 1u : 0, &rect);
                                        }
                                        else
                                        {
                                            Color color = new Color(0.0f);
                                            cmds.Cmds->ClearRenderTargetView(_rtvHeap.GetDescriptorHandle(resource), (float*)&color, cmd.Rect.HasValue ? 1u : 0, &rect);
                                        }

                                        _resourceManager.SetAsInitialized(resource);

                                        break;
                                    }
                                case RecCommandType.ClearDepthStencil:
                                    {
                                        CmdClearDepthStencil cmd = recorder.GetCommandAtOffset<CmdClearDepthStencil>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdClearDepthStencil>();

                                        NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);

                                        _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Texture);

                                        RECT rect = default;
                                        if (cmd.Rect.HasValue)
                                        {
                                            FGRect val = cmd.Rect.Value;
                                            rect = new RECT(val.Left, val.Top, val.Right, val.Bottom);
                                        }

                                        D3D12_CLEAR_FLAGS flags = (D3D12_CLEAR_FLAGS)cmd.ClearFlags;

                                        float depthVal = cmd.Depth.GetValueOrDefault(1.0f);
                                        byte stencilVal = cmd.Stencil.GetValueOrDefault(0xff);

                                        cmds.Cmds->ClearDepthStencilView(_dsvHeap.GetDescriptorHandle(resource), flags, depthVal, stencilVal, cmd.Rect.HasValue ? 1u : 0, &rect);

                                        _resourceManager.SetAsInitialized(resource);

                                        break;
                                    }
                                case RecCommandType.SetViewport:
                                    {
                                        CmdSetViewport cmd = recorder.GetCommandAtOffset<CmdSetViewport>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdSetViewport>();

                                        _state.Viewports[cmd.Slot] = new D3D12_VIEWPORT(cmd.Viewport.TopLeftX, cmd.Viewport.TopLeftY, cmd.Viewport.Width, cmd.Viewport.Height, cmd.Viewport.MinDepth, cmd.Viewport.MaxDepth);

                                        break;
                                    }
                                case RecCommandType.CommitViewports:
                                    {
                                        CmdCommitViewports cmd = recorder.GetCommandAtOffset<CmdCommitViewports>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdCommitViewports>();

                                        _state.Viewports.Count = cmd.ActiveCount;
                                        fixed (D3D12_VIEWPORT* ptr = _state.Viewports.Span)
                                        {
                                            cmds.Cmds->RSSetViewports(cmd.ActiveCount, ptr);
                                        }
                                        break;
                                    }
                                case RecCommandType.SetScissor:
                                    {
                                        CmdSetScissor cmd = recorder.GetCommandAtOffset<CmdSetScissor>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdSetScissor>();

                                        _state.Scissors[cmd.Slot] = new RECT(cmd.Scissor.Left, cmd.Scissor.Top, cmd.Scissor.Right, cmd.Scissor.Bottom);

                                        break;
                                    }
                                case RecCommandType.CommitScissors:
                                    {
                                        CmdCommitScissors cmd = recorder.GetCommandAtOffset<CmdCommitScissors>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdCommitScissors>();

                                        _state.Scissors.Count = cmd.ActiveCount;
                                        fixed (RECT* ptr = _state.Scissors.Span)
                                        {
                                            cmds.Cmds->RSSetScissorRects(cmd.ActiveCount, ptr);
                                        }
                                        break;
                                    }
                                case RecCommandType.SetStencilReference:
                                    {
                                        CmdSetStencilRef cmd = recorder.GetCommandAtOffset<CmdSetStencilRef>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdSetStencilRef>();

                                        cmds.Cmds->OMSetStencilRef(cmd.StencilRef);

                                        break;
                                    }
                                case RecCommandType.SetVertexBuffer:
                                    {
                                        CmdSetVertexBuffer cmd = recorder.GetCommandAtOffset<CmdSetVertexBuffer>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdSetVertexBuffer>();

                                        if (cmd.Resource.IsNull)
                                        {
                                            cmds.Cmds->IASetVertexBuffers(0, 0, null);

                                        }
                                        else
                                        {
                                            NRDResource resource = ResourceUtility.AsNRDResource(cmd.Resource);
                                            ID3D12Resource* native = (ID3D12Resource*)_resourceManager.GetResource(resource);

                                            D3D12_VERTEX_BUFFER_VIEW vbv = new D3D12_VERTEX_BUFFER_VIEW
                                            {
                                                BufferLocation = native->GetGPUVirtualAddress(),
                                                SizeInBytes = cmd.BufferSize,
                                                StrideInBytes = cmd.Stride,
                                            };

                                            cmds.Cmds->IASetVertexBuffers(0, 1, &vbv);
                                        }

                                        break;
                                    }
                                case RecCommandType.SetIndexBuffer:
                                    {
                                        CmdSetIndexBuffer cmd = recorder.GetCommandAtOffset<CmdSetIndexBuffer>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdSetIndexBuffer>();

                                        if (cmd.Resource.IsNull)
                                        {
                                            cmds.Cmds->IASetIndexBuffer(null);
                                        }
                                        else
                                        {
                                            NRDResource resource = ResourceUtility.AsNRDResource(cmd.Resource);
                                            ID3D12Resource* native = (ID3D12Resource*)_resourceManager.GetResource(resource);

                                            D3D12_INDEX_BUFFER_VIEW ibv = new D3D12_INDEX_BUFFER_VIEW
                                            {
                                                BufferLocation = native->GetGPUVirtualAddress(),
                                                SizeInBytes = cmd.BufferSize,
                                                Format = cmd.Stride switch
                                                {
                                                    2 => DXGI_FORMAT_R16_UINT,
                                                    4 => DXGI_FORMAT_R32_UINT,
                                                    _ => throw new NotImplementedException(),
                                                }
                                            };

                                            cmds.Cmds->IASetIndexBuffer(&ibv);
                                        }

                                        break;
                                    }
                                case RecCommandType.DrawInstanced:
                                    {
                                        CmdDrawInstanced cmd = recorder.GetCommandAtOffset<CmdDrawInstanced>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdDrawInstanced>();

                                        _resourceManager.FlushPendingInits(cmds.Cmds);
                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                        cmds.Cmds->DrawInstanced(cmd.VertexCount, cmd.InstanceCount, cmd.StartVertex, cmd.StartInstance);

                                        break;
                                    }
                                case RecCommandType.DrawIndexedInstanced:
                                    {
                                        CmdDrawIndexedInstanced cmd = recorder.GetCommandAtOffset<CmdDrawIndexedInstanced>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdDrawIndexedInstanced>();

                                        _resourceManager.FlushPendingInits(cmds.Cmds);
                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                        cmds.Cmds->DrawIndexedInstanced(cmd.IndexCount, cmd.InstanceCount, cmd.StartIndex, cmd.BaseVertex, cmd.StartInstance);

                                        break;
                                    }
                                case RecCommandType.PresentOnWindow:
                                    {
                                        CmdPresentOnWindow cmd = recorder.GetCommandAtOffset<CmdPresentOnWindow>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdPresentOnWindow>();

                                        Window? window = WindowManager.Instance.FindWindow((SDL.SDL_WindowID)cmd.WindowId);
                                        if (window != null)
                                        {
                                            D3D12RHISwapChain swapChain = Unsafe.As<D3D12RHISwapChain>(_manager.SwapChainCache.GetForWindow(window));
                                            D3D12RHISwapChainNative* native = (D3D12RHISwapChainNative*)swapChain.GetAsNative();

                                            int activeIndex = native->ActiveBufferIndex;//(int)swapChain.SwapChain.Get()->GetCurrentBackBufferIndex();

                                            ref D3D12RHISwapChainBuffer currentBuffer = ref native->Buffers[activeIndex];
                                            NRDResource resource = ResourceUtility.AsNRDResource(cmd.Texture);

                                            _barrierManager.SetResourceState((ID3D12Resource*)currentBuffer.Resource.Get(), new NRDResourceState(FGResourceId.Texture, null, currentBuffer.BarrierSync, currentBuffer.BarrierAccess, currentBuffer.BarrierLayout));

                                            _barrierManager.AddTextureBarrier((ID3D12Resource*)currentBuffer.Resource.Get(), D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST);
                                            _barrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE, D3D12_BARRIER_LAYOUT_COPY_SOURCE);
                                            _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Texture);

                                            D3D12_TEXTURE_COPY_LOCATION dst = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)currentBuffer.Resource.Get());
                                            D3D12_TEXTURE_COPY_LOCATION src = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)resource.GetNativeResource(_resourceManager));

                                            cmds.Cmds->CopyTextureRegion(&dst, 0, 0, 0, &src, null);

                                            _barrierManager.AddTextureBarrier((ID3D12Resource*)currentBuffer.Resource.Get(), D3D12_BARRIER_SYNC_DRAW, D3D12_BARRIER_ACCESS_COMMON, D3D12_BARRIER_LAYOUT_PRESENT);
                                            _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Texture);

                                            _awaitingPresents.Enqueue(swapChain);

                                            currentBuffer.BarrierSync = D3D12_BARRIER_SYNC_DRAW;
                                            currentBuffer.BarrierAccess = D3D12_BARRIER_ACCESS_COMMON;
                                            currentBuffer.BarrierLayout = D3D12_BARRIER_LAYOUT_PRESENT;
                                        }

                                        break;
                                    }

                                //Compute
                                case RecCommandType.Dispatch:
                                    {
                                        CmdDispatch cmd = recorder.GetCommandAtOffset<CmdDispatch>(byteOffset);
                                        byteOffset += Unsafe.SizeOf<CmdDispatch>();

                                        _resourceManager.FlushPendingInits(cmds.Cmds);
                                        _barrierManager.FlushBarriers(cmds.Cmds, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                                        cmds.Cmds->Dispatch(cmd.ThreadGroupSizeX, cmd.ThreadGroupSizeY, cmd.ThreadGroupSizeZ);

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
                }

                if (currentList.HasValue)
                {
                    ExecuteCommandListData(GetListTypeForEvent(lastEventType), currentList.Value);
                }
            }

            using (new ProfilingScope("Present"))
            {
                while (_awaitingPresents.TryDequeue(out RHISwapChain? @internal))
                {
                    @internal.Present();
                }
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

        private void RestoreGraphicsCmdState(ID3D12GraphicsCommandList10* cmdList)
        {
            if (_state.RenderTargets.Count > 0 || _state.DepthStencil != _dsvHeap.NullDescriptor)
            {
                D3D12_CPU_DESCRIPTOR_HANDLE dsvHandle = _state.DepthStencil;

                fixed (D3D12_CPU_DESCRIPTOR_HANDLE* ptr = _state.RenderTargets.Span)
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
                fixed (D3D12_VIEWPORT* ptr = _state.Viewports.Span)
                {
                    cmdList->RSSetViewports((uint)_state.Viewports.Count, ptr);
                }
            }

            if (_state.Scissors.Count > 0)
            {
                fixed (RECT* ptr = _state.Scissors.Span)
                {
                    cmdList->RSSetScissorRects((uint)_state.Scissors.Count, ptr);
                }
            }
        }

        public NRDResourceInfo QueryResourceInfo(FrameGraphResource resource)
        {
            D3D12_RESOURCE_DESC1 desc = ResourceManager.GetResourceDescription(resource);
            D3D12_RESOURCE_ALLOCATION_INFO allocInfo = _device->GetResourceAllocationInfo2(0, 1, &desc, null);

            if (resource.ResourceId == FGResourceId.Buffer && Flags.HasFlag(resource.BufferDesc.Usage, FGBufferUsage.ConstantBuffer))
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

            if (Flags.HasFlag(buffer.Description.Usage, FGBufferUsage.ConstantBuffer))
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
                _ => throw new NotImplementedException(),
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

            data.Allocator->Reset();
            data.Cmds->Reset(data.Allocator, null);

            if (_hasPixAvailable)
                PIXBeginEventOnCommandList((nint)data.Cmds, 0xff808080, "NoName");

            return data;
        }

        private void ExecuteCommandListData(D3D12_COMMAND_LIST_TYPE listType, CmdListData data)
        {
            if (_hasPixAvailable)
                PIXEndEventOnCommandList((nint)data.Cmds);

            data.Cmds->Close();

            int idx = listType switch
            {
                D3D12_COMMAND_LIST_TYPE_DIRECT => 0,
                D3D12_COMMAND_LIST_TYPE_COPY => 1,
                D3D12_COMMAND_LIST_TYPE_COMPUTE => 2,
            };

            Queue<CmdListData> queue = !_drawCycle ? _allocatorQueue1[idx] : _allocatorQueue2[idx];
            queue.Enqueue(data);

            ID3D12GraphicsCommandList10* ptr = data.Cmds;
            switch (listType)
            {
                case D3D12_COMMAND_LIST_TYPE_DIRECT: _graphicsQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
                case D3D12_COMMAND_LIST_TYPE_COMPUTE: _computeQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
                case D3D12_COMMAND_LIST_TYPE_COPY: _copyQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&ptr); break;
            }
        }

        private static D3D12_COMMAND_LIST_TYPE GetListTypeForEvent(TimelineEventType queue) => queue switch
        {
            TimelineEventType.Raster => D3D12_COMMAND_LIST_TYPE_DIRECT,
            TimelineEventType.Compute => D3D12_COMMAND_LIST_TYPE_COMPUTE,
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

        private readonly record struct CmdListData(Ptr<ID3D12GraphicsCommandList10> CmdListPtr, Ptr<ID3D12CommandAllocator> AllocatorPtr) : IDisposable
        {
            public void Dispose()
            {
                CmdListPtr.Pointer->Release();
                AllocatorPtr.Pointer->Release();
            }

            internal ID3D12GraphicsCommandList10* Cmds => CmdListPtr.Pointer;
            internal ID3D12CommandAllocator* Allocator => AllocatorPtr.Pointer;
        }

        private readonly record struct SetHeapBundle(Ptr<ID3D12DescriptorHeap> RTVHeap, Ptr<ID3D12DescriptorHeap> SamplerHeap);
    }
}
