using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Primary.Common;
using Primary.Memory.Native;
using Primary.RHI.Validation;
using Serilog;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

using static Interop.D3D12MemAlloc.ALLOCATOR_FLAGS;

using D3D12MA = Interop.D3D12MemAlloc;
using Feature = Silk.NET.Direct3D12.Feature;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe sealed class D3D12RHIDevice : RHIDevice
    {
        private ILogger? _logger;

        private D3D12RHISetup _setup;

        private CancellationTokenSource _videoBudgetCts;
        private TerraFX.Interop.Windows.HANDLE _videoBudgetChangeEvent;
        private Thread _videoBudgetThread;
        private uint _videoBudgetChangeCookie;

        private ComPtr<IDXGIFactory7> _factory;
        private ComPtr<IDXGIAdapter4> _adapter;

        private ComPtr<ID3D12Debug6> _debug;
        private ComPtr<ID3D12Device14> _device;

        private ComPtr<ID3D12InfoQueue> _infoQueue;
        private ComPtr<ID3D12InfoQueue1> _infoQueue1;
        private ComPtr<ID3D12DeviceRemovedExtendedDataSettings1> _dredSettings;

        private ComPtr<ID3D12CommandQueue> _directCmdQueue;
        private ComPtr<ID3D12CommandQueue> _computeCmdQueue;
        private ComPtr<ID3D12CommandQueue> _copyCmdQueue;

        private D3D12MA.Allocator* _d3d12Allocator;

        private CompositionDevice? _compositionDevice;

        private D3D12RHIDeviceNative* _nativeRep;

        private UploadManager _uploadManager;
        private ResourceTracker _resourceTracker;

        private ConcurrentQueue<(Action, ulong)> _pendingFreeCallbacks;

        private CancellationTokenSource _resoureFreeCts;
        private AutoResetEvent _resourceFreeEvent;
        private Thread _resourceFreeThread;

        private int _debugMessageWidth;
        private void* _debugMessageData;

        private ulong _renderFrameIndex;
        private ulong _frameIndex;

        internal D3D12RHIDevice(RHIDeviceDescription description, ILogger? logger)
        {
            s_instance.Target = this;

            _logger = logger;

            //Setup
            {
                _setup = new D3D12RHISetup
                {
                    UseTightAlignment = !AppArguments.HasArgument("d3d12-no-tight-alignment")
                };
            }

            //DXGI
            {
                uint flags = 0;
                if (description.EnableValidation)
                    flags |= DXGI.CreateFactoryDebug;

                HResult hr = DXGI.CreateDXGIFactory2(flags, out _factory);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create DXGI factory with error", hr.Value);
                }
            }

            {
                HResult hr = _factory.EnumAdapterByGpuPreference(0, GpuPreference.HighPerformance, out _adapter);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to enumerate for a valid DXGI adapter", hr.Value);
                }
            }

            //Video budget
            fixed (uint* cookie = &_videoBudgetChangeCookie)
            {
                _videoBudgetCts = new CancellationTokenSource();
                _videoBudgetChangeEvent = TerraFX.Interop.Windows.Windows.CreateEventA(null, false, true, null);
                _videoBudgetThread = new Thread(VideoBudgetThreadProc) { IsBackground = true };
                _videoBudgetChangeCookie = 0;

                HResult hr = _adapter.RegisterVideoMemoryBudgetChangeNotificationEvent(_videoBudgetChangeEvent, cookie);
                if (hr.IsSuccess)
                {
                    _videoBudgetThread.Start();
                }
                else
                {
                    logger?.Warning("Failed to register event for a video memory budget change event");
                }
            }

            //D3D12
            if (description.EnableValidation)
            {
                HResult hr = D3D12.GetDebugInterface(out _debug);
                if (hr.IsSuccess)
                {
                    _debug.EnableDebugLayer();
                    _debug.SetEnableAutoName(true);
                }
                else
                    _logger?.Warning("Failed to query D3D12 debug interface!");
            }

            {
                ComPtr<ID3D12Device14> deviceComPtr = default;

                HResult hr = D3D12.CreateDevice(_adapter, D3DFeatureLevel.Level122, out deviceComPtr);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create D3D12 device", hr.Value);
                }

                _device = deviceComPtr;

                if (_device.Handle == null)
                {
                    throw new D3D12RHIException($"D3D12 device handle is null!");
                }
            }

            //Validate device features
            {
                D3DShaderModel shaderModel = D3DShaderModel.HighestShaderModel;

                while (shaderModel > D3DShaderModel.ShaderModel51)
                {
                    FeatureDataShaderModel featureDataShaderModel = new FeatureDataShaderModel { HighestShaderModel = shaderModel };

                    HResult hr = new HResult(_device.CheckFeatureSupport(Feature.ShaderModel, &featureDataShaderModel, (uint)Unsafe.SizeOf<FeatureDataShaderModel>()));
                    if (hr.IsSuccess)
                    {
                        break;
                    }

                    --shaderModel;
                }

                if (shaderModel < D3DShaderModel.ShaderModel66)
                    throw new D3D12RHIException("Needs atleast shader model 6.6 support");

                //AssertFeatureSupport<D3D12_FEATURE_DATA_SHADER_MODEL>(D3D12_FEATURE_SHADER_MODEL, (x) =>
                //{
                //    if (x.HighestShaderModel < D3D_SHADER_MODEL_6_6)
                //        throw new RHIException("Shader model 6.6 support not found!");
                //});

                ResourceBindingTier resourceBindingTier = 0;
                ResourceHeapTier resourceHeapTier = 0;
                TightAlignmentTier tightAlignmentTier = 0;
                D3DRootSignatureVersion rootSignatureVersion = 0;
                ShaderMinPrecisionSupport shaderMinPrecisionSupport = 0;
                ConservativeRasterizationTier conservativeRasterizationTier = 0;

                AssertFeatureSupport<FeatureDataD3D12Options>(Feature.D3D12Options, (x) =>
                {
                    if (x.ResourceBindingTier < ResourceBindingTier.Tier3)
                        throw new D3D12RHIException("Needs atleast resource binding tier 3!");
                    if (x.ResourceHeapTier < ResourceHeapTier.Tier2)
                        throw new D3D12RHIException("Needs atleast resource heap tier 3!");

                    resourceBindingTier = x.ResourceBindingTier;
                    resourceHeapTier = x.ResourceHeapTier;
                    shaderMinPrecisionSupport = x.MinPrecisionSupport;
                    conservativeRasterizationTier = x.ConservativeRasterizationTier;
                });

                AssertFeatureSupport<FeatureDataTightAlignment>(Feature.D3D12TightAlignment, (x) =>
                {
                    if (x.SupportTier < TightAlignmentTier.Tier1)
                    {
                        _setup.UseTightAlignment = false;
                        _logger?.Information("No tight alignment support from GPU");
                    }

                    tightAlignmentTier = x.SupportTier;
                });

                // AssertFeatureSupport<FeatureDataRootSignature>(Feature.RootSignature, (x) =>
                // {
                //     if (x.HighestVersion < D3DRootSignatureVersion.Version12)
                //         throw new RHIException("Needs atleast resource root signature version 1.2!");
                // 
                //     rootSignatureVersion = x.HighestVersion;
                // });

                EngLog.RHI.Information(@"Relevant d3d12 feature caps:
    Shader model: {a}
    Resource binding tier: {b}
    Resource heap tier: {c}
    Tight alignment tier: {d} (enabled: {i})
    Root signature version: {e}
    Shader minimum precision support: {f}
    Conservative raster tier: {g}", shaderModel, resourceBindingTier, resourceHeapTier, tightAlignmentTier, _setup.UseTightAlignment, rootSignatureVersion, shaderMinPrecisionSupport, conservativeRasterizationTier);

                void AssertFeatureSupport<T>(Feature feature, Action<T> callback) where T : unmanaged
                {
                    T data = default;
                    HResult hr = _device.CheckFeatureSupport(feature, &data, (uint)Unsafe.SizeOf<T>());
                    if (hr.IsFailure)
                        throw new D3D12RHIException($"Failed to query support for feature: {feature}", hr.Value);

                    callback(data);
                }
            }

            {
                if (new HResult(_device.QueryInterface(out _infoQueue1)).IsSuccess)
                {
                    //_infoQueue1.Get()->RegisterMessageCallback()
                }

                if (new HResult(_device.QueryInterface(out _infoQueue)).IsSuccess)
                {
                    fixed (MessageSeverity* ptr0 = s_allowedSeverities)
                    {
                        fixed (MessageID* ptr1 = s_deniedIds)
                        {
                            var filter = new Silk.NET.Direct3D12.InfoQueueFilter
                            {
                                AllowList = new Silk.NET.Direct3D12.InfoQueueFilterDesc
                                {
                                    NumSeverities = (uint)s_allowedSeverities.Length,
                                    PSeverityList = ptr0
                                },
                                DenyList = new Silk.NET.Direct3D12.InfoQueueFilterDesc
                                {
                                    NumIDs = (uint)s_deniedIds.Length,
                                    PIDList = ptr1
                                }
                            };

                            _infoQueue.ClearStorageFilter();
                            _infoQueue.PushStorageFilter(&filter);
                        }
                    }

                    //_infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_CORRUPTION, true);
                    //_infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_ERROR, true);
                }

                if (new HResult(_device.QueryInterface(out _dredSettings)).IsSuccess)
                {
                    _dredSettings.SetAutoBreadcrumbsEnablement(DredEnablement.ForcedOn);
                    _dredSettings.SetBreadcrumbContextEnablement(DredEnablement.ForcedOn);
                    _dredSettings.SetPageFaultEnablement(DredEnablement.ForcedOn);
                }
            }

            {
                CommandQueueDesc desc = new CommandQueueDesc
                {
                    Type = CommandListType.Direct,
                    Flags = CommandQueueFlags.AllowDynamicPriority,
                    Priority = (int)CommandQueuePriority.Normal,
                    NodeMask = 0
                };

                HResult hr = _device.CreateCommandQueue(&desc, out _directCmdQueue);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create direct command queue", hr.Value);
                }

                desc.Type = CommandListType.Compute;
                hr = _device.CreateCommandQueue(&desc, out _computeCmdQueue);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create compute command queue", hr.Value);
                }

                desc.Type = CommandListType.Copy;
                hr = _device.CreateCommandQueue(&desc, out _copyCmdQueue);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create compute copy queue", hr.Value);
                }
            }

            {
                D3D12MA.ALLOCATOR_DESC desc = new D3D12MA.ALLOCATOR_DESC
                {
                    pDevice = (ID3D12Device*)Unsafe.AsPointer(ref _device.Get()),
                    pAdapter = (IDXGIAdapter*)Unsafe.AsPointer(ref _adapter.Get()),
                    Flags = ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED | ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED,
                    pAllocationCallbacks = null,
                    PreferredBlockSize = 0
                };

                if (!_setup.UseTightAlignment)
                    desc.Flags |= ALLOCATOR_FLAG_DONT_USE_TIGHT_ALIGNMENT;

                D3D12MA.Allocator* ptr = null;
                HResult hr = D3D12MA.D3D12MA.CreateAllocator(&desc, &ptr);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create D3D12MA allocator", hr.Value);
                }

                _d3d12Allocator = ptr;
            }

            {
                // using ComPtr<IDXGIDevice4> dxgiDevice = _device.QueryInterface<IDXGIDevice4>();
                // if (dxgiDevice.Handle != null)
                // {
                //     try
                //     {
                //         _compositionDevice = new CompositionDevice((TerraFX.Interop.DirectX.IDXGIDevice4*)dxgiDevice.Handle);
                //     }
                //     catch (Exception ex)
                //     {
                //         EngLog.RHI.Error(ex, "Failed to create composition device");
                //     }
                // }
                // else
                // {
                //     EngLog.RHI.Information("Failed to get DXGI device! No composition will be available for any swap chains");
                // }
            }

            //Native
            {
                _nativeRep = (D3D12RHIDeviceNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIDeviceNative>());
                _nativeRep->Base = new RHIDeviceNative
                {

                };
                _nativeRep->Factory = (IDXGIFactory7*)Unsafe.AsPointer(ref _factory.Get());
                _nativeRep->Adapter = (IDXGIAdapter4*)Unsafe.AsPointer(ref _adapter.Get());
                _nativeRep->Debug = (ID3D12Debug6*)Unsafe.AsPointer(ref _debug.Get());
                _nativeRep->Device = (ID3D12Device14*)Unsafe.AsPointer(ref _device.Get());
                _nativeRep->InfoQueue = (ID3D12InfoQueue*)Unsafe.AsPointer(ref _infoQueue.Get());
                _nativeRep->InfoQueue1 = (ID3D12InfoQueue1*)Unsafe.AsPointer(ref _infoQueue1.Get());
                _nativeRep->DirectCmdQueue = (ID3D12CommandQueue*)Unsafe.AsPointer(ref _directCmdQueue.Get());
                _nativeRep->ComputeCmdQueue = (ID3D12CommandQueue*)Unsafe.AsPointer(ref _computeCmdQueue.Get());
                _nativeRep->CopyCmdQueue = (ID3D12CommandQueue*)Unsafe.AsPointer(ref _copyCmdQueue.Get());
                _nativeRep->D3D12MAllocator = _d3d12Allocator;
            }

            _uploadManager = new UploadManager(this);
            _resourceTracker = new ResourceTracker();

            _pendingFreeCallbacks = new ConcurrentQueue<(Action, ulong)>();

            _resoureFreeCts = new CancellationTokenSource();
            _resourceFreeEvent = new AutoResetEvent(false);
            _resourceFreeThread = new Thread(ResourceFreeProc);

            _debugMessageWidth = 0;
            _debugMessageData = null;

            _frameIndex = 0;
            _renderFrameIndex = 0;

            //_resourceFreeThread.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_videoBudgetThread.ThreadState == ThreadState.Running)
                {
                    _adapter.UnregisterVideoMemoryBudgetChangeNotification(_videoBudgetChangeCookie);
                    _videoBudgetCts.Cancel();

                    TerraFX.Interop.Windows.Windows.SetEvent(_videoBudgetChangeEvent);

                    _videoBudgetThread.Join();
                }

                _frameIndex = ulong.MaxValue;
                HandlePendingUpdates();

                if (_resourceFreeThread.ThreadState == ThreadState.Running)
                {
                    _resoureFreeCts.Cancel();
                    _resourceFreeEvent.Set();
                    _resourceFreeThread.Join();
                }

                _resoureFreeCts.Dispose();
                _resourceFreeEvent.Dispose();

                if (_nativeRep != null)
                {
                    NativeMemory.Free(_nativeRep);
                    _nativeRep = null;
                }

                _uploadManager.Dispose();

                _resourceTracker.PrintUnreleased();

                _compositionDevice?.Dispose();

                _d3d12Allocator->Base.Release();

                _copyCmdQueue.Dispose();
                _computeCmdQueue.Dispose();
                _directCmdQueue.Dispose();

                _infoQueue1.Dispose();
                _infoQueue.Dispose();
                _dredSettings.Dispose();

                _device.Dispose();
                _debug.Dispose();

                TerraFX.Interop.Windows.Windows.CloseHandle(_videoBudgetChangeEvent);
                _videoBudgetCts.Dispose();

                _adapter.Dispose();
                _factory.Dispose();

                _disposedValue = true;
            }
        }

        public override void HandlePendingUpdates()
        {
            if (!_pendingFreeCallbacks.IsEmpty)
            {
                if (_resourceFreeThread.ThreadState == ThreadState.Running)
                {
                    _resourceFreeEvent.Set();
                }
                else
                {
                    while (_pendingFreeCallbacks.TryPeek(out (Action, ulong) tuple))
                    {
                        if (tuple.Item2 <= _frameIndex)
                        {
                            Logger?.Debug("Freeing: {ac}{{ {targ} }} ({fr})", tuple.Item1.Method.Name, tuple.Item1.Target, tuple.Item2);

                            tuple.Item1();
                            _pendingFreeCallbacks.TryDequeue(out _);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            FlushPendingMessages();
            ++_frameIndex;
        }

        public override void IncrementFrame()
        {
            ++_renderFrameIndex;
        }

        public void UploadPendingData(ref ID3D12GraphicsCommandList10 cmds) => _uploadManager.UploadPending(ref cmds);

        public override RHIBuffer? CreateBuffer(in RHIBufferDescription description, ArrayPtr<byte> rawData, [CallerMemberName] string? debugName = "")
        {
            if (!BufferValidator.Validate(in description, _logger, debugName))
                return null;

            D3D12RHIBuffer buffer = new D3D12RHIBuffer(this, description);
            if (debugName != null)
                buffer.DebugName = debugName;

            if (!rawData.IsNullOrEmpty)
                _uploadManager.AddBufferUpload(buffer, rawData);

            _resourceTracker.Track(buffer);
            return buffer;
        }

        public override RHITexture? CreateTexture(in RHITextureDescription description, Span<ArrayPtr<byte>> planeSlices, [CallerMemberName] string? debugName = "")
        {
            if (!TextureValidator.Validate(in description, _logger, debugName))
                return null;

            D3D12RHITexture texture = new D3D12RHITexture(this, description);
            if (debugName != null)
                texture.DebugName = debugName;

            if (!planeSlices.IsEmpty)
            {
                for (int i = 0; i < planeSlices.Length; ++i)
                {
                    ArrayPtr<byte> rawData = planeSlices[i];
                    if (!rawData.IsNullOrEmpty)
                        _uploadManager.AddTextureUpload(texture, rawData, i);
                }
            }

            _resourceTracker.Track(texture);
            return texture;
        }

        public override RHISampler? CreateSampler(in RHISamplerDescription description, [CallerMemberName] string? debugName = "")
        {
            if (!SamplerValidator.Validate(in description, _logger, debugName))
                return null;

            D3D12RHISampler sampler = new D3D12RHISampler(this, description);
            if (debugName != null)
                sampler.DebugName = debugName;

            _resourceTracker.Track(sampler);
            return sampler;
        }

        public override RHISwapChain? CreateSwapChain(in RHISwapChainDescription description, [CallerMemberName] string? debugName = "")
        {
            if (!SwapChainValidator.Validate(in description, _logger, debugName))
                return null;

            D3D12RHISwapChain swapChain = new D3D12RHISwapChain(this, description);
            if (debugName != null)
                swapChain.DebugName = debugName;

            _resourceTracker.Track(swapChain);
            return swapChain;
        }

        public override RHIGraphicsPipeline? CreateGraphicsPipeline(in RHIGraphicsPipelineDescription description, in RHIGraphicsPipelineBytecode bytecode, [CallerMemberName] string? debugName = "")
        {
            if (!GraphicsPipelineValidator.Validate(in description, in bytecode, _logger, debugName))
                return null;

            D3D12RHIGraphicsPipeline graphicsPipeline = new D3D12RHIGraphicsPipeline(this, description, bytecode);
            if (debugName != null)
                graphicsPipeline.DebugName = debugName;

            _resourceTracker.Track(graphicsPipeline);
            return graphicsPipeline;
        }

        public override RHIComputePipeline? CreateComputePipeline(in RHIComputePipelineDescription description, in RHIComputePipelineBytecode bytecode, [CallerMemberName] string? debugName = "")
        {
            if (!ComputePipelineValidator.Validate(in description, in bytecode, _logger, debugName))
                return null;

            D3D12RHIComputePipeline computePipeline = new D3D12RHIComputePipeline(this, description, bytecode);
            if (debugName != null)
                computePipeline.DebugName = debugName;

            _resourceTracker.Track(computePipeline);
            return computePipeline;
        }

        public override void FlushPendingMessages()
        {
            if (!Unsafe.IsNullRef(in _infoQueue1.Get()))
            {
                _infoQueue1.ClearStoredMessages();
            }
            else if (!Unsafe.IsNullRef(in _infoQueue.Get()))
            {
                for (int i = 0; i < (int)_infoQueue.GetNumStoredMessages(); i++)
                {
                    uint length = 0;
                    if (new HResult(_infoQueue.GetMessageA((ulong)i, (Message*)null, (nuint*)&length)).IsFailure)
                        continue;

                    if (_debugMessageWidth < length)
                    {
                        if (_debugMessageData != null)
                            NativeMemory.Free(_debugMessageData);

                        _debugMessageWidth = (int)(length * 2);
                        _debugMessageData = NativeMemory.Alloc((nuint)_debugMessageWidth);
                    }

                    Message* message = (Message*)_debugMessageData;
                    if (new HResult(_infoQueue.GetMessageA((ulong)i, message, (nuint*)&length)).IsFailure)
                        continue;

                    string cat = message->Category.ToString();
                    string id = message->ID.ToString();
                    string desc = new string((sbyte*)message->PDescription, 0, (int)message->DescriptionByteLength);

                    switch (message->Severity)
                    {
                        case MessageSeverity.Corruption: _logger?.Fatal("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case MessageSeverity.Error: _logger?.Error("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case MessageSeverity.Warning: _logger?.Warning("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case MessageSeverity.Info: _logger?.Information("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case MessageSeverity.Message: _logger?.Debug("[{cat}/{id}]: {desc}", cat, id, desc); break;
                    }

                    switch (message->ID)
                    {
                        case MessageID.DeviceRemovalProcessATFault:
                            {
                                ReportDREDErrors();
                                break;
                            }
                    }
                }

                _infoQueue.ClearStoredMessages();
            }
        }

        public override RHIUsedMemoryInfo QueryUsedMemory()
        {
            D3D12MA.Budget local;
            D3D12MA.Budget nonLocal;

            D3D12MA.Allocator.GetBudget(_d3d12Allocator, &local, &nonLocal);

            return new RHIUsedMemoryInfo
            (
                 new RHIUsedMemoryBudget
                 (
                    (int)local.Stats.BlockCount,
                    (int)local.Stats.AllocationCount,
                    (long)local.Stats.BlockBytes,
                    (long)local.Stats.AllocationBytes,
                    (long)local.BudgetBytes,
                    (long)local.UsageBytes
                ),
                new RHIUsedMemoryBudget
                (
                    (int)nonLocal.Stats.BlockCount,
                    (int)nonLocal.Stats.AllocationCount,
                    (long)nonLocal.Stats.BlockBytes,
                    (long)nonLocal.Stats.AllocationBytes,
                    (long)nonLocal.BudgetBytes,
                    (long)nonLocal.UsageBytes
                )
            );
        }

        internal void AddResourceFreeNextFrame(Action callback)
        {
            _pendingFreeCallbacks.Enqueue((callback, _renderFrameIndex + 1));
        }

        internal void ReportDREDErrors()
        {
            ComPtr<ID3D12DeviceRemovedExtendedData1> data = new ComPtr<ID3D12DeviceRemovedExtendedData1>();
            if (new HResult(_device.QueryInterface(out data)).IsSuccess)
            {
                Logger?.Fatal("Uh-oh a GPU crash/hung has occured!");
                Logger?.Fatal("Dumping available DRED data:");

                {
                    DredAutoBreadcrumbsOutput1 breadcrumps;
                    if (new HResult(data.GetAutoBreadcrumbsOutput1(&breadcrumps)).IsSuccess)
                    {
                        Logger?.Fatal("    Breadcrumb data:");

                        AutoBreadcrumbNode1* node = breadcrumps.PHeadAutoBreadcrumbNode;
                        while (node != null)
                        {
                            Logger?.Fatal("        Command list debug name: {val} ({ptr:x8})", new string(node->PCommandListDebugNameW), (nint)node->PCommandList);
                            Logger?.Fatal("        Command queue debug name: {val} ({ptr:x8})", new string(node->PCommandQueueDebugNameW), (nint)node->PCommandQueue);
                            Logger?.Fatal("        Breadcrumbs:");

                            for (int i = 0; i < node->BreadcrumbCount; i++)
                            {
                                AutoBreadcrumbOp op = node->PCommandHistory[i];
                                Logger?.Error("            Executed: {ex}, Operation: {op}", *node->PLastBreadcrumbValue >= i, op);
                            }

                            if (node->BreadcrumbContextsCount > 0)
                            {
                                Logger?.Fatal("        Contexts:");

                                for (int i = 0; i < node->BreadcrumbContextsCount; i++)
                                {
                                    DredBreadcrumbContext context = node->PBreadcrumbContexts[i];
                                    Logger?.Fatal("            Index: {idx}, Context: {str}", context.BreadcrumbIndex, new string(context.PContextString));
                                }
                            }

                            node = node->PNext;
                        }
                    }
                    else
                        Logger?.Fatal("    No breadcrumbs");
                }

                {
                    DredPageFaultOutput1 pagefault;
                    if (new HResult(data.GetPageFaultAllocationOutput1(&pagefault)).IsSuccess)
                    {
                        Logger?.Fatal("    Pagefault data:");

                        Logger?.Fatal("        Pagefault virtual address: {val:x8}", pagefault.PageFaultVA);

                        Logger?.Fatal("        Existing allocations:");

                        DredAllocationNode1* node = pagefault.PHeadExistingAllocationNode;
                        while (node != null)
                        {
                            Logger?.Fatal("            Object name: {val}", new string(node->ObjectNameW));
                            Logger?.Fatal("            Allocation type: {val}", node->AllocationType);
                            Logger?.Fatal("            Object: {val:x8}", (nint)node->PObject);

                            node = node->PNext;
                        }

                        Logger?.Fatal("        Recent freed allocations:");

                        node = pagefault.PHeadRecentFreedAllocationNode;
                        while (node != null)
                        {
                            Logger?.Fatal("            Object name: {val}", new string(node->ObjectNameW));
                            Logger?.Fatal("            Allocation type: {val}", node->AllocationType);
                            Logger?.Fatal("            Object: {val:x8}", (nint)node->PObject);

                            node = node->PNext;
                        }
                    }
                    else
                        Logger?.Fatal("    No pagefaults");
                }
            }
            else
                Logger?.Fatal("No DRED data available!");

            data.Dispose();
        }

        private void VideoBudgetThreadProc()
        {
            TerraFX.Interop.Windows.Windows.WaitForSingleObject(_videoBudgetChangeEvent, TerraFX.Interop.Windows.Windows.INFINITE);

            while (!_videoBudgetCts.IsCancellationRequested)
            {
                QueryVideoMemoryInfo queryResult = default;
                HResult hr = _adapter.QueryVideoMemoryInfo(0, MemorySegmentGroup.Local, &queryResult);

                if (hr.IsFailure)
                {
                    _logger?.Debug("Failed to query for new video memory info!");
                }
                else
                {
                    _logger?.Debug("New video memory budget:\n    Budget: {v1}\n    Current usage: {v2}\n    Available for reservation: {v3}\n    Current reservation: {v4}",
                        FileUtility.FormatSize((long)queryResult.Budget, "f1", CultureInfo.InvariantCulture),
                        FileUtility.FormatSize((long)queryResult.CurrentUsage, "f1", CultureInfo.InvariantCulture),
                        FileUtility.FormatSize((long)queryResult.AvailableForReservation, "f1", CultureInfo.InvariantCulture),
                        FileUtility.FormatSize((long)queryResult.CurrentReservation, "f1", CultureInfo.InvariantCulture));
                }

                TerraFX.Interop.Windows.Windows.WaitForSingleObject(_videoBudgetChangeEvent, TerraFX.Interop.Windows.Windows.INFINITE);
            }
        }

        private void ResourceFreeProc()
        {
            Thread.CurrentThread.Name = "FreeResourcesThread";

            ulong requiredNextFrame = 0;
            while (!_resoureFreeCts.IsCancellationRequested)
            {
                _resourceFreeEvent.WaitOne();

                if (requiredNextFrame <= _frameIndex)
                {
                    while (_pendingFreeCallbacks.TryPeek(out (Action, ulong) tuple))
                    {
                        if (tuple.Item2 <= _frameIndex)
                        {
                            Logger?.Debug("Freeing: {ac}{{ {targ} }} ({fr})", tuple.Item1.Method.Name, tuple.Item1.Target, tuple.Item2);

                            tuple.Item1();
                            _pendingFreeCallbacks.TryDequeue(out _);
                        }
                        else
                        {
                            requiredNextFrame = tuple.Item2;
                            break;
                        }
                    }
                }
            }
        }

        public override unsafe RHIDeviceNative* GetAsNative() => (RHIDeviceNative*)_nativeRep;

        public ILogger? Logger => _logger;
        public D3D12RHISetup Setup => _setup;

        public ComPtr<IDXGIFactory7> Factory => _factory;

        public ComPtr<ID3D12Device14> Device => _device;
        public D3D12MA.Allocator* Allocator => _d3d12Allocator;

        public ComPtr<ID3D12CommandQueue> DirectCmdQueue => _directCmdQueue;
        public ComPtr<ID3D12CommandQueue> ComputeCmdQueue => _computeCmdQueue;
        public ComPtr<ID3D12CommandQueue> CopyCmdQueue => _copyCmdQueue;

        public bool HasPendingUploads => _uploadManager.HasPendingUploads;

        internal CompositionDevice? CompositionDevice => _compositionDevice;

        internal UploadManager UploadManager => _uploadManager;
        internal ResourceTracker ResourceTracker => _resourceTracker;

        public override RHIDeviceAPI DeviceAPI => RHIDeviceAPI.Direct3D12;

        private static MessageSeverity[] s_allowedSeverities = [
            MessageSeverity.Corruption,
            MessageSeverity.Error,
            MessageSeverity.Warning,
            //D3D12_MESSAGE_SEVERITY_INFO,
            MessageSeverity.Message,
            ];

        private static MessageID[] s_deniedIds = [
            MessageID.HeapAddressRangeIntersectsMultipleBuffers,
            MessageID.CleardepthstencilviewMismatchingclearvalue,
            MessageID.ClearrendertargetviewMismatchingclearvalue
            ];

        internal readonly static D3D12 D3D12 = D3D12.GetApi();
        internal readonly static DXGI DXGI = DXGI.GetApi(null);
    }

    public unsafe struct D3D12RHIDeviceNative
    {
        public RHIDeviceNative Base;

        public IDXGIFactory7* Factory;
        public IDXGIAdapter4* Adapter;

        public ID3D12Debug6* Debug;
        public ID3D12Device14* Device;

        public ID3D12InfoQueue* InfoQueue;
        public ID3D12InfoQueue1* InfoQueue1;

        public ID3D12CommandQueue* DirectCmdQueue;
        public ID3D12CommandQueue* ComputeCmdQueue;
        public ID3D12CommandQueue* CopyCmdQueue;

        public D3D12MA.Allocator* D3D12MAllocator;

        public static implicit operator RHIDeviceNative(D3D12RHIDeviceNative native) => native.Base;
    }

    public struct D3D12RHISetup
    {
        public bool UseTightAlignment;
    }
}
