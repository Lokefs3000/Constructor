using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.DXGI_ALPHA_MODE;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.DXGI_SCALING;
using static TerraFX.Interop.DirectX.DXGI_SWAP_CHAIN_FLAG;
using static TerraFX.Interop.DirectX.DXGI_SWAP_EFFECT;

using D3D12MA = Interop.D3D12MemAlloc;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    public unsafe sealed class D3D12RHISwapChain : RHISwapChain
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<IDXGISwapChain4> _swapChain;
        private D3D12RHISwapChainBuffer* _buffers;

        private int _activeBufferIndex;

        private D3D12RHISwapChainNative* _nativeRep;

        internal D3D12RHISwapChain(D3D12RHIDevice device, RHISwapChainDescription description)
        {
            _device = device;
            _description = description;

            {
                DXGI_SWAP_CHAIN_DESC1 desc = new DXGI_SWAP_CHAIN_DESC1
                {
                    Width = (uint)description.WindowSize.X,
                    Height = (uint)description.WindowSize.Y,
                    Format = description.BackBufferFormat.ToSwapChainFormat(),
                    Stereo = false,
                    SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                    BufferUsage = DXGI.DXGI_USAGE_BACK_BUFFER,
                    BufferCount = (uint)description.BackBufferCount,
                    Scaling = DXGI_SCALING_NONE,
                    SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
                    AlphaMode = DXGI_ALPHA_MODE_UNSPECIFIED,
                    Flags = (uint)DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING
                };

                using ComPtr<IDXGISwapChain1> swapChain = new ComPtr<IDXGISwapChain1>();
                HRESULT hr = device.Factory.Get()->CreateSwapChainForHwnd((IUnknown*)device.DirectCmdQueue.Get(), new HWND(description.WindowHandle.ToPointer()), &desc, null, null, swapChain.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create DXGI swap chain: {hr}");
                }

                hr = swapChain.Get()->QueryInterface(UuidOf.Get<IDXGISwapChain4>(), (void**)_swapChain.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to query DXGI swap chain 4: {hr}");
                }
            }

            _buffers = (D3D12RHISwapChainBuffer*)NativeMemory.AllocZeroed((nuint)description.BackBufferCount, (nuint)Unsafe.SizeOf<D3D12RHISwapChainBuffer>());

            for (int i = 0; i < description.BackBufferCount; i++)
            {
                ComPtr<ID3D12Resource2> resource = new ComPtr<ID3D12Resource2>();
                HRESULT hr = _swapChain.Get()->GetBuffer((uint)i, UuidOf.Get<ID3D12Resource2>(), (void**)resource.GetAddressOf());

                _buffers[i] = new D3D12RHISwapChainBuffer
                {
                    Resource = resource,

                    BarrierSync = D3D12_BARRIER_SYNC_DRAW,
                    BarrierAccess = D3D12_BARRIER_ACCESS_COMMON,
                    BarrierLayout = D3D12_BARRIER_LAYOUT_PRESENT
                };
            }

            _activeBufferIndex = (int)_swapChain.Get()->GetCurrentBackBufferIndex();

            {
                _nativeRep = (D3D12RHISwapChainNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHISwapChainNative>());
                _nativeRep->Base = new RHISwapChainNative
                {
                    Description = description,
                };
                _nativeRep->Buffers = _buffers;
                _nativeRep->ActiveBufferIndex = (int*)Unsafe.AsPointer(ref _activeBufferIndex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _device.AddResourceFreeNextFrame(() =>
                {
                    if (_nativeRep != null)
                        NativeMemory.Free(_nativeRep);
                    _nativeRep = null;

                    if (_buffers != null)
                    {
                        for (int i = 0; i < _description.BackBufferCount; i++)
                        {
                            _buffers[i].Resource.Reset();
                        }

                        NativeMemory.Free(_buffers);
                    }
                    _buffers = null;

                    _swapChain.Reset();
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (_buffers != null)
            {
                for (int i = 0; i < _description.BackBufferCount; i++)
                {
                    if (_buffers[i].Resource.Get() != null)
                    {
                        ResourceHelper.SetResourceName(_buffers[i].Resource.Get(), $"{debugName}-Tex{i}");
                    }
                }
            }
        }

        public override void Present()
        {
            DXGI_PRESENT_PARAMETERS @params = default;
            HRESULT hr = _swapChain.Get()->Present1(0, DXGI.DXGI_PRESENT_ALLOW_TEARING, &@params);

            if (hr.FAILED)
            {
                //TODO: exception handling
                throw new Exception(hr.ToString());
            }

            _activeBufferIndex = (int)_swapChain.Get()->GetCurrentBackBufferIndex();
        }

        public override void Resize(Vector2 newSize)
        {
            if (_buffers != null)
            {
                for (int i = 0; i < _description.BackBufferCount; i++)
                {
                    _buffers[i].Resource.Reset();
                }
            }

            IUnknown*[] queues = new IUnknown*[_description.BackBufferCount];
            for (int i = 0; i < queues.Length; i++)
                queues[i] = (IUnknown*)_device.DirectCmdQueue.Get();

            uint[] nodeMasks = new uint[_description.BackBufferCount];
            Array.Fill<uint>(nodeMasks, 0);

            fixed (IUnknown** ptr1 = queues)
            {
                fixed (uint* ptr2 = nodeMasks)
                {
                    HRESULT hr = _swapChain.Get()->ResizeBuffers1((uint)_description.BackBufferCount, (uint)newSize.X, (uint)newSize.Y, DXGI_FORMAT_UNKNOWN, DXGI.DXGI_PRESENT_ALLOW_TEARING, ptr2, ptr1);
                    if (hr.FAILED)
                    {
                        //TODO: exception handling
                        throw new Exception(hr.ToString());
                    }
                }
            }

            if (_buffers != null)
            {
                for (int i = 0; i < _description.BackBufferCount; i++)
                {
                    ComPtr<ID3D12Resource2> resource = new ComPtr<ID3D12Resource2>();
                    HRESULT hr = _swapChain.Get()->GetBuffer((uint)i, UuidOf.Get<ID3D12Resource2>(), (void**)resource.GetAddressOf());

                    _buffers[i] = new D3D12RHISwapChainBuffer
                    {
                        Resource = resource,

                        BarrierSync = D3D12_BARRIER_SYNC_DRAW,
                        BarrierAccess = D3D12_BARRIER_ACCESS_COMMON,
                        BarrierLayout = D3D12_BARRIER_LAYOUT_PRESENT
                    };
                }
            }

            _description.WindowSize = newSize;
            _nativeRep->Base.Description.WindowSize = newSize;
        }

        public override unsafe RHISwapChainNative* GetAsNative() => (RHISwapChainNative*)_nativeRep;
    }

    public unsafe struct D3D12RHISwapChainNative
    {
        public RHISwapChainNative Base;

        public D3D12RHISwapChainBuffer* Buffers;
        public int* ActiveBufferIndex;

        public static implicit operator RHISwapChainNative(D3D12RHISwapChainNative native) => native.Base;
    }

    public unsafe struct D3D12RHISwapChainBuffer
    {
        public ComPtr<ID3D12Resource2> Resource;

        public D3D12_BARRIER_SYNC BarrierSync;
        public D3D12_BARRIER_ACCESS BarrierAccess;
        public D3D12_BARRIER_LAYOUT BarrierLayout;
    }
}
