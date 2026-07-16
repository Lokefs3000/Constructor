using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe sealed class D3D12RHISwapChain : RHISwapChain
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<IDXGISwapChain4> _swapChain;
        private D3D12RHISwapChainBuffer* _buffers;

        private CompositionSurface? _compositionSurface;

        private int _activeBufferIndex;

        private Vector2? _pendingResize;

        private D3D12RHISwapChainNative* _nativeRep;

        internal D3D12RHISwapChain(D3D12RHIDevice device, RHISwapChainDescription description)
        {
            _device = device;
            _description = description;

            _pendingResize = null;

            if (description.EnableComposition && _device.CompositionDevice == null)
                throw new D3D12RHIException("Trying to create composited swap chain without a valid composition device!");

            {
                SwapChainDesc1 desc = new SwapChainDesc1
                {
                    Width = (uint)description.WindowSize.X,
                    Height = (uint)description.WindowSize.Y,
                    Format = description.BackBufferFormat.ToSwapChainFormat(),
                    Stereo = false,
                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                    BufferUsage = DXGI.UsageBackBuffer,
                    BufferCount = (uint)description.BackBufferCount,
                    Scaling = Scaling.None,
                    SwapEffect = SwapEffect.FlipDiscard,
                    AlphaMode = AlphaMode.Unspecified,
                    Flags = (uint)SwapChainFlag.AllowTearing
                };

                ComPtr<IDXGISwapChain1> swapChain = new ComPtr<IDXGISwapChain1>();
                HResult hr;

                if (description.EnableComposition)
                {
                    desc.AlphaMode = AlphaMode.Premultiplied;
                    hr = device.Factory.CreateSwapChainForComposition(device.DirectCmdQueue, &desc, ref Unsafe.NullRef<IDXGIOutput>(), ref swapChain);
                }
                else
                {
                    hr = device.Factory.CreateSwapChainForHwnd(device.DirectCmdQueue, description.WindowHandle, &desc, null, ref Unsafe.NullRef<IDXGIOutput>(), ref swapChain);
                }

                if (hr.IsFailure)
                {
                    swapChain.Dispose();
                    _device.FlushPendingMessages();
                    throw new D3D12RHIException($"Failed to create DXGI swap chain", hr.Value);
                }

                hr = swapChain.QueryInterface(out _swapChain);
                if (hr.IsFailure)
                {
                    swapChain.Dispose();
                    _device.FlushPendingMessages();
                    throw new D3D12RHIException($"Failed to query DXGI swap chain 4", hr.Value);
                }

                swapChain.Dispose();
            }

            if (description.EnableComposition)
            {
                try
                {
                    _compositionSurface = _device.CompositionDevice?.CreateSurface(description.WindowHandle.ToPointer(), (void*)_swapChain.Handle);
                }
                catch (Exception)
                {
                    _swapChain.Dispose();
                    throw;
                }
            }
            
            _buffers = (D3D12RHISwapChainBuffer*)NativeMemory.AllocZeroed((nuint)description.BackBufferCount, (nuint)Unsafe.SizeOf<D3D12RHISwapChainBuffer>());

            for (int i = 0; i < description.BackBufferCount; i++)
            {
                ComPtr<ID3D12Resource2> resource = new ComPtr<ID3D12Resource2>();
                HResult hr = _swapChain.GetBuffer((uint)i, out resource);

                _buffers[i] = new D3D12RHISwapChainBuffer
                {
                    Resource = resource,

                    BarrierSync = BarrierSync.Draw,
                    BarrierAccess = BarrierAccess.Common,
                    BarrierLayout = BarrierLayout.Present
                };
            }

            {
                _nativeRep = (D3D12RHISwapChainNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHISwapChainNative>());
                _nativeRep->Base = new RHISwapChainNative
                {
                    Description = description,
                };
                _nativeRep->SwapChain = (IDXGISwapChain4*)Unsafe.AsPointer(ref _swapChain.Get());
                _nativeRep->Buffers = _buffers;
                _nativeRep->ActiveBufferIndex = (int)_swapChain.GetCurrentBackBufferIndex();
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

                    _compositionSurface?.Dispose();
                    _compositionSurface = null;

                    if (_buffers != null)
                    {
                        for (int i = 0; i < _description.BackBufferCount; i++)
                        {
                            _buffers[i].Resource.Dispose();
                        }

                        NativeMemory.Free(_buffers);
                    }
                    _buffers = null;

                    _swapChain.Dispose();

                    _device.ResourceTracker.Untrack(this);
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
                    if (!Unsafe.IsNullRef(in _buffers[i].Resource.Get()))
                    {
                        ResourceHelper.SetResourceName(ref _buffers[i].Resource.Get(), $"{debugName}-Tex{i}");
                    }
                }
            }
        }

        public override void Present()
        {
            PresentParameters @params = default;
            HResult hr = _swapChain.Present1(0, DXGI.PresentAllowTearing, &@params);

            if (hr.IsFailure)
            {
                //TODO: exception handling
                _device.FlushPendingMessages();
                throw new D3D12RHIException("Failed to present", hr.Value);
            }

            _nativeRep->ActiveBufferIndex = (int)_swapChain.GetCurrentBackBufferIndex();
        }

        public override void Resize(Vector2 newSize)
        {
            _pendingResize = newSize;
        }

        public override void ResizeBuffersToNewSize()
        {
            if (!_pendingResize.HasValue)
                return;

            Vector2 newSize = _pendingResize.Value;
            _pendingResize = null;

            if (newSize == _nativeRep->Base.Description.WindowSize)
                return;

            if (_buffers != null)
            {
                for (int i = 0; i < _description.BackBufferCount; i++)
                {
                    _buffers[i].Resource.Dispose();
                }
            }

            IUnknown*[] queues = new IUnknown*[_description.BackBufferCount];
            for (int i = 0; i < queues.Length; i++)
                queues[i] = (IUnknown*)Unsafe.AsPointer(ref _device.DirectCmdQueue.Get());

            uint[] nodeMasks = new uint[_description.BackBufferCount];
            Array.Fill<uint>(nodeMasks, 0);

            fixed (IUnknown** ptr1 = queues)
            {
                fixed (uint* ptr2 = nodeMasks)
                {
                    HResult hr = _swapChain.ResizeBuffers1((uint)_description.BackBufferCount, (uint)newSize.X, (uint)newSize.Y, Format.FormatUnknown, (uint)SwapChainFlag.AllowTearing, ptr2, ptr1);
                    if (hr.IsFailure)
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
                    HResult hr = _swapChain.GetBuffer((uint)i, out resource);

                    _buffers[i] = new D3D12RHISwapChainBuffer
                    {
                        Resource = resource,

                        BarrierSync = BarrierSync.Draw,
                        BarrierAccess = BarrierAccess.Common,
                        BarrierLayout = BarrierLayout.Present
                    };
                }
            }

            _description.WindowSize = newSize;
            _nativeRep->Base.Description.WindowSize = newSize;

            _nativeRep->ActiveBufferIndex = (int)_swapChain.GetCurrentBackBufferIndex();
        }

        public override string ToString()
        {
            return $"RHISwapChain{{{_debugName}}}";
        }

        public ComPtr<IDXGISwapChain4> SwapChain => _swapChain;

        public bool HasPendingResize => _pendingResize.HasValue;

        public override unsafe RHISwapChainNative* GetAsNative() => (RHISwapChainNative*)_nativeRep;
    }

    public unsafe struct D3D12RHISwapChainNative
    {
        public RHISwapChainNative Base;

        public IDXGISwapChain4* SwapChain;

        public D3D12RHISwapChainBuffer* Buffers;
        public int ActiveBufferIndex;

        public static implicit operator RHISwapChainNative(D3D12RHISwapChainNative native) => native.Base;
    }

    public unsafe struct D3D12RHISwapChainBuffer
    {
        public ComPtr<ID3D12Resource2> Resource;

        public BarrierSync BarrierSync;
        public BarrierAccess BarrierAccess;
        public BarrierLayout BarrierLayout;
    }
}
