using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using System.Runtime.Versioning;
using Serilog;
using Primary.Common;

using static TerraFX.Interop.DirectX.DXGI;
using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.DXGI_GPU_PREFERENCE;
using static TerraFX.Interop.DirectX.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.DirectX.D3D12_FEATURE;
using static TerraFX.Interop.DirectX.D3D_SHADER_MODEL;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_BINDING_TIER;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_HEAP_TIER;
using static TerraFX.Interop.DirectX.D3D12_MESSAGE_SEVERITY;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_QUEUE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_QUEUE_PRIORITY;
using static Interop.D3D12MemAlloc.ALLOCATOR_FLAGS;

using D3D12MA = Interop.D3D12MemAlloc;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    public unsafe sealed class D3D12RHIDevice : RHIDevice
    {
        private D3D12RHIDeviceNative* _nativeRep;

        private ILogger? _logger;

        private ComPtr<IDXGIFactory7> _factory;
        private ComPtr<IDXGIAdapter4> _adapter;

        private ComPtr<ID3D12Debug6> _debug;
        private ComPtr<ID3D12Device14> _device;

        private ComPtr<ID3D12InfoQueue> _infoQueue;
        private ComPtr<ID3D12InfoQueue1> _infoQueue1;

        private ComPtr<ID3D12CommandQueue> _directCmdQueue;
        private ComPtr<ID3D12CommandQueue> _computeCmdQueue;
        private ComPtr<ID3D12CommandQueue> _copyCmdQueue;

        private D3D12MA.Allocator* _d3d12Allocator;

        internal D3D12RHIDevice(RHIDeviceDescription description, ILogger logger)
        {
            _nativeRep = (D3D12RHIDeviceNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIDeviceNative>());

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

                AssertFeatureSupport<D3D12_FEATURE_DATA_SHADER_MODEL>(D3D12_FEATURE_SHADER_MODEL, (x) =>
                {
                    if (x.HighestShaderModel < D3D_SHADER_MODEL_6_6)
                        throw new RHIException("Shader model 6.6 support not found!");
                });

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
                        D3D12_INFO_QUEUE_FILTER filter = new D3D12_INFO_QUEUE_FILTER
                        {
                            AllowList = new D3D12_INFO_QUEUE_FILTER_DESC
                            {
                                NumSeverities = (uint)s_allowedSeverities.Length,
                                pSeverityList = ptr
                            },
                            DenyList = new D3D12_INFO_QUEUE_FILTER_DESC
                            {

                            }
                        };

                        _infoQueue.Get()->PushStorageFilter(&filter);
                    }
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
                    Flags = ALLOCATOR_FLAG_NONE,
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
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _d3d12Allocator->Base.Release();

                _copyCmdQueue.Reset();
                _computeCmdQueue.Reset();
                _directCmdQueue.Reset();

                _infoQueue1.Reset();
                _infoQueue.Reset();

                _device.Reset();
                _debug.Reset();

                _adapter.Reset();
                _factory.Reset();

                if (_nativeRep != null)
                {
                    NativeMemory.Free(_nativeRep);
                    _nativeRep = null;
                }

                _disposedValue = true;
            }
        }

        public override RHIBuffer CreateBuffer(in RHIBufferDescription description, string? debugName = null)
        {
            throw new NotImplementedException();
        }

        internal void AddResourceFreeNextFrame(Action callback)
        {
            throw new NotImplementedException();
        }

        public override unsafe RHIDeviceNative* GetAsNative() => (RHIDeviceNative*)_nativeRep;

        public ComPtr<ID3D12Device14> Device => _device;
        public D3D12MA.Allocator* Allocator => _d3d12Allocator;

        private static D3D12_MESSAGE_SEVERITY[] s_allowedSeverities = [
            D3D12_MESSAGE_SEVERITY_CORRUPTION,
            D3D12_MESSAGE_SEVERITY_ERROR,
            D3D12_MESSAGE_SEVERITY_WARNING,
            D3D12_MESSAGE_SEVERITY_INFO,
            ];
    }

    public struct D3D12RHIDeviceNative
    {
        public RHIDeviceNative Base;
    }
}
