using Primary.Common;
using Primary.RHI.Direct3D12.Descriptors;
using Primary.RHI.Direct3D12.Helpers;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Utility;
using SharpGen.Runtime;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class SwapChainImpl : SwapChain
    {
        private readonly GraphicsDeviceImpl _device;

        private uint _activeBackBuffer;

        private IDXGISwapChain4 _swapChain;
        private ID3D12Resource[] _backBuffers = Array.Empty<ID3D12Resource>();
        private DescriptorHeapAllocation[] _rtvCpuDescriptors = Array.Empty<DescriptorHeapAllocation>();

        private InternalRT _internalRt;
        private ResourceStates _currentBackBufferState;

        private Vector2 _clientSize;

        private bool _disposedValue;

        internal SwapChainImpl(GraphicsDeviceImpl device, Vector2 clientSize, nint windowHandle)
        {
            _device = device;
            _clientSize = clientSize;

            _activeBackBuffer = 0;

            try
            {
                using IDXGISwapChain1 swapChain1 = device.DXGIFactory.CreateSwapChainForHwnd(device.DirectCommandQueue, windowHandle, new SwapChainDescription1
                {
                    Width = (uint)clientSize.X,
                    Height = (uint)clientSize.Y,
                    Format = Format.R8G8B8A8_UNorm,
                    Stereo = false,
                    SampleDescription = SampleDescription.Default,
                    BufferUsage = Usage.Backbuffer,
                    BufferCount = 2,
                    Scaling = Scaling.None,
                    SwapEffect = SwapEffect.FlipDiscard,
                    AlphaMode = AlphaMode.Unspecified,
                    Flags = SwapChainFlags.AllowTearing
                });

                _swapChain = swapChain1.QueryInterface<IDXGISwapChain4>();
            }
            catch (SharpGenException ex)
            {
                ResultChecker.ThrowIfUnhandled(ex.ResultCode);
            }

            _device.DXGIFactory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAll);

            _backBuffers = new ID3D12Resource[2];
            _rtvCpuDescriptors = new DescriptorHeapAllocation[2];

            for (int i = 0; i < 2; i++)
            {
                _rtvCpuDescriptors[i] = device.CpuRTVDescriptors.Rent(1);

                ResultChecker.ThrowIfUnhandled(_swapChain!.GetBuffer((uint)i, out _backBuffers[i]!));
                _device.D3D12Device.CreateRenderTargetView(_backBuffers[i], null, _rtvCpuDescriptors[i].GetCpuHandle());

                _backBuffers[i].Name = $"SwapChain - BackBuffer[{i}]";
            }
        
            _currentBackBufferState = ResourceStates.Present;

            _internalRt = new InternalRT(_backBuffers[_activeBackBuffer])
            {
                CpuHandle = _rtvCpuDescriptors[_activeBackBuffer].GetCpuHandle(),
                BackBufferResourceState = _currentBackBufferState,
                ClientSize = clientSize
            };
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    _internalRt.ReleaseInternal();
                    foreach (DescriptorHeapAllocation allocation in _rtvCpuDescriptors)
                        _device.CpuRTVDescriptors.Return(allocation);
                    foreach (ID3D12Resource resource in _backBuffers)
                        resource.Dispose();
                    _swapChain?.Dispose();
                });

                _disposedValue = true;
            }
        }

        ~SwapChainImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Present(PresentParameters parameters)
        {
            _device.QueueSwapChainForPresentation(this, parameters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Resize(Vector2 newClientSize)
        {
            if (newClientSize != _clientSize)
                _device.EnqueueForResizeNextFrame(this, newClientSize);
            return true;
        }

        internal void ResizeInternal(Vector2 newClientSize)
        {
            if (newClientSize != _clientSize)
            {
                for (int i = 0; i < _backBuffers.Length; i++)
                {
                    _backBuffers[i].Dispose();
                }

                Result r = _swapChain.ResizeBuffers(2u, (uint)newClientSize.X, (uint)newClientSize.Y, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowTearing);
                if (r.Failure)
                    HandleSwapChainIssue(r);

                for (int i = 0; i < _backBuffers.Length; i++)
                {
                    ResultChecker.ThrowIfUnhandled(_swapChain.GetBuffer((uint)i, out _backBuffers[i]!), _device);
                    _device.D3D12Device.CreateRenderTargetView(_backBuffers[i], null, _rtvCpuDescriptors[i].GetCpuHandle());

                    _backBuffers[i].Name = $"SwapChain - BackBuffer[{i}]";
                }

                _activeBackBuffer = _swapChain.CurrentBackBufferIndex;

                _internalRt.Resource = _backBuffers[_activeBackBuffer];
                _internalRt.CpuHandle = _rtvCpuDescriptors[_activeBackBuffer].GetCpuHandle();
                _internalRt.ClientSize = newClientSize;

                _clientSize = newClientSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PresentInternal(PresentParameters parameters)
        {
            bool hasVSync = FlagUtility.HasFlag(parameters, PresentParameters.VSync);

            Result r;
            try
            {
                r = _swapChain.Present1(hasVSync ? 1u : 0u, hasVSync ? PresentFlags.None : PresentFlags.AllowTearing, new Vortice.DXGI.PresentParameters { });
            }
            catch (SharpGenException ex)
            {
                r = ex.ResultCode;
            }

            if (r.Failure)
            {
                HandleSwapChainIssue(r);
            }

            _activeBackBuffer = _swapChain.CurrentBackBufferIndex;

            _internalRt.Resource = _backBuffers[_activeBackBuffer];
            _internalRt.CpuHandle = _rtvCpuDescriptors[_activeBackBuffer].GetCpuHandle();
            //_internalRt.BackBufferResourceState = ResourceStates.Present;
            _internalRt.ClientSize = _clientSize;

            //_currentBackBufferState = ResourceStates.Present;
        }

        private void HandleSwapChainIssue(Result r)
        {
            _device.DumpMessageQueue();
            _device.DumpDREDOutput();
            ResultChecker.ThrowIfUnhandled(r);
        }

        public override Vector2 ClientSize => _clientSize;
        public override RenderTarget BackBuffer => _internalRt;

        private class InternalRT : RenderTarget, ICommandBufferRTView, ICommandBufferRT
        {
            private GCHandle _handle;
            private nint _handlePtr;

            public ID3D12Resource Resource;
            public CpuDescriptorHandle CpuHandle = CpuDescriptorHandle.Default;
            public ResourceStates BackBufferResourceState = ResourceStates.Present;
            public Vector2 ClientSize;

            public bool _hasInitialClearCompleted;

            internal InternalRT(ID3D12Resource resource)
            {
                _handle = GCHandle.Alloc(this, GCHandleType.Weak);
                _handlePtr = nint.Zero;

                Resource = resource;

                _hasInitialClearCompleted = false;
            }

            internal void ReleaseInternal()
            {
                _handle.Free();
                _handlePtr = nint.Zero;
            }

            public override void Dispose() => throw new NotSupportedException("Cannot dispose swapchain backbuffer!");

            public void EnsureResourceStates(ResourceBarrierManager manager, ResourceStates requiredStates, bool toggle = false)
            {
                if (BackBufferResourceState != requiredStates)
                {
                    ResourceStates newState = requiredStates;
                    manager.AddTransition(Resource, BackBufferResourceState, newState, ref BackBufferResourceState);

                    //GraphicsDeviceImpl.Logger.Information("{a} -> {b}", BackBufferResourceState, newState);
                }
            }

            public void SetImplicitResourcePromotion(ResourceStates state)
            {
                BackBufferResourceState = state;
            }

            public void TransitionImmediate(ID3D12GraphicsCommandList7 commandList, ResourceStates newState)
            {
                if (BackBufferResourceState != newState)
                    commandList.ResourceBarrierTransition(Resource, BackBufferResourceState, newState);
                BackBufferResourceState = newState;
            }

            public void CopyTexture(ID3D12GraphicsCommandList7 commandList, ICommandBufferRTView view) => throw new NotSupportedException();

            public override nint Handle => _handlePtr;

            public override ref readonly RenderTargetDescription Description => throw new NotImplementedException("Cannot access description for swapchain back buffer!");

            public override RenderTextureView? ColorTexture => null;
            public override RenderTextureView? DepthTexture => null;
            public override RenderTextureView? StencilTexture => null;

            public CpuDescriptorHandle ViewCpuDescriptor => CpuHandle;
            ID3D12Resource ICommandBufferRTView.Resource => Resource;

            public bool IsShaderVisible => false;
            public ResourceType Type => ResourceType.Texture;
            public string ResourceName => "SwapChain";
            public CpuDescriptorHandle CpuDescriptor => CpuDescriptorHandle.Default;
            public ResourceStates GenericState => ResourceStates.Present;
            public ResourceStates CurrentState => BackBufferResourceState;

            ICommandBufferRTView? ICommandBufferRT.ColorTexture => this;
            ICommandBufferRTView? ICommandBufferRT.DepthTexture => null;

            public RenderTargetFormat RTFormat => RenderTargetFormat.RGBA8un;
            public DepthStencilFormat DSTFormat => DepthStencilFormat.Undefined;

            public Vortice.Mathematics.Viewport Viewport => new Vortice.Mathematics.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            public bool HasInitialClearCompleted { get => _hasInitialClearCompleted; set => _hasInitialClearCompleted = value; }
            public override string Name { set => throw new NotSupportedException(); }
        }
    }
}
