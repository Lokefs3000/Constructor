using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using NativeCollections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Interop;
using Primary.Mathematics;
using Primary.Memory;
using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Passes;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Primary.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Playback.D3D12
{
    internal unsafe sealed class PlaybackExecutor : IDisposable, IJob
    {
        private readonly PlaybackDevice _device;

        private CompiledRenderPlan? _renderPlan;
        private JobHandle _handle;

        private readonly PlaybackResourceManager _resourceManager;
        private readonly PlaybackUploadManager _uploadManager;
        private readonly PlaybackDescriptorAllocator _directDescriptorAllocator;
        private readonly PlaybackDescriptorAllocator _computeDescriptorAllocator;
        private readonly PlaybackBarrierBatch _barrierBatch;
        private readonly PlaybackCommandPool _commandPool;

        private readonly NativeArray<byte> _constantsDataArray;
        private NativeArray<byte> _resourcesDataArray;

        private UnsafeList<Ptr<ID3D12GraphicsCommandList10>> _pendingDirectCommandLists;
        private UnsafeList<Ptr<ID3D12GraphicsCommandList10>> _pendingComputeCommandLists;

        private readonly Ptr<ID3D12Fence1> _directFence;
        private readonly Ptr<ID3D12Fence1> _computeFence;
        private readonly ManualResetEventSlim _fenceWaitEvent;

        private ulong _activeFenceValue;
        private RenderPassType _activeFences;

        private bool _hasPixAvailable;

        private bool _disposedValue;

        internal PlaybackExecutor(PlaybackDevice device)
        {
            _device = device;

            _renderPlan = null;
            _handle = JobHandle.Completed;

            _resourceManager = new PlaybackResourceManager(device, this);
            _uploadManager = new PlaybackUploadManager(device, this);
            _directDescriptorAllocator = new PlaybackDescriptorAllocator(device, this);
            _computeDescriptorAllocator = new PlaybackDescriptorAllocator(device, this);
            _barrierBatch = new PlaybackBarrierBatch(device, this);
            _commandPool = new PlaybackCommandPool(device, this);

            _constantsDataArray = new NativeArray<byte>(128);
            _resourcesDataArray = NativeArray<byte>.Empty;

            _pendingDirectCommandLists = new UnsafeList<Ptr<ID3D12GraphicsCommandList10>>(4);
            _pendingComputeCommandLists = new UnsafeList<Ptr<ID3D12GraphicsCommandList10>>(4);

            CreateFence(ref _directFence);
            CreateFence(ref _computeFence);
            _fenceWaitEvent = new ManualResetEventSlim(false);

            _activeFenceValue = 0;
            _activeFences = RenderPassType.None;

            _hasPixAvailable = File.Exists("WinPixEventRuntime.dll") || File.Exists("runtimes/win-x64/native/WinPixEventRuntime.dll");

            void CreateFence(ref Ptr<ID3D12Fence1> ptr)
            {
                ID3D12Fence1* fence = null;
                HResult hr = _device.NativeDevice.Device.CreateFence(0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence1>(), (void**)&fence);

                if (hr.IsFailure)
                {
                    EngLog.Render.Fatal("Failed to fence to wait on command queues with");
                    EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                    _device.NativeDevice.FlushPendingMessages();
                    EngLog.ThrowFatal();
                }

                ptr = fence;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_activeFences != RenderPassType.None)
                    {
                        InlineArray2<Ptr<ID3D12Fence1>> fenceArray = default;
                        InlineArray2<ulong> fenceValueArray = default;

                        int arrayIndex = 0;

                        if (_activeFences.HasFlags(RenderPassType.Raster))
                        {
                            fenceArray[arrayIndex] = _directFence;
                            fenceValueArray[arrayIndex] = _activeFenceValue;

                            ++arrayIndex;
                        }

                        if (_activeFences.HasFlags(RenderPassType.Compute))
                        {
                            fenceArray[arrayIndex] = _computeFence;
                            fenceValueArray[arrayIndex] = _activeFenceValue;

                            ++arrayIndex;
                        }

                        if (arrayIndex > 0)
                        {
                            HResult hr = _device.NativeDevice.Device.SetEventOnMultipleFenceCompletion(
                                (ID3D12Fence**)&fenceArray[0],
                                &fenceValueArray[0],
                                (uint)arrayIndex,
                                MultipleFenceWaitFlags.All,
                                null);
                        }
                    }

                    if (_constantsDataArray.IsCreated)
                        _constantsDataArray.Dispose();
                    if (_resourcesDataArray.IsCreated)
                        _resourcesDataArray.Dispose();

                    if (!_directFence.IsNull)
                        _directFence.Pointer->Release();
                    if (!_computeFence.IsNull)
                        _computeFence.Pointer->Release();

                    _fenceWaitEvent.Dispose();

                    _pendingDirectCommandLists.Dispose();
                    _pendingComputeCommandLists.Dispose();

                    _commandPool.Dispose();
                    _computeDescriptorAllocator.Dispose();
                    _directDescriptorAllocator.Dispose();
                    _uploadManager.Dispose();
                    _resourceManager.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void GiveWorkData(CompiledRenderPlan renderPlan, JobHandle jobHandle)
        {
            Debug.Assert(_renderPlan == null && _handle.HasCompleted);

            _renderPlan = renderPlan;
            _handle = jobHandle;
        }

        void IJob.Execute()
        {
            JobHandle monolithic;
            using (new ProfilingScope("ExecutePlayback"))
            {
                if (_renderPlan == null)
                {
                    EngLog.Render.Error("Playback executor scheduled and flushed to threadpool without render plan");

                    ReturnToDevice();
                    return;
                }

                _barrierBatch.AssertIsFlushed();

                ID3D12Device14* device = *_device.NativeDevice.Device.GetAddressOf();

                if (_activeFences != RenderPassType.None)
                {
                    using (new ProfilingScope("WaitOnFences"))
                    {
                        InlineArray2<Ptr<ID3D12Fence1>> fenceArray = default;
                        InlineArray2<ulong> fenceValueArray = default;

                        int arrayIndex = 0;

                        if (_activeFences.HasFlags(RenderPassType.Raster))
                        {
                            fenceArray[arrayIndex] = _directFence;
                            fenceValueArray[arrayIndex] = _activeFenceValue;

                            ++arrayIndex;
                        }

                        if (_activeFences.HasFlags(RenderPassType.Compute))
                        {
                            fenceArray[arrayIndex] = _computeFence;
                            fenceValueArray[arrayIndex] = _activeFenceValue;

                            ++arrayIndex;
                        }

                        if (arrayIndex > 0)
                        {
                            HResult hr = device->SetEventOnMultipleFenceCompletion(
                                (ID3D12Fence**)&fenceArray[0],
                                &fenceValueArray[0],
                                (uint)arrayIndex,
                                MultipleFenceWaitFlags.All,
                                null);

                            if (hr.IsFailure)
                            {
                                EngLog.Render.Fatal("Failed to set event on fences");
                                EngLog.Render.Fatal("    Active fences: {v}", _activeFences);
                                EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                                _device.NativeDevice.FlushPendingMessages();
                                EngLog.ThrowFatal();
                            }
                        }

                        _activeFences = RenderPassType.None;
                    }
                }

                using (new ProfilingScope("FlushJobs"))
                {
                    _resourceManager.SetJobArguments(_renderPlan);
                    _uploadManager.SetJobArguments(_renderPlan);
                    _directDescriptorAllocator.SetJobArguments(_renderPlan, _renderPlan.DirectDescriptorAllocator);
                    _computeDescriptorAllocator.SetJobArguments(_renderPlan, _renderPlan.ComputeDescriptorAllocator);

                    JobHandle resourceAllocJob = JobScheduler.Schedule(_resourceManager, JobPriority.Realtime);
                    JobScheduler.Flush(resourceAllocJob);

                    JobHandle uploadHandleJob = JobScheduler.Schedule(_uploadManager, JobPriority.Realtime);
                    JobScheduler.Flush(uploadHandleJob);

                    JobHandle directDescriptorAllocJob = JobScheduler.Schedule(_directDescriptorAllocator, resourceAllocJob, JobPriority.Realtime);
                    JobHandle computeDescriptorAllocJob = JobScheduler.Schedule(_computeDescriptorAllocator, resourceAllocJob, JobPriority.Realtime);

                    using RentedArray<JobHandle> jobArray = new RentedArray<JobHandle>(3);
                    jobArray[0] = directDescriptorAllocJob;
                    jobArray[1] = computeDescriptorAllocJob;
                    jobArray[2] = uploadHandleJob;

                    monolithic = JobScheduler.CombineAll(jobArray.Span);
                }

                _pendingDirectCommandLists.Clear();
                _pendingComputeCommandLists.Clear();

                _commandPool.ClearForNewRender();

                using (new ProfilingScope("UpdateRHI"))
                {
                    _device.NativeDevice.HandlePendingUpdates();

                    if (_device.NativeDevice.HasPendingUploads)
                    {
                        ID3D12GraphicsCommandList10* commandList = _commandPool.GetDirectCommandList();
                        _device.NativeDevice.UploadPendingData(ref Unsafe.AsRef<ID3D12GraphicsCommandList10>(commandList));

                        HResult hr = commandList->Close();
                        if (hr.IsFailure)
                        {
                            EngLog.Render.Fatal("Failed to close device upload command list");
                            EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                            _device.NativeDevice.FlushPendingMessages();
                            EngLog.ThrowFatal();
                        }

                        _pendingDirectCommandLists.Add(commandList);
                    }
                }

                monolithic.WaitForCompletion();

                if (monolithic.IsAbort)
                {
                    EngLog.Render.Fatal("Exception was raised during playback");

                    _device.NativeDevice.FlushPendingMessages();
                    EngLog.ThrowFatal();
                }

                if (_renderPlan.WarmupBarriers.Count > 0)
                {
                    throw new NotImplementedException();
                }

                using (new ProfilingScope("GenerateCmds"))
                {
                    ROList<RenderEventInfo> events = _renderPlan.Events;
                    for (int i = 0; i < events.Count; ++i)
                    {
                        RenderEventInfo eventInfo = events[i];
                        switch (eventInfo.EventType)
                        {
                            case RenderEventType.ExecuteDirect:
                                {
                                    CompiledRenderPass renderPass = eventInfo.RenderPass1!;
                                    string passName = renderPass.RenderPass!.Name;

                                    using (new ProfilingScope(passName))
                                    {
                                        ID3D12GraphicsCommandList10* commandList = _commandPool.GetDirectCommandList();

                                        if (_hasPixAvailable)
                                        {
                                            uint nameColor = (uint)(passName.GetDjb2HashCode() | 0xff000000);
                                            PIX.PIXBeginEventOnCommandList((nint)commandList, nameColor, passName);
                                        }

                                        GenerateCommandList(renderPass, i, commandList, _directDescriptorAllocator);

                                        if (_hasPixAvailable)
                                        {
                                            PIX.PIXEndEventOnCommandList((nint)commandList);
                                        }

                                        HResult hr = commandList->Close();
                                        if (hr.IsFailure)
                                        {
                                            EngLog.Render.Fatal("Failed to close direct command list ({p})", passName);
                                            EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                                            _device.NativeDevice.FlushPendingMessages();
                                            EngLog.ThrowFatal();
                                        }

                                        _pendingDirectCommandLists.Add(commandList);
                                    }

                                    _activeFences |= RenderPassType.Raster;
                                    break;
                                }
                            case RenderEventType.ExecuteCompute:
                                {
                                    CompiledRenderPass renderPass = eventInfo.RenderPass1!;
                                    string passName = renderPass.RenderPass!.Name;

                                    using (new ProfilingScope(passName))
                                    {
                                        ID3D12GraphicsCommandList10* commandList = _commandPool.GetComputeCommandList();

                                        if (_hasPixAvailable)
                                        {
                                            uint nameColor = (uint)(passName.GetDjb2HashCode() | 0xff000000);
                                            PIX.PIXBeginEventOnCommandList((nint)commandList, nameColor, passName);
                                        }

                                        GenerateCommandList(renderPass, i, commandList, _computeDescriptorAllocator);

                                        if (_hasPixAvailable)
                                        {
                                            PIX.PIXEndEventOnCommandList((nint)commandList);
                                        }

                                        HResult hr = commandList->Close();
                                        if (hr.IsFailure)
                                        {
                                            EngLog.Render.Fatal("Failed to close compute command list ({p})", passName);
                                            EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                                            _device.NativeDevice.FlushPendingMessages();
                                            EngLog.ThrowFatal();
                                        }

                                        _pendingComputeCommandLists.Add(commandList);
                                    }

                                    _activeFences |= RenderPassType.Compute;
                                    break;
                                }
                            case RenderEventType.InsertBarrier:
                                {
                                    CompiledRenderPass passToWait = eventInfo.RenderPass1!;
                                    CompiledRenderPass passToSignal = eventInfo.RenderPass2!;

                                    if (passToWait.PassType != passToSignal.PassType)
                                    {
                                        RenderPassType types = passToWait.PassType | passToSignal.PassType;
                                        FlushCommandLists(types);

                                        _activeFences &= ~passToSignal.PassType;
                                        _activeFences |= passToWait.PassType;
                                    }

                                    break;
                                }
                        }
                    }

                    FlushCommandLists(RenderPassType.Raster | RenderPassType.Compute);
                }

                ++_activeFenceValue;
                using (new ProfilingScope("SetFences"))
                {
                    if (_activeFences.HasFlags(RenderPassType.Raster))
                    {
                        _device.NativeDevice.DirectCmdQueue.Signal((ID3D12Fence*)_directFence.Pointer, _activeFenceValue);
                    }

                    if (_activeFences.HasFlags(RenderPassType.Compute))
                    {
                        _device.NativeDevice.ComputeCmdQueue.Signal((ID3D12Fence*)_computeFence.Pointer, _activeFenceValue);
                    }
                }

                using (new ProfilingScope("Present"))
                {
                    foreach (RHISwapChain swapChain in _renderPlan.SwapChains)
                    {
                        swapChain.Present();
                    }
                }

                ReturnToDevice();
            }
        }

        internal void ForceWaitOnFences()
        {
            if (_activeFences != RenderPassType.None)
            {
                using (new ProfilingScope("WaitOnFences"))
                {
                    _fenceWaitEvent.Wait();
                    _fenceWaitEvent.Reset();

                    _activeFences = RenderPassType.None;
                }
            }
        }

        private void ReturnToDevice()
        {
            _renderPlan = null;
            _handle = JobHandle.Completed;

            _device.ReturnFinishedExecutor(this);
        }

        private void FlushCommandLists(RenderPassType passTypes)
        {
            if (passTypes.HasFlags(RenderPassType.Raster) && !_pendingDirectCommandLists.IsEmpty)
            {
                _device.NativeDevice.DirectCmdQueue.ExecuteCommandLists(
                    (uint)_pendingDirectCommandLists.Count,
                    (ID3D12CommandList**)Unsafe.AsPointer(ref _pendingDirectCommandLists[0]));
                _pendingDirectCommandLists.Clear();
                _commandPool.FinishDirectCommandLists();
            }

            if (passTypes.HasFlags(RenderPassType.Compute) && !_pendingComputeCommandLists.IsEmpty)
            {
                _device.NativeDevice.ComputeCmdQueue.ExecuteCommandLists(
                    (uint)_pendingComputeCommandLists.Count,
                    (ID3D12CommandList**)Unsafe.AsPointer(ref _pendingComputeCommandLists[0]));
                _pendingComputeCommandLists.Clear();
                _commandPool.FinishComputeCommandLists();
            }
        }

        private void GenerateCommandList(CompiledRenderPass renderPass, int eventIndex, ID3D12GraphicsCommandList10* commandList, PlaybackDescriptorAllocator descriptorAllocator)
        {
            Debug.Assert(_renderPlan != null);

            ROList<RenderBarrier> barriers = renderPass.Barriers;

            int barrierIndex = 0;
            int nextBarrierCommandOffset = int.MaxValue;

            if (barrierIndex < barriers.Count)
            {
                RenderBarrier barrier = barriers[barrierIndex];
                Debug.Assert(barrier.EventIndex == eventIndex);

                nextBarrierCommandOffset = barrier.CommandOffset;
            }

            // if (renderPass.UninitializedTextures.Count > 0)
            // {
            //     ROHashSet<TextureHandle> uninitializedTextures = renderPass.UninitializedTextures;
            //     foreach (TextureHandle texture in uninitializedTextures)
            //     {
            //         FGResourceDesc.Texture textureDesc = _renderPlan.ResourceDescs[_renderPlan.Resources[_renderPlan.ResourceIndices[texture.Index]].DescriptionIndex].Resource.TextureDesc;
            //         ID3D12Resource2* resource = _resourceManager.GetResourcePointer(texture.Index);
            // 
            //         int subresourceCount = textureDesc.MipCount;
            //         if (textureDesc.Dimension == FGTextureDimension.Tex2DArray)
            //             subresourceCount *= textureDesc.DepthOrArraySize;
            //         else if (textureDesc.Dimension == FGTextureDimension.Cube)
            //             subresourceCount *= 6;
            //         else if (textureDesc.Dimension == FGTextureDimension.CubeArray)
            //             subresourceCount *= 6 * textureDesc.DepthOrArraySize;
            // 
            //         DiscardRegion region = new DiscardRegion
            //         {
            //             NumRects = 0,
            //             PRects = null,
            //             FirstSubresource = 0,
            //             NumSubresources = (uint)subresourceCount
            //         };
            // 
            //         commandList->DiscardResource((ID3D12Resource*)resource, &region);
            //     }
            // }

            InlineArray8<CpuDescriptorHandle> renderTargetArray = default;
            InlineArray8<FGViewport> viewports = default;
            InlineArray8<FGRect> scissors = default;

            D3D12RasterState rasterState = default;

            ID3D12PipelineState* currentPipeline = null;
            ID3D12RootSignature* currentSignature = null;

            D3DPrimitiveTopology currentTopologyType = D3DPrimitiveTopology.D3DPrimitiveTopologyUndefined;

            bool haveConstantsUpdated = false;
            bool haveResourcesUpdated = false;

            int constatsSetData = 0;
            int resourcesSetData = 0;

            bool areResourcesInBuffer = false;

            D3D12RHISwapChain? swapChainToTrySet = null;

            CommandReader commandReader = new CommandReader(renderPass.Recorder);
            while (true)
            {
                if (commandReader.CurrentOffset >= nextBarrierCommandOffset)
                {
                    while (true)
                    {
                        RenderBarrier barrier = barriers[barrierIndex];
                        if (barrier.CommandOffset > commandReader.CurrentOffset)
                        {
                            nextBarrierCommandOffset = barrier.CommandOffset;
                            break;
                        }

                        _barrierBatch.AddBarrierToBatch(in barrier, _renderPlan, renderPass);

                        if (++barrierIndex >= barriers.Count)
                        {
                            nextBarrierCommandOffset = int.MaxValue;
                            break;
                        }
                    }

                    _barrierBatch.FlushBarriers(commandList);
                }

                // Doing this to ensure we get any barriers at the end aswell like swap chains transitions to present
                if (commandReader.IsAtEnd)
                    break;

                CmdCommandType commandType = commandReader.ReadCommandType();
                switch (commandType)
                {
                    case CmdCommandType.ClearRenderTarget:
                        {
                            CmdClearRenderTarget cmdData = commandReader.ReadCommandData<CmdClearRenderTarget>();

                            CpuDescriptorHandle descriptorHandle = descriptorAllocator.GetCpuDescriptorHandle(cmdData.RenderTarget);
                            Color clearColor = cmdData.ClearColor.GetValueOrDefault(Color.TransparentBlack);

                            commandList->ClearRenderTargetView(
                                descriptorHandle,
                                (float*)&clearColor,
                                cmdData.RectCount,
                                cmdData.RectCount > 0 ? (Box2D<int>*)commandReader.ReadCommandDataAsPointer<FGRect>(cmdData.RectCount) : null);
                            break;
                        }
                    case CmdCommandType.ClearDepthStencil:
                        {
                            CmdClearDepthStencil cmdData = commandReader.ReadCommandData<CmdClearDepthStencil>();

                            CpuDescriptorHandle descriptorHandle = descriptorAllocator.GetCpuDescriptorHandle(cmdData.DepthStencil);

                            float depth = cmdData.Depth.GetValueOrDefault(1.0f);
                            byte stencil = cmdData.Stencil.GetValueOrDefault(0xff);

                            commandList->ClearDepthStencilView(
                                descriptorHandle,
                                ClearFlags.Depth | ClearFlags.Stencil,
                                depth,
                                stencil,
                                cmdData.RectCount,
                                cmdData.RectCount > 0 ? (Box2D<int>*)commandReader.ReadCommandDataAsPointer<FGRect>(cmdData.RectCount) : null);
                            break;
                        }
                    case CmdCommandType.ClearUAVFloat:
                        {
                            CmdClearUAVFloat cmdData = commandReader.ReadCommandData<CmdClearUAVFloat>();

                            CpuDescriptorHandle cpuDescriptorHandle = descriptorAllocator.GetCpuDescriptorHandle(cmdData.ResourceCPU);
                            GpuDescriptorHandle gpuDescriptorHandle = descriptorAllocator.GetGpuDescriptorHandle(cmdData.ResourceGPU);

                            ID3D12Resource2* resource = _resourceManager.GetResourcePointer(cmdData.ResourceIndex);

                            commandList->ClearUnorderedAccessViewFloat(
                                gpuDescriptorHandle,
                                cpuDescriptorHandle,
                                (ID3D12Resource*)resource,
                                (float*)Unsafe.AsPointer(ref cmdData.Value),
                                cmdData.RectCount,
                                cmdData.RectCount > 0 ? (Box2D<int>*)commandReader.ReadCommandDataAsPointer<FGRect>(cmdData.RectCount) : null);
                            break;
                        }
                    case CmdCommandType.ClearUAVUInt:
                        {
                            CmdClearUAVUInt cmdData = commandReader.ReadCommandData<CmdClearUAVUInt>();

                            CpuDescriptorHandle cpuDescriptorHandle = descriptorAllocator.GetCpuDescriptorHandle(cmdData.ResourceCPU);
                            GpuDescriptorHandle gpuDescriptorHandle = descriptorAllocator.GetGpuDescriptorHandle(cmdData.ResourceGPU);

                            ID3D12Resource2* resource = _resourceManager.GetResourcePointer(cmdData.ResourceIndex);

                            commandList->ClearUnorderedAccessViewUint(
                                gpuDescriptorHandle,
                                cpuDescriptorHandle,
                                (ID3D12Resource*)resource,
                                (uint*)Unsafe.AsPointer(ref cmdData.Value),
                                cmdData.RectCount,
                                cmdData.RectCount > 0 ? (Box2D<int>*)commandReader.ReadCommandDataAsPointer<FGRect>(cmdData.RectCount) : null);
                            break;
                        }
                    case CmdCommandType.SetRenderTarget:
                        {
                            CmdSetRenderTarget cmdData = commandReader.ReadCommandData<CmdSetRenderTarget>();
                            CpuDescriptorHandle cpuDescriptorHandle = descriptorAllocator.GetCpuDescriptorHandle(cmdData.RenderTarget);

                            renderTargetArray[cmdData.Slot] = cpuDescriptorHandle;
                            rasterState.RTVFormats[cmdData.Slot] = ConversionHelper.ToRenderTargetFormat(cmdData.RenderTargetFormat);
                            break;
                        }
                    case CmdCommandType.CommitRenderTargets:
                        {
                            CmdCommitRenderTargets cmdData = commandReader.ReadCommandData<CmdCommitRenderTargets>();

                            CpuDescriptorHandle depthStencil = cmdData.DepthStencil.IsNull ? default : descriptorAllocator.GetCpuDescriptorHandle(cmdData.DepthStencil);
                            commandList->OMSetRenderTargets(
                                cmdData.NumRenderTargets,
                                cmdData.NumRenderTargets > 0 ? (CpuDescriptorHandle*)Unsafe.AsPointer(ref renderTargetArray[cmdData.StartSlot]) : null,
                                false,
                                cmdData.DepthStencil.IsNull ? null : &depthStencil);

                            rasterState.DSVFormat = ConversionHelper.ToDepthStencilFormat(cmdData.DepthStencilFormat);

                            if (rasterState.RTVFormats.Offset > cmdData.StartSlot)
                            {
                                for (int i = cmdData.StartSlot; i < rasterState.RTVFormats.Offset; ++i)
                                {
                                    rasterState.RTVFormats[i] = Format.FormatUnknown;
                                }
                            }

                            if (rasterState.RTVFormats.Count > cmdData.NumRenderTargets)
                            {
                                for (int i = rasterState.RTVFormats.Count + cmdData.StartSlot; i < cmdData.NumRenderTargets; ++i)
                                {
                                    rasterState.RTVFormats[i] = Format.FormatUnknown;
                                }
                            }

                            rasterState.RTVFormats.Offset = cmdData.StartSlot;
                            rasterState.RTVFormats.Count = cmdData.NumRenderTargets;

                            break;
                        }
                    case CmdCommandType.SetViewport:
                        {
                            CmdSetViewport cmdData = commandReader.ReadCommandData<CmdSetViewport>();
                            viewports[cmdData.Slot] = cmdData.Viewport;
                            break;
                        }
                    case CmdCommandType.CommitViewports:
                        {
                            CmdCommitViewports cmdData = commandReader.ReadCommandData<CmdCommitViewports>();
                            commandList->RSSetViewports(
                                cmdData.NumViewportsToSet,
                                cmdData.NumViewportsToSet > 0 ? (Viewport*)Unsafe.AsPointer(ref viewports[cmdData.StartSlot]) : null);

                            break;
                        }
                    case CmdCommandType.SetScissor:
                        {
                            CmdSetScissor cmdData = commandReader.ReadCommandData<CmdSetScissor>();
                            scissors[cmdData.Slot] = cmdData.Rect;
                            break;
                        }
                    case CmdCommandType.CommitScissors:
                        {
                            CmdCommitScissors cmdData = commandReader.ReadCommandData<CmdCommitScissors>();
                            commandList->RSSetScissorRects(
                                cmdData.NumScissorsToSet,
                                cmdData.NumScissorsToSet > 0 ? (Box2D<int>*)Unsafe.AsPointer(ref scissors[cmdData.StartSlot]) : null);

                            break;
                        }
                    case CmdCommandType.SetStencilRef:
                        {
                            CmdSetStencilRef cmdData = commandReader.ReadCommandData<CmdSetStencilRef>();
                            commandList->OMSetStencilRef(cmdData.StencilRef);
                            break;
                        }
                    case CmdCommandType.SetBlendFactor:
                        {
                            CmdSetBlendFactor cmdData = commandReader.ReadCommandData<CmdSetBlendFactor>();
                            commandList->OMSetBlendFactor((float*)&cmdData.Color);
                            break;
                        }
                    case CmdCommandType.SetPipeline:
                        {
                            CmdSetPipeline cmdData = commandReader.ReadCommandData<CmdSetPipeline>();

                            ID3D12PipelineState* newPipeline = null;
                            if (renderPass.PassType == RenderPassType.Raster)
                            {
                                D3D12RHIGraphicsPipeline graphicsPipeline = renderPass.ObjectsCache.RetrieveObject<D3D12RHIGraphicsPipeline>(cmdData.PipelineIndex);

                                newPipeline = (ID3D12PipelineState*)Unsafe.AsPointer(ref graphicsPipeline.GetPipelineState(rasterState));

                                ID3D12RootSignature* newSignature = *graphicsPipeline.RootSignature.GetAddressOf();
                                if (newSignature != currentSignature)
                                {
                                    commandList->SetGraphicsRootSignature(newSignature);
                                    currentSignature = newSignature;
                                }

                                D3DPrimitiveTopology newTopologyType = ConversionHelper.ToD3DTopologyType(graphicsPipeline.Description.PrimitiveTopologyType);
                                if (newTopologyType != currentTopologyType)
                                {
                                    commandList->IASetPrimitiveTopology(newTopologyType);
                                    currentTopologyType = newTopologyType;
                                }
                            }
                            else
                            {
                                D3D12RHIComputePipeline computePipeline = renderPass.ObjectsCache.RetrieveObject<D3D12RHIComputePipeline>(cmdData.PipelineIndex);
                                newPipeline = *computePipeline.PipelineState.GetAddressOf();

                                ID3D12RootSignature* newSignature = *computePipeline.RootSignature.GetAddressOf();
                                if (newSignature != currentSignature)
                                {
                                    commandList->SetComputeRootSignature(newSignature);
                                    currentSignature = newSignature;
                                }
                            }

                            Debug.Assert(newPipeline != null);
                            if (newPipeline != currentPipeline)
                            {
                                commandList->SetPipelineState(newPipeline);
                                currentPipeline = newPipeline;
                            }

                            break;
                        }
                    case CmdCommandType.SetDescriptorHeap:
                        {
                            CmdSetDescriptorHeap cmdData = commandReader.ReadCommandData<CmdSetDescriptorHeap>();

                            InlineArray2<Ptr<ID3D12DescriptorHeap>> heapsToSet = default;
                            heapsToSet[0] = descriptorAllocator.GetGpuHeapPointer(cmdData.GPUHeapIndex);
                            heapsToSet[1] = descriptorAllocator.GetSamplerHeapPointer(cmdData.SamplerHeapIndex);

                            Debug.Assert(heapsToSet[0] != null && heapsToSet[1] != null);
                            commandList->SetDescriptorHeaps(2, (ID3D12DescriptorHeap**)&heapsToSet);
                            break;
                        }
                    case CmdCommandType.DrawInstanced:
                        {
                            CmdDrawInstanced cmdData = commandReader.ReadCommandData<CmdDrawInstanced>();
                            commandList->DrawInstanced(
                                cmdData.VertexCountPerInstance,
                                cmdData.InstanceCount,
                                cmdData.StartVertexLocation,
                                cmdData.StartInstanceLocation);
                            break;
                        }
                    case CmdCommandType.DrawIndexedInstanced:
                        {
                            CmdDrawIndexedInstanced cmdData = commandReader.ReadCommandData<CmdDrawIndexedInstanced>();
                            commandList->DrawIndexedInstanced(
                                cmdData.IndexCountPerInstance,
                                cmdData.InstanceCount,
                                cmdData.StartIndexLocation,
                                cmdData.BaseVertexLocation,
                                cmdData.StartInstanceLocation);
                            break;
                        }
                    case CmdCommandType.CopyBuffer:
                        {
                            CmdCopyBuffer cmdData = commandReader.ReadCommandData<CmdCopyBuffer>();

                            ID3D12Resource2* source = _resourceManager.GetResourcePointer(cmdData.Source);
                            ID3D12Resource2* destination = _resourceManager.GetResourcePointer(cmdData.Destination);

                            commandList->CopyBufferRegion(
                                (ID3D12Resource*)destination, cmdData.DestinationOffset,
                                (ID3D12Resource*)source, cmdData.SourceOffset,
                                cmdData.NumBytes);

                            break;
                        }
                    case CmdCommandType.CopyTexture:
                        {
                            CmdCopyTexture cmdData = commandReader.ReadCommandData<CmdCopyTexture>();

                            ID3D12Resource2* source = _resourceManager.GetResourcePointer(cmdData.Source);
                            ID3D12Resource2* destination = _resourceManager.GetResourcePointer(cmdData.Destination);

                            TextureCopyLocation sourceCopyLoc = new TextureCopyLocation((ID3D12Resource*)source);
                            if (cmdData.SourceFootprint.Type == TextureCopyFootprint.Type.Buffer)
                            {
                                sourceCopyLoc.Type = TextureCopyType.SubresourceIndex;
                                sourceCopyLoc.SubresourceIndex = cmdData.SourceFootprint.SubresourceIndex;
                            }
                            else
                            {
                                CmdCopyTexture.PlacedFootprint placedFootprint = cmdData.SourceFootprint.PlacedFootprint;

                                sourceCopyLoc.Type = TextureCopyType.PlacedFootprint;
                                sourceCopyLoc.PlacedFootprint = new PlacedSubresourceFootprint
                                {
                                    Offset = placedFootprint.DataOffset,
                                    Footprint = new SubresourceFootprint
                                    {
                                        Format = ConversionHelper.ToTextureFormat(placedFootprint.Format),
                                        Width = placedFootprint.Width,
                                        Height = placedFootprint.Height,
                                        Depth = placedFootprint.Depth,
                                        RowPitch = placedFootprint.RowPitch
                                    }
                                };
                            }

                            TextureCopyLocation destinationCopyLoc = new TextureCopyLocation((ID3D12Resource*)destination);
                            if (cmdData.DestinationFootprint.Type == TextureCopyFootprint.Type.Buffer)
                            {
                                destinationCopyLoc.Type = TextureCopyType.SubresourceIndex;
                                destinationCopyLoc.SubresourceIndex = cmdData.DestinationFootprint.SubresourceIndex;
                            }
                            else
                            {
                                CmdCopyTexture.PlacedFootprint placedFootprint = cmdData.DestinationFootprint.PlacedFootprint;

                                destinationCopyLoc.Type = TextureCopyType.PlacedFootprint;
                                destinationCopyLoc.PlacedFootprint = new PlacedSubresourceFootprint
                                {
                                    Offset = placedFootprint.DataOffset,
                                    Footprint = new SubresourceFootprint
                                    {
                                        Format = ConversionHelper.ToTextureFormat(placedFootprint.Format),
                                        Width = placedFootprint.Width,
                                        Height = placedFootprint.Height,
                                        Depth = placedFootprint.Depth,
                                        RowPitch = placedFootprint.RowPitch
                                    }
                                };
                            }

                            FGBox box = cmdData.SourceBox.GetValueOrDefault();
                            commandList->CopyTextureRegion(
                                &sourceCopyLoc, (uint)cmdData.DestinationOffset.X, (uint)cmdData.DestinationOffset.Y, (uint)cmdData.DestinationOffset.Z,
                                &destinationCopyLoc, cmdData.SourceBox.HasValue ? (Box*)&box : null);

                            break;
                        }
                    case CmdCommandType.SetVertexBuffer:
                        {
                            CmdSetVertexBuffer cmdData = commandReader.ReadCommandData<CmdSetVertexBuffer>();

                            ID3D12Resource2* resource = _resourceManager.GetResourcePointer(cmdData.Buffer);

                            VertexBufferView vbv = new VertexBufferView
                            {
                                BufferLocation = resource->GetGPUVirtualAddress(),
                                SizeInBytes = cmdData.ByteWidth,
                                StrideInBytes = cmdData.ByteStride
                            };

                            commandList->IASetVertexBuffers(0, 1, &vbv);
                            break;
                        }
                    case CmdCommandType.SetIndexBuffer:
                        {
                            CmdSetIndexBuffer cmdData = commandReader.ReadCommandData<CmdSetIndexBuffer>();

                            ID3D12Resource2* resource = _resourceManager.GetResourcePointer(cmdData.Buffer);

                            IndexBufferView ibv = new IndexBufferView
                            {
                                BufferLocation = resource->GetGPUVirtualAddress(),
                                SizeInBytes = cmdData.ByteWidth,
                                Format = cmdData.ByteStride == 2 ? Format.FormatR16Uint : Format.FormatR32Uint
                            };

                            commandList->IASetIndexBuffer(&ibv);
                            break;
                        }
                    case CmdCommandType.SetResourcesData:
                        {
                            CmdSetResourcesData cmdData = commandReader.ReadCommandData<CmdSetResourcesData>();
                            if (cmdData.UseDedicatedBuffer)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                NativeMemory.Copy(
                                    commandReader.ReadCommandDataAsPointer<byte>(cmdData.ResourcesDataSize),
                                    &_constantsDataArray.Buffer[cmdData.ResourcesByteOffset],
                                    cmdData.ResourcesDataSize);

                                haveResourcesUpdated = true;
                                areResourcesInBuffer = false;
                                resourcesSetData = cmdData.ResourcesByteOffset + cmdData.ResourcesDataSize;
                            }

                            break;
                        }
                    case CmdCommandType.SetConstantsData:
                        {
                            CmdSetConstantsData cmdData = commandReader.ReadCommandData<CmdSetConstantsData>();
                            NativeMemory.Copy(
                                    commandReader.ReadCommandDataAsPointer<byte>(cmdData.ConstantsDataSize),
                                    _constantsDataArray.Buffer,
                                    cmdData.ConstantsDataSize);

                            haveConstantsUpdated = true;
                            constatsSetData = cmdData.ConstantsDataSize;
                            break;
                        }
                    case CmdCommandType.CommitShaderInput:
                        {
                            if (haveConstantsUpdated || (haveResourcesUpdated && !areResourcesInBuffer))
                            {
                                int setOffset = haveConstantsUpdated ? 0 : constatsSetData;
                                int dataToSet = Math.Max(constatsSetData, resourcesSetData) - setOffset;

                                if (dataToSet > 0)
                                {
                                    Debug.Assert(currentPipeline != null);

#if DEBUG
                                    Span<uint> dbgView = new Span<uint>(_constantsDataArray.Buffer, 128 / 4);
#endif

                                    if (renderPass.PassType == RenderPassType.Raster)
                                    {
                                        commandList->SetGraphicsRoot32BitConstants(0, (uint)(dataToSet / sizeof(uint)), &_constantsDataArray.Buffer[setOffset], (uint)(setOffset / sizeof(uint)));
                                    }
                                    else
                                    {
                                        commandList->SetComputeRoot32BitConstants(0, (uint)(dataToSet / sizeof(uint)), &_constantsDataArray.Buffer[setOffset], (uint)(setOffset / sizeof(uint)));
                                    }
                                }

                                if (!areResourcesInBuffer)
                                    haveResourcesUpdated = false;
                                haveConstantsUpdated = false;
                            }

                            if (haveResourcesUpdated)
                            {
                                // Should only reach here if they are in a separate buffer
                                Debug.Assert(areResourcesInBuffer);

                                throw new NotImplementedException();
                            }

                            break;
                        }
                    case CmdCommandType.SetSwapChain:
                        {
                            CmdSetSwapChain cmdData = commandReader.ReadCommandData<CmdSetSwapChain>();
                            swapChainToTrySet = renderPass.ObjectsCache.RetrieveObject<D3D12RHISwapChain>(cmdData.SwapChainIndex);

                            break;
                        }
                    case CmdCommandType.UploadResource:
                        {
                            CmdUploadResource cmdData = commandReader.ReadCommandData<CmdUploadResource>();

                            ID3D12Resource2* resource = _resourceManager.GetResourcePointer(cmdData.Resource);

                            RenderResourceInfo resourceInfo = _renderPlan.Resources[_renderPlan.ResourceIndices[cmdData.Resource.Index]];
                            FGResourceType resourceType = resourceInfo.ImportedResource != null ?
                                (resourceInfo.ImportedResource is RHIBuffer ? FGResourceType.Buffer : FGResourceType.Texture) :
                                _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Type;

                            if (resourceType == FGResourceType.Buffer)
                            {
                                RenderUploadInfo uploadInfo = _renderPlan.Uploads[cmdData.UploadIndex];
                                ID3D12Resource2* uploadBuffer = _uploadManager.UploadBuffer;

                                commandList->CopyBufferRegion(
                                    (ID3D12Resource*)resource,
                                    cmdData.Slice,
                                    (ID3D12Resource*)uploadBuffer,
                                    (ulong)uploadInfo.ByteOffsetInHeap,
                                    _uploadManager.GetUploadByteWidth(cmdData.UploadIndex));
                            }
                            else
                            {
                                RenderUploadInfo uploadInfo = _renderPlan.Uploads[cmdData.UploadIndex];
                                ID3D12Resource2* uploadBuffer = _uploadManager.UploadBuffer;

                                RHIFormat rhiFormat = resourceInfo.ImportedResource != null ?
                                    Unsafe.As<RHITexture>(resourceInfo.ImportedResource).Description.Format :
                                    _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Resource.TextureDesc.Format;
                                Format format = ConversionHelper.ToTextureFormat(rhiFormat);

                                Int3 copySize = uploadInfo.DestBox.Size;

                                TextureCopyLocation resourceFootprint = new TextureCopyLocation
                                {
                                    PResource = (ID3D12Resource*)resource,
                                    Type = TextureCopyType.SubresourceIndex,
                                    Anonymous = new TextureCopyLocationUnion
                                    {
                                        SubresourceIndex = cmdData.Slice
                                    }
                                };

                                TextureCopyLocation uploadFootprint = new TextureCopyLocation
                                {
                                    PResource = (ID3D12Resource*)uploadBuffer,
                                    Type = TextureCopyType.PlacedFootprint,
                                    Anonymous = new TextureCopyLocationUnion
                                    {
                                        PlacedFootprint = new PlacedSubresourceFootprint
                                        {
                                            Offset = (ulong)uploadInfo.ByteOffsetInHeap,
                                            Footprint = new SubresourceFootprint
                                            {
                                                Format = format,
                                                Width = (uint)copySize.X,
                                                Height = (uint)copySize.Y,
                                                Depth = (uint)copySize.Z,
                                                RowPitch = (uint)MemoryHelper.AlignUp((int)RHIFormatInfo.Query(rhiFormat).CalculatePitch(copySize.X), Vortice.Direct3D12.D3D12.TextureDataPitchAlignment)
                                            }
                                        }
                                    }
                                };

                                commandList->CopyTextureRegion(
                                    &resourceFootprint, (uint)uploadInfo.DestBox.X1, (uint)uploadInfo.DestBox.Y1, (uint)uploadInfo.DestBox.Z1,
                                    &uploadFootprint, null);
                            }

                            break;
                        }
                    case CmdCommandType.Dispatch:
                        {
                            CmdDispatch cmdData = commandReader.ReadCommandData<CmdDispatch>();
                            commandList->Dispatch(
                                cmdData.ThreadGroupCountX,
                                cmdData.ThreadGroupCountY,
                                cmdData.ThreadGroupCountZ);
                            break;
                        }
                    default: throw new NotSupportedException(commandType.ToString());
                }
            }

            _barrierBatch.AssertIsFlushed();
        }

        internal PlaybackResourceManager ResourceManager => _resourceManager;
        internal PlaybackUploadManager UploadManager => _uploadManager;
        internal PlaybackDescriptorAllocator DirectDescriptorAllocator => _directDescriptorAllocator;
        internal PlaybackDescriptorAllocator ComputeDescriptorAllocator => _computeDescriptorAllocator;
        internal PlaybackBarrierBatch BarrierBatch => _barrierBatch;

        private record struct CommandReader(CommandRecorder Recorder)
        {
            private int _currentOffset = 0;

            public CmdCommandType ReadCommandType()
            {
                CmdCommandType value = *Recorder.GetCommandPointer<CmdCommandType>(_currentOffset);
                _currentOffset += Unsafe.SizeOf<CmdCommandType>();

                return value;
            }

            public T ReadCommandData<T>() where T : unmanaged
            {
                T value = *Recorder.GetCommandPointer<T>(_currentOffset);
                _currentOffset += Unsafe.SizeOf<T>();

                return value;
            }

            public T* ReadCommandDataAsPointer<T>(int elementCount = 1) where T : unmanaged
            {
                T* value = Recorder.GetCommandPointer<T>(_currentOffset);
                _currentOffset += Unsafe.SizeOf<T>() * elementCount;

                return value;
            }

            public readonly int CurrentOffset => _currentOffset;
            public readonly bool IsAtEnd => _currentOffset >= Recorder.CommandOffset;
        }
    }
}
