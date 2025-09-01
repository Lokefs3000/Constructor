using Primary.Common;
using Primary.RHI.Direct3D12.Descriptors;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Memory;
using Primary.RHI.Direct3D12.Utility;
using Serilog;
using SharpGen.Runtime;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;

using Terra = TerraFX.Interop.DirectX;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class GraphicsDeviceImpl : GraphicsDevice
    {
        private static ILogger? s_int_logger = null;

        private readonly bool _cmdBufferValidation = true;

        private bool _isPixEnabled = false;

        private IDXGIFactory7 _factory;
        private IDXGIAdapter4 _adapter;
        private ID3D12SDKConfiguration1 _sdkConfiguration;
        private ID3D12DeviceFactory _deviceFactory;
        private ID3D12Debug6? _debug;
        private ID3D12InfoQueue? _infoQueue;
        private ID3D12DeviceRemovedExtendedDataSettings2? _dredSettings;
        private ID3D12Device14 _device;
        private ID3D12DeviceConfiguration _deviceConfig;
        private Terra.D3D12MA_Allocator* _allocator;
        private ID3D12CommandQueue _graphicsQueue;
        private ID3D12CommandQueue _computeQueue;
        private ID3D12CommandQueue _copyQueue;

        private UploadManager _uploadManager;

        private CpuDescriptorHeap _rtvDescriptorHeap;
        private CpuDescriptorHeap _dsvDescriptorHeap;
        private CpuDescriptorHeap _srvCbvUavDescriptorHeap;

        private AllocatorStack[] _commandAllocators;

        private ConcurrentStack<Action> _deferredResourceFrees;
        private ConcurrentQueue<(SwapChainImpl SW, PresentParameters Params)> _queuedSwapChainPresentations;
        private List<ICommandBufferImpl> _commandBuffersUsed;

        private ConcurrentDictionary<SwapChainImpl, Vector2> _pendingResizes;

        private WeakReference _lastSubmittedCommandBuffer;
        private ID3D12Fence _synchronizeFence;
        private ManualResetEventSlim _waitForCompletionEvent;

        private string _deviceName;
        private ulong _videoMemory;

        private bool _disposedValue;

        internal GraphicsDeviceImpl(ILogger logger)
        {
            s_int_logger = logger;

            _commandAllocators = new AllocatorStack[6];

            _deferredResourceFrees = new ConcurrentStack<Action>();
            _queuedSwapChainPresentations = new ConcurrentQueue<(SwapChainImpl SW, PresentParameters Params)>();
            _commandBuffersUsed = new List<ICommandBufferImpl>();

            _pendingResizes = new ConcurrentDictionary<SwapChainImpl, Vector2>();

            _lastSubmittedCommandBuffer = new WeakReference(null);
            _waitForCompletionEvent = new ManualResetEventSlim(false);

            {
                if (File.Exists("WinPixEventRuntime.dll") || File.Exists("runtimes/win-x64/native/WinPixEventRuntime.dll"))
                {
                    _isPixEnabled = true;
                }
            }

            try
            {
                {
                    ResultChecker.ThrowIfUnhandled(DXGI.CreateDXGIFactory2(true, out _factory!));
                }
                {
                    int adapterId = _factory.GetAdapterByGpuPreference(GpuPreference.HighPerformance);

                    //TODO: improve the logic for finding the adapter instead of hardcoded magic number "0"!
                    ResultChecker.ThrowIfUnhandled(_factory.EnumAdapters1(0, out IDXGIAdapter1 adapter1));
                    ResultChecker.ThrowIfUnhandled(adapter1.QueryInterface(out _adapter!));
                }
                {
                    ResultChecker.ThrowIfUnhandled(D3D12.D3D12GetInterface(D3D12.D3D12SDKConfigurationClsId, out _sdkConfiguration!));
                    ResultChecker.ThrowIfUnhandled(_sdkConfiguration.CreateDeviceFactory(616, "./D3D12/", out _deviceFactory!));
                }
                {
                    _deviceFactory.GetConfigurationInterface(D3D12.D3D12DebugClsId, out _debug);
                    if (_debug != null)
                    {
                        Logger.Information("Debug layer enabled successfully!");

                        _debug.EnableDebugLayer();
                        _debug.SetEnableSynchronizedCommandQueueValidation(true);
                        _debug.SetEnableAutoName(true);
                     
#if false
                        _debug.SetEnableGPUBasedValidation(true);
                        _debug.SetGPUBasedValidationFlags(GpuBasedValidationFlags.None);
#endif
                    }
                }
                {
                    _deviceFactory.GetConfigurationInterface(D3D12.D3D12DeviceRemovedExtendedDataClsId, out _dredSettings);
                    if (_dredSettings != null)
                    {
                        Logger.Information("DRED successfully retrieved!");

                        _dredSettings.SetAutoBreadcrumbsEnablement(DredEnablement.ForcedOn);
                        _dredSettings.SetBreadcrumbContextEnablement(DredEnablement.ForcedOn);
                        _dredSettings.SetPageFaultEnablement(DredEnablement.ForcedOn);
                    }
                }
                {
                    ResultChecker.ThrowIfUnhandled(_deviceFactory.CreateDevice(_adapter, FeatureLevel.Level_12_0, out _device!));
                }
                {
                    if (_debug != null)
                    {
                        ResultChecker.PrintIfUnhandled(_device.QueryInterface(out _infoQueue));
                        if (_infoQueue != null)
                        {
                            Logger.Information("Info queue queried successfully!");

                            _infoQueue.ClearStorageFilter();
                            _infoQueue.PushStorageFilter(new InfoQueueFilter
                            {
                                AllowList = new InfoQueueFilterDescription
                                {
                                    Severities = [MessageSeverity.Info, MessageSeverity.Warning, MessageSeverity.Error, MessageSeverity.Corruption]
                                },
                                DenyList = new InfoQueueFilterDescription
                                {
                                    Categories = [MessageCategory.StateCreation],
                                    Ids = _ignoredIds.ToArray()
                                }
                            });

                            //_infoQueue.SetBreakOnSeverity(MessageSeverity.Error | MessageSeverity.Corruption, true);
                        }
                    }
                }
                {
                    ResultChecker.ThrowIfUnhandled(_device.QueryInterface(out _deviceConfig!));
                }
                {
                    Terra.D3D12MA_ALLOCATOR_DESC desc = new()
                    {
                        Flags = Terra.D3D12MA_ALLOCATOR_FLAGS.D3D12MA_ALLOCATOR_FLAG_NONE,
                        pDevice = (Terra.ID3D12Device*)_device.NativePointer.ToPointer(),
                        PreferredBlockSize = 0,
                        pAllocationCallbacks = null,
                        pAdapter = (Terra.IDXGIAdapter*)_adapter.NativePointer.ToPointer()
                    };

                    Terra.D3D12MA_Allocator* allocator = null;
                    ResultChecker.ThrowIfUnhandled(new Result(Terra.D3D12MemAlloc.D3D12MA_CreateAllocator(&desc, &allocator).Value));

                    _allocator = allocator;
                }
                {
                    ResultChecker.ThrowIfUnhandled(_device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct), out _graphicsQueue!));
                    ResultChecker.ThrowIfUnhandled(_device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Compute), out _computeQueue!));
                    ResultChecker.ThrowIfUnhandled(_device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Copy), out _copyQueue!));
                }
                {
                    const uint UploadManagerStartSize = 1048576 /*1mb*/;

                    _uploadManager = new UploadManager(this, UploadManagerStartSize);
                }
                {
                    ResultChecker.ThrowIfUnhandled(_device.CreateFence(0, FenceFlags.None, out _synchronizeFence!));
                }
                {
                    _rtvDescriptorHeap = new CpuDescriptorHeap(this, DescriptorHeapType.RenderTargetView, 256u);
                    _dsvDescriptorHeap = new CpuDescriptorHeap(this, DescriptorHeapType.DepthStencilView, 256u);
                    _srvCbvUavDescriptorHeap = new CpuDescriptorHeap(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 2048u);
                }
                {
                    for (int i = 0; i < _commandAllocators.Length; i++)
                    {
                        _commandAllocators[i] = new AllocatorStack
                        {
                            Ready = new ConcurrentStack<ID3D12CommandAllocator>(),
                            Old = new ConcurrentStack<ID3D12CommandAllocator>()
                        };
                    }
                }
                {
                    AdapterDescription3 desc = _adapter.Description3;

                    _deviceName = desc.Description;
                    _videoMemory = desc.DedicatedVideoMemory;
                }
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                SynchronizeDevice(SynchronizeDeviceTargets.All);

                while (_deferredResourceFrees.TryPop(out Action? result))
                {
                    result.Invoke();
                }

                _waitForCompletionEvent.Dispose();

                for (int i = 0; i < _commandAllocators.Length; i++)
                {
                    ref AllocatorStack stack = ref _commandAllocators[i];
                    while (stack.Ready.TryPop(out ID3D12CommandAllocator? allocator))
                        allocator.Dispose();
                    while (stack.Old.TryPop(out ID3D12CommandAllocator? allocator))
                        allocator.Dispose();
                }

                _rtvDescriptorHeap.Dispose();
                _dsvDescriptorHeap.Dispose();
                _srvCbvUavDescriptorHeap.Dispose();
                _graphicsQueue.Dispose();
                _computeQueue.Dispose();
                _copyQueue.Dispose();
                if (_allocator != null) _allocator->Release();
                _deviceConfig?.Dispose();
                _infoQueue?.Dispose();
                _device?.Dispose();
                _dredSettings?.Dispose();
                _debug?.Dispose();
                _deviceFactory?.Dispose();
                _sdkConfiguration?.Dispose();
                _adapter?.Dispose();
                _factory?.Dispose();

                _disposedValue = true;
            }
        }

        ~GraphicsDeviceImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override void SynchronizeDevice(SynchronizeDeviceTargets targets)
        {
            if (FlagUtility.HasFlag(targets, SynchronizeDeviceTargets.Graphics))
                WaitForFenceOnQueue(_graphicsQueue, _synchronizeFence);
            if (FlagUtility.HasFlag(targets, SynchronizeDeviceTargets.Compute))
                WaitForFenceOnQueue(_computeQueue, _synchronizeFence);
            if (FlagUtility.HasFlag(targets, SynchronizeDeviceTargets.Copy))
                WaitForFenceOnQueue(_copyQueue, _synchronizeFence);

            void WaitForFenceOnQueue(ID3D12CommandQueue queue, ID3D12Fence fence)
            {
                ulong nextValue = fence.CompletedValue == 2 ? 1ul : 2ul;
                _waitForCompletionEvent.Reset();

                ResultChecker.PrintIfUnhandled(queue.Signal(fence, nextValue));
                ResultChecker.PrintIfUnhandled(_synchronizeFence.SetEventOnCompletion(nextValue, null));

                /*if (fence.CompletedValue != nextValue)
                {
                    Logger.Information("Wait {a} -> {b}", fence.CompletedValue, nextValue);
                    ExceptionUtility.Assert(_waitForCompletionEvent.Wait(2000));
                }*/
            }
        }

        public override void BeginFrame()
        {
            if (_lastSubmittedCommandBuffer.Target != null)
            {
                ICommandBufferImpl commandBuffer = (ICommandBufferImpl)_lastSubmittedCommandBuffer.Target;
                if (commandBuffer.ExecutionCompleteFence.CompletedValue < commandBuffer.FenceValue)
                {
                    commandBuffer.ExecutionCompleteFence.SetEventOnCompletion(commandBuffer.FenceValue);
                }
            }

            if (_pendingResizes.Count > 0)
            {
                foreach (var kvp in _pendingResizes)
                {
                    kvp.Key.ResizeInternal(kvp.Value);
                }

                _pendingResizes.Clear();
            }

            while (_deferredResourceFrees.TryPop(out Action? result))
            {
                result.Invoke();
            }

            _rtvDescriptorHeap.ReleaseStaleAllocations();
            _dsvDescriptorHeap.ReleaseStaleAllocations();
            _srvCbvUavDescriptorHeap.ReleaseStaleAllocations();

            _uploadManager.ReleasePreviousBuffers();

            _lastSubmittedCommandBuffer.Target = null;
        }

        public override void FinishFrame()
        {
            while (_queuedSwapChainPresentations.TryDequeue(out var result))
            {
                result.SW.PresentInternal(result.Params);
            }

            for (int i = 0; i < _commandBuffersUsed.Count; i++)
            {
                _commandBuffersUsed[i].ResetFrameData();
            }

            for (int i = 0; i < _commandAllocators.Length; i++)
            {
                ref AllocatorStack stack = ref _commandAllocators[i];
                while (stack.Old.TryPop(out ID3D12CommandAllocator? allocator))
                    stack.Ready.Push(allocator);
            }

            DumpMessageQueue();
        }

        public override void Submit(CommandBuffer commandBuffer)
        {
            ICommandBufferImpl impl = (ICommandBufferImpl)commandBuffer;
            /*if (impl.ExecutionCompleteFence.CompletedValue < impl.FenceValue)
            {
                Logger.Information("Waiting on fence value: {v}", impl.FenceValue);
                //TODO: maybe add a bogus Thread.Sleep to ensure no issues occur?
                if (ResultChecker.PrintIfUnhandled(impl.ExecutionCompleteFence.SetEventOnCompletion(impl.FenceValue, _waitForCompletionEvent.WaitHandle), this));
                {
                    _waitForCompletionEvent.Wait(2000);
                    _waitForCompletionEvent.Reset();
                }
            }*/

            //Logger.Information("Submitting {a} to queue and waiting on {b}", commandBuffer, _lastSubmittedCommandBuffer.Target);

            ICommandBufferImpl? previousCommandBuffer = (ICommandBufferImpl?)_lastSubmittedCommandBuffer.Target;
            impl.SubmitToQueueInternal(previousCommandBuffer?.ExecutionCompleteFence, previousCommandBuffer?.FenceValue ?? 0);

            _lastSubmittedCommandBuffer.Target = impl;
        }

        #region Create functions
        public override SwapChain CreateSwapChain(Vector2 clientSize, nint windowHandle)
        {
            try
            {
                return new SwapChainImpl(this, clientSize, windowHandle);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override Buffer CreateBuffer(BufferDescription description, nint bufferData)
        {
            try
            {
                return new BufferImpl(this, description, bufferData);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override Texture CreateTexture(TextureDescription description, Span<nint> textureData)
        {
            try
            {
                return new TextureImpl(this, description, textureData);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override PipelineLibrary CreatePipelineLibrary(Span<byte> initialData)
        {
            try
            {
                return new PipelineLibraryImpl(this, initialData);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override GraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDescription description, GraphicsPipelineBytecode bytecode)
        {
            try
            {
                return new GraphicsPipelineImpl(this, description, bytecode);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override CopyCommandBuffer CreateCopyCommandBuffer()
        {
            try
            {
                return new CopyCommandBufferImpl(this);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override GraphicsCommandBuffer CreateGraphicsCommandBuffer()
        {
            try
            {
                return new GraphicsCommandBufferImpl(this);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override Fence CreateFence(ulong initialValue)
        {
            try
            {
                return new FenceImpl(this, initialValue);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }

        public override RenderTarget CreateRenderTarget(RenderTargetDescription description)
        {
            try
            {
                return new RenderTargetImpl(this, description);
            }
            catch (Exception)
            {
                DumpMessageQueue();
                throw;
            }
        }
        #endregion

        #region Support
        private Dictionary<Format, FormatSupport1> _support = new Dictionary<Format, FormatSupport1>();

        public override bool IsSupported(RenderTargetFormat format)
        {
            Format dxgiFormat = FormatConverter.Convert(format);
            if (!_support.TryGetValue(dxgiFormat, out FormatSupport1 support1))
            {
                FeatureDataFormatSupport support = new FeatureDataFormatSupport
                {
                    Format = dxgiFormat,
                };

                if (!_device.CheckFeatureSupport(Vortice.Direct3D12.Feature.FormatSupport, ref support))
                {
                    _support[support.Format] = FormatSupport1.None;
                }
                else
                {
                    _support[support.Format] = support.Support1;
                }

                support1 = support.Support1;
            }

            return FlagUtility.HasFlag((int)support1, (int)FormatSupport1.RenderTarget);
        }

        public override bool IsSupported(DepthStencilFormat format)
        {
            Format dxgiFormat = FormatConverter.Convert(format);
            if (!_support.TryGetValue(dxgiFormat, out FormatSupport1 support1))
            {
                FeatureDataFormatSupport support = new FeatureDataFormatSupport
                {
                    Format = dxgiFormat,
                };

                if (!_device.CheckFeatureSupport(Vortice.Direct3D12.Feature.FormatSupport, ref support))
                {
                    _support[support.Format] = FormatSupport1.None;
                }
                else
                {
                    _support[support.Format] = support.Support1;
                }

                support1 = support.Support1;
            }

            return FlagUtility.HasFlag(support1, FormatSupport1.DepthStencil);
        }

        public override bool IsSupported(TextureFormat format, TextureDimension dimension)
        {
            Format dxgiFormat = FormatConverter.Convert(format);
            if (!_support.TryGetValue(dxgiFormat, out FormatSupport1 support1))
            {
                FeatureDataFormatSupport support = new FeatureDataFormatSupport
                {
                    Format = dxgiFormat,
                };

                if (!_device.CheckFeatureSupport(Vortice.Direct3D12.Feature.FormatSupport, ref support))
                {
                    _support[support.Format] = FormatSupport1.None;
                }
                else
                {
                    _support[support.Format] = support.Support1;
                }

                support1 = support.Support1;
            }

            return FlagUtility.HasFlag(support1, dimension switch
            {
                TextureDimension.Texture1D => FormatSupport1.Texture1D,
                TextureDimension.Texture2D => FormatSupport1.Texture2D,
                TextureDimension.Texture3D => FormatSupport1.Texture3D,
                TextureDimension.TextureCube => FormatSupport1.TextureCube,
                _ => FormatSupport1.None
            });
        }
        #endregion

        #region Interface
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnqueueDataFree(Action function)
        {
            _deferredResourceFrees.Push(function);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void QueueSwapChainForPresentation(SwapChainImpl swapChain, PresentParameters parameters)
        {
            _queuedSwapChainPresentations.Enqueue((swapChain, parameters));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnqueueForResizeNextFrame(SwapChainImpl swapChain, Vector2 size)
        {
            _pendingResizes[swapChain] = size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddCommandBuffer(ICommandBufferImpl commandBuffer)
        {
            _commandBuffersUsed.Add(commandBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveCommandBuffer(ICommandBufferImpl commandBuffer)
        {
            _commandBuffersUsed.Remove(commandBuffer);
        }

        internal ID3D12CommandAllocator GetNewCommandAllocator(CommandListType type)
        {
            ref AllocatorStack stack = ref _commandAllocators[(int)type];
            if (!stack.Ready.TryPop(out ID3D12CommandAllocator? allocator))
            {
                ResultChecker.ThrowIfUnhandled(_device.CreateCommandAllocator(type, out allocator));
            }

            return allocator!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReturnCommandAllocator(CommandListType type, ID3D12CommandAllocator allocator)
        {
            _commandAllocators[(int)type].Old.Push(allocator);
        }
        #endregion

        private HashSet<MessageId> _ignoredIds = [
            MessageId.ClearDepthStencilViewMismatchingClearValue,
            MessageId.ClearRenderTargetViewMismatchingClearValue,
            MessageId.CreateGraphicsPipelineStateDepthStencilViewNotSet,
            MessageId.CreateGraphicsPipelineStateRenderTargetViewNotSet
            ];

        internal void DumpMessageQueue()
        {
            if (_infoQueue != null)
            {
                ulong numStored = _infoQueue.NumStoredMessages;
                for (ulong i = 0; i < numStored; i++)
                {
                    Message message = _infoQueue.GetMessage(i);

                    //TODO: fix me because d3d12 wont stop sending messages ive already ignored
                    if (_ignoredIds.Contains(message.Id))
                        continue;

                    switch (message.Severity)
                    {
                        case MessageSeverity.Corruption: Logger.Fatal("[D3D12]: [{cat}/{id}]: {desc}", message.Category, message.Id, message.Description); break;
                        case MessageSeverity.Error: Logger.Error("[D3D12]: [{cat}/{id}]: {desc}", message.Category, message.Id, message.Description); break;
                        case MessageSeverity.Warning: Logger.Warning("[D3D12]: [{cat}/{id}]: {desc}", message.Category, message.Id, message.Description); break;
                        case MessageSeverity.Info: Logger.Information("[D3D12]: [{cat}/{id}]: {desc}", message.Category, message.Id, message.Description); break;
                        case MessageSeverity.Message: Logger.Debug("[D3D12]: [{cat}/{id}]: {desc}", message.Category, message.Id, message.Description); break;
                    }
                }

                _infoQueue.ClearStoredMessages();
            }
        }

        internal void DumpDREDOutput()
        {
            using ID3D12DeviceRemovedExtendedData2? dred = _device.QueryInterfaceOrNull<ID3D12DeviceRemovedExtendedData2>();
            if (dred == null)
            {
                Logger.Error("No DRED data available to diagnose device removal!");
                return;
            }

            Logger.Error("Outputting DRED information to aid debugging..");

            Logger.Error("Device State: {state}", dred.DeviceState);

            DredAutoBreadcrumbsOutput1? breadcrumbsOutput;
            dred.GetAutoBreadcrumbsOutput1(out breadcrumbsOutput);

            if (breadcrumbsOutput != null)
            {
                Logger.Error("Breadcrumb provided information:");

                AutoBreadcrumbNode1? currentNode = breadcrumbsOutput.HeadAutoBreadcrumbNode;
                int index = 0;
                while (currentNode != null)
                {
                    Logger.Error("    Breadcrumb node {idx}:", index++);
                    Logger.Error("        CommandList: {cmdList}", currentNode.CommandListDebugName);
                    Logger.Error("        CommandQueue: {cmdQueue}", currentNode.CommandQueueDebugName);
                    Logger.Error("        History:");

                    if (currentNode.CommandHistory != null)
                    {
                        for (int i = 0; i < currentNode.CommandHistory.Length; i++)
                        {
                            Logger.Error("            {value}", currentNode.CommandHistory[i]);
                        }
                    }
                    else
                        Logger.Error("            null");

                    Logger.Error("        Context:");

                    if (currentNode.BreadcrumbContexts != null)
                    {
                        for (int i = 0; i < currentNode.BreadcrumbContexts.Length; i++)
                        {
                            DredBreadcrumbContext context = currentNode.BreadcrumbContexts[i];
                            Logger.Error("            {}: {value}", context.BreadcrumbIndex, context.ContextString);
                        }
                    }
                    else
                        Logger.Error("            null");

                    currentNode = currentNode.Next;
                }
            }
            else
                Logger.Error("No auto breadcrumbs output!");

            try
            {
                DredPageFaultOutput2 pageFault = dred.PageFaultAllocationOutput2;
                Logger.Error("Pagefault provided information:");

                Logger.Error("    Virtual address: {va}, Flags: {flags}", pageFault.PageFaultVA, pageFault.PageFaultFlags);
                Logger.Error("    Recent allocations:");

                DredAllocationNode1* allocNode = (DredAllocationNode1*)pageFault.HeadExistingAllocationNode;
                int index = 0;
                while (allocNode != null)
                {
                    Logger.Error("    {i}: Name: {name}, Allocation type: {type}", index++, Marshal.PtrToStringUni(allocNode->ObjectNameW), allocNode->AllocationType);
                    allocNode = allocNode->pNext;
                }

                Logger.Error("    Recent frees:");

                allocNode = (DredAllocationNode1*)pageFault.HeadRecentFreedAllocationNode;
                index = 0;
                while (allocNode != null)
                {
                    Logger.Error("    {i}: Name: {name}, Allocation type: {type}", index++, Marshal.PtrToStringUni(allocNode->ObjectNameW), allocNode->AllocationType);
                    allocNode = allocNode->pNext;
                }
            }
            catch (Exception)
            {
                Log.Error("No page fault allocation output!");
            }
        }

        internal bool CmdBufferValidation => _cmdBufferValidation;
        internal bool IsPixEnabled => _isPixEnabled;

        internal IDXGIFactory7 DXGIFactory => _factory;
        internal ID3D12DeviceConfiguration D3D12DeviceConfiguration => _deviceConfig;
        internal ID3D12Device14 D3D12Device => _device;
        internal ID3D12CommandQueue DirectCommandQueue => _graphicsQueue;
        internal ID3D12CommandQueue ComputeCommandQueue => _computeQueue;
        internal ID3D12CommandQueue CopyCommandQueue => _copyQueue;

        internal Terra.D3D12MA_Allocator* D3D12MAAllocator => _allocator;

        internal UploadManager UploadManager => _uploadManager;

        internal CpuDescriptorHeap CpuRTVDescriptors => _rtvDescriptorHeap;
        internal CpuDescriptorHeap CpuDSVDescriptors => _dsvDescriptorHeap;
        internal CpuDescriptorHeap CpuSRVCBVUAVDescriptors => _srvCbvUavDescriptorHeap;

        public override GraphicsAPI API => GraphicsAPI.Direct3D12;
        public override string Name => _deviceName;

        public override ulong TotalVideoMemory => _videoMemory;

        internal static ILogger Logger => NullableUtility.ThrowIfNull(s_int_logger);

        private struct DredAllocationNode1
        {
            public nint ObjectNameA;

            public nint ObjectNameW;

            public DredAllocationType AllocationType;

            public unsafe DredAllocationNode1* pNext;
        }

        private record struct AllocatorStack
        {
            public ConcurrentStack<ID3D12CommandAllocator> Ready;
            public ConcurrentStack<ID3D12CommandAllocator> Old;
        }
    }
}
