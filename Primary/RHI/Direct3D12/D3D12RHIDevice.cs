using Primary.Common;
using Primary.Memory.Native;
using Primary.RHI.Validation;
using Serilog;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static Interop.D3D12MemAlloc.ALLOCATOR_FLAGS;
using static TerraFX.Interop.DirectX.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.DirectX.D3D_SHADER_MODEL;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_QUEUE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_QUEUE_PRIORITY;
using static TerraFX.Interop.DirectX.D3D12_DRED_ENABLEMENT;
using static TerraFX.Interop.DirectX.D3D12_FEATURE;
using static TerraFX.Interop.DirectX.D3D12_MESSAGE_ID;
using static TerraFX.Interop.DirectX.D3D12_MESSAGE_SEVERITY;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_BINDING_TIER;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_HEAP_TIER;
using static TerraFX.Interop.DirectX.DXGI;
using static TerraFX.Interop.DirectX.DXGI_GPU_PREFERENCE;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;
using D3D12MA = Interop.D3D12MemAlloc;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe sealed class D3D12RHIDevice : RHIDevice
    {
        private ILogger? _logger;

        private CancellationTokenSource _videoBudgetCts;
        private HANDLE _videoBudgetChangeEvent;
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

            //DXGI
            {
                uint flags = 0;
                if (description.EnableValidation)
                    flags |= DXGI_CREATE_FACTORY_DEBUG;

                HRESULT hr = DirectX.CreateDXGIFactory2(flags, UuidOf.Get<IDXGIFactory7>(), (void**)_factory.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create DXGI factory with error: {hr.ToString()}");
                }
            }

            {
                HRESULT hr = _factory.Get()->EnumAdapterByGpuPreference(0, DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, UuidOf.Get<IDXGIAdapter4>(), (void**)_adapter.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to enumerate for a valid DXGI adapter: {hr.ToString()}");
                }
            }

            //Video budget
            fixed (uint* cookie = &_videoBudgetChangeCookie)
            {
                _videoBudgetCts = new CancellationTokenSource();
                _videoBudgetChangeEvent = Windows.CreateEventA(null, false, true, null);
                _videoBudgetThread = new Thread(VideoBudgetThreadProc) { IsBackground = true };
                _videoBudgetChangeCookie = 0;

                HRESULT hr = _adapter.Get()->RegisterVideoMemoryBudgetChangeNotificationEvent(_videoBudgetChangeEvent, cookie);
                if (hr.SUCCEEDED)
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
                HRESULT hr = DirectX.D3D12GetDebugInterface(UuidOf.Get<ID3D12Debug6>(), (void**)_debug.GetAddressOf());
                if (hr.SUCCEEDED)
                {
                    ID3D12Debug6* debug = _debug.Get();

                    debug->EnableDebugLayer();
                    debug->SetEnableAutoName(true);
                }
                else
                    _logger?.Warning("Failed to query D3D12 debug interface!");
            }

            {
                HRESULT hr = DirectX.D3D12CreateDevice((IUnknown*)_adapter.Get(), D3D_FEATURE_LEVEL_12_2, UuidOf.Get<ID3D12Device14>(), (void**)_device.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create D3D12 device: {hr.ToString()}");
                }
            }

            //Validate device features
            {
                ID3D12Device14* device = _device.Get();

                //AssertFeatureSupport<D3D12_FEATURE_DATA_SHADER_MODEL>(D3D12_FEATURE_SHADER_MODEL, (x) =>
                //{
                //    if (x.HighestShaderModel < D3D_SHADER_MODEL_6_6)
                //        throw new RHIException("Shader model 6.6 support not found!");
                //});

                AssertFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS>(D3D12_FEATURE_D3D12_OPTIONS, (x) =>
                {
                    if (x.ResourceBindingTier < D3D12_RESOURCE_BINDING_TIER_3)
                        throw new RHIException("Needs atleast resource binding tier 3!");
                    if (x.ResourceHeapTier < D3D12_RESOURCE_HEAP_TIER_2)
                        throw new RHIException("Needs atleast resource heap tier 3!");
                });

                void AssertFeatureSupport<T>(D3D12_FEATURE feature, Action<T> callback) where T : unmanaged
                {
                    T data = default;
                    if (device->CheckFeatureSupport(feature, &data, (uint)Unsafe.SizeOf<T>()).FAILED)
                        throw new RHIException($"Failed to query support for feature: {feature}");

                    callback(data);
                }
            }

            {
                if (_device.Get()->QueryInterface(UuidOf.Get<ID3D12InfoQueue1>(), (void**)_infoQueue1.GetAddressOf()).SUCCEEDED)
                {
                    //_infoQueue1.Get()->RegisterMessageCallback()
                }

                if (_device.Get()->QueryInterface(UuidOf.Get<ID3D12InfoQueue>(), (void**)_infoQueue.GetAddressOf()).SUCCEEDED)
                {
                    fixed (D3D12_MESSAGE_SEVERITY* ptr = s_allowedSeverities)
                    {
                        fixed (D3D12_MESSAGE_ID* ptr2 = s_deniedIds)
                        {
                            D3D12_INFO_QUEUE_FILTER filter = new D3D12_INFO_QUEUE_FILTER
                            {
                                AllowList = new D3D12_INFO_QUEUE_FILTER_DESC
                                {
                                    NumSeverities = (uint)s_allowedSeverities.Length,
                                    pSeverityList = ptr
                                },
                                DenyList = new D3D12_INFO_QUEUE_FILTER_DESC
                                {
                                    NumIDs = (uint)s_deniedIds.Length,
                                    pIDList = ptr2
                                }
                            };

                            _infoQueue.Get()->ClearStorageFilter();
                            _infoQueue.Get()->PushStorageFilter(&filter);
                        }
                    }

                    //_infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_CORRUPTION, true);
                    //_infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_ERROR, true);
                }

                if (_device.Get()->QueryInterface(UuidOf.Get<ID3D12DeviceRemovedExtendedDataSettings1>(), (void**)_dredSettings.GetAddressOf()).SUCCEEDED)
                {
                    ID3D12DeviceRemovedExtendedDataSettings1* settings = _dredSettings.Get();

                    settings->SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT_FORCED_ON);
                    settings->SetBreadcrumbContextEnablement(D3D12_DRED_ENABLEMENT_FORCED_ON);
                    settings->SetPageFaultEnablement(D3D12_DRED_ENABLEMENT_FORCED_ON);
                }
            }

            {
                D3D12_COMMAND_QUEUE_DESC desc = new D3D12_COMMAND_QUEUE_DESC
                {
                    Type = D3D12_COMMAND_LIST_TYPE_DIRECT,
                    Flags = D3D12_COMMAND_QUEUE_FLAG_NONE,
                    Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY_NORMAL,
                    NodeMask = 0
                };

                HRESULT hr = _device.Get()->CreateCommandQueue(&desc, UuidOf.Get<ID3D12CommandQueue>(), (void**)_directCmdQueue.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create direct command queue: {hr.ToString()}");
                }

                desc.Type = D3D12_COMMAND_LIST_TYPE_COMPUTE;
                hr = _device.Get()->CreateCommandQueue(&desc, UuidOf.Get<ID3D12CommandQueue>(), (void**)_computeCmdQueue.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create compute command queue: {hr.ToString()}");
                }

                desc.Type = D3D12_COMMAND_LIST_TYPE_COPY;
                hr = _device.Get()->CreateCommandQueue(&desc, UuidOf.Get<ID3D12CommandQueue>(), (void**)_copyCmdQueue.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create compute copy queue: {hr.ToString()}");
                }
            }

            {
                D3D12MA.ALLOCATOR_DESC desc = new D3D12MA.ALLOCATOR_DESC
                {
                    pDevice = (ID3D12Device*)_device.Get(),
                    pAdapter = (IDXGIAdapter*)_adapter.Get(),
                    Flags = ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED | ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED,
                    pAllocationCallbacks = null,
                    PreferredBlockSize = 0
                };

                D3D12MA.Allocator* ptr = null;
                HRESULT hr = D3D12MA.D3D12MA.CreateAllocator(&desc, &ptr);
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create D3D12MA allocator: {hr.ToString()}");
                }

                _d3d12Allocator = ptr;
            }

            //Native
            {
                _nativeRep = (D3D12RHIDeviceNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIDeviceNative>());
                _nativeRep->Base = new RHIDeviceNative
                {

                };
                _nativeRep->Factory = _factory.Get();
                _nativeRep->Adapter = _adapter.Get();
                _nativeRep->Debug = _debug.Get();
                _nativeRep->Device = _device.Get();
                _nativeRep->InfoQueue = _infoQueue.Get();
                _nativeRep->InfoQueue1 = _infoQueue1.Get();
                _nativeRep->DirectCmdQueue = _directCmdQueue.Get();
                _nativeRep->ComputeCmdQueue = _computeCmdQueue.Get();
                _nativeRep->CopyCmdQueue = _copyCmdQueue.Get();
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
                    _adapter.Get()->UnregisterVideoMemoryBudgetChangeNotification(_videoBudgetChangeCookie);
                    _videoBudgetCts.Cancel();

                    Windows.SetEvent(_videoBudgetChangeEvent);

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

                _d3d12Allocator->Base.Release();

                _copyCmdQueue.Reset();
                _computeCmdQueue.Reset();
                _directCmdQueue.Reset();

                _infoQueue1.Reset();
                _infoQueue.Reset();
                _dredSettings.Reset();

                _device.Reset();
                _debug.Reset();

                Windows.CloseHandle(_videoBudgetChangeEvent);
                _videoBudgetCts.Dispose();

                _adapter.Reset();
                _factory.Reset();

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

        public void UploadPendingData(ID3D12GraphicsCommandList10* cmds) => _uploadManager.UploadPending(cmds);

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
            if (_infoQueue1.Get() != null)
            {
                _infoQueue1.Get()->ClearStoredMessages();
            }
            else if (_infoQueue.Get() != null)
            {
                ID3D12InfoQueue* infoQueue = _infoQueue.Get();
                for (int i = 0; i < (int)infoQueue->GetNumStoredMessages(); i++)
                {
                    uint length = 0;
                    if (infoQueue->GetMessage((ulong)i, null, (nuint*)&length).FAILED)
                        continue;

                    if (_debugMessageWidth < length)
                    {
                        if (_debugMessageData != null)
                            NativeMemory.Free(_debugMessageData);

                        _debugMessageWidth = (int)(length * 2);
                        _debugMessageData = NativeMemory.Alloc((nuint)_debugMessageWidth);
                    }

                    D3D12_MESSAGE* message = (D3D12_MESSAGE*)_debugMessageData;
                    if (infoQueue->GetMessage((ulong)i, message, (nuint*)&length).FAILED)
                        continue;

                    string cat = message->Category.ToString().Substring(23);
                    string id = message->ID.ToString().Substring(17);
                    string desc = new string(message->pDescription, 0, (int)message->DescriptionByteLength);

                    switch (message->Severity)
                    {
                        case D3D12_MESSAGE_SEVERITY_CORRUPTION: _logger?.Fatal("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case D3D12_MESSAGE_SEVERITY_ERROR: _logger?.Error("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case D3D12_MESSAGE_SEVERITY_WARNING: _logger?.Warning("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case D3D12_MESSAGE_SEVERITY_INFO: _logger?.Information("[{cat}/{id}]: {desc}", cat, id, desc); break;
                        case D3D12_MESSAGE_SEVERITY_MESSAGE: _logger?.Debug("[{cat}/{id}]: {desc}", cat, id, desc); break;
                    }

                    switch (message->ID)
                    {
                        case D3D12_MESSAGE_ID_DEVICE_REMOVAL_PROCESS_AT_FAULT:
                            {
                                ReportDREDErrors();
                                break;
                            }
                    }
                }

                infoQueue->ClearStoredMessages();
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
            ComPtr<ID3D12DeviceRemovedExtendedData1> dataComPtr = new ComPtr<ID3D12DeviceRemovedExtendedData1>();
            if (_device.Get()->QueryInterface(UuidOf.Get<ID3D12DeviceRemovedExtendedData1>(), (void**)dataComPtr.GetAddressOf()).SUCCEEDED)
            {
                ID3D12DeviceRemovedExtendedData1* data = dataComPtr.Get();

                Logger?.Fatal("Uh-oh a GPU crash/hung has occured!");
                Logger?.Fatal("Dumping available DRED data:");

                {
                    D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT1 breadcrumps;
                    if (data->GetAutoBreadcrumbsOutput1(&breadcrumps).SUCCEEDED)
                    {
                        Logger?.Fatal("    Breadcrumb data:");

                        D3D12_AUTO_BREADCRUMB_NODE1* node = breadcrumps.pHeadAutoBreadcrumbNode;
                        while (node != null)
                        {
                            Logger?.Fatal("        Command list debug name: {val} ({ptr:x8})", new string(node->pCommandListDebugNameW), (nint)node->pCommandList);
                            Logger?.Fatal("        Command queue debug name: {val} ({ptr:x8})", new string(node->pCommandQueueDebugNameW), (nint)node->pCommandQueue);
                            Logger?.Fatal("        Breadcrumbs:");

                            for (int i = 0; i < node->BreadcrumbCount; i++)
                            {
                                D3D12_AUTO_BREADCRUMB_OP op = node->pCommandHistory[i];
                                Logger?.Error("            Executed: {ex}, Operation: {op}", *node->pLastBreadcrumbValue >= i, op);
                            }

                            if (node->BreadcrumbContextsCount > 0)
                            {
                                Logger?.Fatal("        Contexts:");

                                for (int i = 0; i < node->BreadcrumbContextsCount; i++)
                                {
                                    D3D12_DRED_BREADCRUMB_CONTEXT context = node->pBreadcrumbContexts[i];
                                    Logger?.Fatal("            Index: {idx}, Context: {str}", context.BreadcrumbIndex, new string(context.pContextString));
                                }
                            }

                            node = node->pNext;
                        }
                    }
                    else
                        Logger?.Fatal("    No breadcrumbs");
                }

                {
                    D3D12_DRED_PAGE_FAULT_OUTPUT1 pagefault;
                    if (data->GetPageFaultAllocationOutput1(&pagefault).SUCCEEDED)
                    {
                        Logger?.Fatal("    Pagefault data:");

                        Logger?.Fatal("        Pagefault virtual address: {val:x8}", pagefault.PageFaultVA);

                        Logger?.Fatal("        Existing allocations:");

                        D3D12_DRED_ALLOCATION_NODE1* node = pagefault.pHeadExistingAllocationNode;
                        while (node != null)
                        {
                            Logger?.Fatal("            Object name: {val}", new string(node->ObjectNameW));
                            Logger?.Fatal("            Allocation type: {val}", node->AllocationType);
                            Logger?.Fatal("            Object: {val:x8}", (nint)node->pObject);

                            node = node->pNext;
                        }

                        Logger?.Fatal("        Recent freed allocations:");

                        node = pagefault.pHeadRecentFreedAllocationNode;
                        while (node != null)
                        {
                            Logger?.Fatal("            Object name: {val}", new string(node->ObjectNameW));
                            Logger?.Fatal("            Allocation type: {val}", node->AllocationType);
                            Logger?.Fatal("            Object: {val:x8}", (nint)node->pObject);

                            node = node->pNext;
                        }
                    }
                    else
                        Logger?.Fatal("    No pagefaults");
                }
            }
            else
                Logger?.Fatal("No DRED data available!");
        }

        private void VideoBudgetThreadProc()
        {
            Windows.WaitForSingleObject(_videoBudgetChangeEvent, Windows.INFINITE);

            while (!_videoBudgetCts.IsCancellationRequested)
            {
                DXGI_QUERY_VIDEO_MEMORY_INFO queryResult = default;
                HRESULT hr = _adapter.Get()->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &queryResult);

                if (hr.FAILED)
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

                Windows.WaitForSingleObject(_videoBudgetChangeEvent, Windows.INFINITE);
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

        public ComPtr<IDXGIFactory7> Factory => _factory;

        public ComPtr<ID3D12Device14> Device => _device;
        public D3D12MA.Allocator* Allocator => _d3d12Allocator;

        public ComPtr<ID3D12CommandQueue> DirectCmdQueue => _directCmdQueue.Get();
        public ComPtr<ID3D12CommandQueue> ComputeCmdQueue => _computeCmdQueue.Get();
        public ComPtr<ID3D12CommandQueue> CopyCmdQueue => _copyCmdQueue.Get();

        public bool HasPendingUploads => _uploadManager.HasPendingUploads;

        internal UploadManager UploadManager => _uploadManager;
        internal ResourceTracker ResourceTracker => _resourceTracker;

        public override RHIDeviceAPI DeviceAPI => RHIDeviceAPI.Direct3D12;

        private static D3D12_MESSAGE_SEVERITY[] s_allowedSeverities = [
            D3D12_MESSAGE_SEVERITY_CORRUPTION,
            D3D12_MESSAGE_SEVERITY_ERROR,
            D3D12_MESSAGE_SEVERITY_WARNING,
            //D3D12_MESSAGE_SEVERITY_INFO,
            D3D12_MESSAGE_SEVERITY_MESSAGE,
            ];

        private static D3D12_MESSAGE_ID[] s_deniedIds = [
            D3D12_MESSAGE_ID_HEAP_ADDRESS_RANGE_INTERSECTS_MULTIPLE_BUFFERS,
            D3D12_MESSAGE_ID_CLEARDEPTHSTENCILVIEW_MISMATCHINGCLEARVALUE,
            D3D12_MESSAGE_ID_CLEARRENDERTARGETVIEW_MISMATCHINGCLEARVALUE
            ];
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
}
