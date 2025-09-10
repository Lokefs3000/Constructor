using Primary.Common;
using Primary.RHI.Direct3D12.Descriptors;
using Primary.RHI.Direct3D12.Helpers;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Memory;
using Primary.RHI.Direct3D12.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;
using Terra = TerraFX.Interop.DirectX;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class RenderTargetImpl : RenderTarget, ICommandBufferRT
    {
        private readonly GraphicsDeviceImpl _device;
        private RenderTargetDescription _description;

        private GCHandle _handle;
        private nint _handlePtr;

        private ID3D12Resource? _renderTexture;
        private Terra.D3D12MA_Allocation* _rtAllocation;
        private DescriptorHeapAllocation _rtDescriptor;

        private ID3D12Resource? _depthStencilTexture;
        private Terra.D3D12MA_Allocation* _dstAllocation;
        private DescriptorHeapAllocation _dstDescriptor;

        private RenderTextureViewImpl? _rtView;
        private RenderTextureViewImpl? _dstView;
        private RenderTextureViewStencilImpl? _stencilView;

        private ResourceStates _rtCurrentState;
        private ResourceStates _dstCurrentState;

        private bool _disposedValue;

        internal RenderTargetImpl(GraphicsDeviceImpl device, RenderTargetDescription desc)
        {
            _device = device;
            _description = desc;

            _handle = GCHandle.Alloc(this, GCHandleType.Weak);
            _handlePtr = GCHandle.ToIntPtr(_handle);

            _rtCurrentState = ResourceStates.RenderTarget;
            _dstCurrentState = ResourceStates.DepthWrite;

            if (desc.ColorFormat != RenderTargetFormat.Undefined)
            {
                ResourceDescription resDesc = new ResourceDescription
                {
                    Dimension = ResourceDimension.Texture2D,
                    Alignment = 0,
                    Width = (ulong)desc.Dimensions.Width,
                    Height = (uint)desc.Dimensions.Height,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = FormatConverter.Convert(desc.ColorFormat),
                    SampleDescription = SampleDescription.Default,
                    Layout = TextureLayout.Unknown,
                    Flags = ResourceFlags.AllowRenderTarget
                };

                Terra.D3D12MA_ALLOCATION_DESC allocDesc = new Terra.D3D12MA_ALLOCATION_DESC
                {
                    Flags = Terra.D3D12MA_ALLOCATION_FLAGS.D3D12MA_ALLOCATION_FLAG_NONE,
                    HeapType = Terra.D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,
                    ExtraHeapFlags = Terra.D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                    CustomPool = null,
                    pPrivateData = null,
                };

                Terra.D3D12MA_Allocation* alloc = null;
                Guid guid = typeof(ID3D12Resource).GUID;
                void* resPtr = null;

                ClearValue clearValue = new ClearValue(resDesc.Format, new Color4(0.0f, 0.0f, 0.0f));
                ResultChecker.ThrowIfUnhandled(device.D3D12MAAllocator->CreateResource(&allocDesc, (Terra.D3D12_RESOURCE_DESC*)&resDesc, Terra.D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET, (Terra.D3D12_CLEAR_VALUE*)&clearValue, &alloc, &guid, &resPtr).Value, device);

                _renderTexture = new ID3D12Resource((nint)resPtr);
                _rtAllocation = alloc;

                _rtDescriptor = device.CpuRTVDescriptors.Rent(1);
                device.D3D12Device.CreateRenderTargetView(_renderTexture, null, _rtDescriptor.GetCpuHandle());

                _rtView = new RenderTextureViewImpl(device, _renderTexture, resDesc.Format, _rtDescriptor, ResourceStates.RenderTarget, FlagUtility.HasFlag(desc.ShaderVisibility, RenderTargetVisiblity.Color), 0);
            }

            if (desc.DepthFormat != DepthStencilFormat.Undefined)
            {
                ResourceDescription resDesc = new ResourceDescription
                {
                    Dimension = ResourceDimension.Texture2D,
                    Alignment = 0,
                    Width = (ulong)desc.Dimensions.Width,
                    Height = (uint)desc.Dimensions.Height,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = FormatConverter.Convert(desc.DepthFormat),
                    SampleDescription = SampleDescription.Default,
                    Layout = TextureLayout.Unknown,
                    Flags = ResourceFlags.AllowDepthStencil
                };

                Terra.D3D12MA_ALLOCATION_DESC allocDesc = new Terra.D3D12MA_ALLOCATION_DESC
                {
                    Flags = Terra.D3D12MA_ALLOCATION_FLAGS.D3D12MA_ALLOCATION_FLAG_NONE,
                    HeapType = Terra.D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,
                    ExtraHeapFlags = Terra.D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                    CustomPool = null,
                    pPrivateData = null,
                };

                Terra.D3D12MA_Allocation* alloc = null;
                Guid guid = typeof(ID3D12Resource).GUID;
                void* resPtr = null;

                ClearValue clearValue = new ClearValue(resDesc.Format, 1.0f, 0xff);
                ResultChecker.ThrowIfUnhandled(device.D3D12MAAllocator->CreateResource(&allocDesc, (Terra.D3D12_RESOURCE_DESC*)&resDesc, Terra.D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_DEPTH_WRITE, FormatHelper.IsTypeless(resDesc.Format) ? null : (Terra.D3D12_CLEAR_VALUE*)&clearValue, &alloc, &guid, &resPtr).Value, device);

                _depthStencilTexture = new ID3D12Resource((nint)resPtr);
                _dstAllocation = alloc;

                _dstDescriptor = device.CpuDSVDescriptors.Rent(1);
                if (FormatHelper.IsTypeless(resDesc.Format))
                {

                    device.D3D12Device.CreateDepthStencilView(_depthStencilTexture, new DepthStencilViewDescription
                    {
                        Format = desc.DepthFormat switch
                        {
                            DepthStencilFormat.R32tX8X24ui => Format.D32_Float_S8X24_UInt,
                            DepthStencilFormat.R32t => Format.D32_Float,
                            DepthStencilFormat.R24tX8ui => Format.D24_UNorm_S8_UInt,
                            DepthStencilFormat.R16t => Format.D16_UNorm,
                            _ => throw new NotImplementedException(),
                        },
                        ViewDimension = DepthStencilViewDimension.Texture2D,
                        Flags = DepthStencilViewFlags.None,
                        Texture2D = new Texture2DDepthStencilView
                        {
                            MipSlice = 0,
                        }
                    }, _dstDescriptor.GetCpuHandle());
                }
                else
                {
                    device.D3D12Device.CreateDepthStencilView(_depthStencilTexture, null, _dstDescriptor.GetCpuHandle());
                }

                _dstView = new RenderTextureViewImpl(device, _depthStencilTexture, resDesc.Format, _dstDescriptor, ResourceStates.DepthWrite, FlagUtility.HasFlag(desc.ShaderVisibility, RenderTargetVisiblity.Depth), 1);
                if (desc.DepthFormat switch
                {
                    DepthStencilFormat.D32sfS8X24ui => true,
                    DepthStencilFormat.D24unS8ui => true,
                    DepthStencilFormat.R32tX8X24ui => true,
                    DepthStencilFormat.R24tX8ui => true,
                    _ => false
                })
                    _stencilView = new RenderTextureViewStencilImpl(device, _dstView, _depthStencilTexture, resDesc.Format, ResourceStates.DepthWrite, FlagUtility.HasFlag(desc.ShaderVisibility, RenderTargetVisiblity.Stencil));
            }

            _device.DumpMessageQueue();

            device.InvokeObjectCreationTracking(this);
            if (_renderTexture != null || _depthStencilTexture != null)
                device.InvokeObjectRenamingTracking(this, _renderTexture?.Name ?? _depthStencilTexture?.Name ?? throw new NullReferenceException());
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    _handle.Free();
                    _handlePtr = nint.Zero;

                    if (!_rtDescriptor.IsNull)
                        _device.CpuRTVDescriptors.Return(_rtDescriptor);
                    if (!_dstDescriptor.IsNull)
                        _device.CpuDSVDescriptors.Return(_dstDescriptor);

                    _rtView?.ReleaseInternal();
                    _dstView?.ReleaseInternal();

                    _renderTexture?.Dispose();
                    if (_rtAllocation != null)
                        _rtAllocation->Release();

                    _depthStencilTexture?.Dispose();
                    if (_dstAllocation != null)
                        _dstAllocation->Release();
                });

                _device.InvokeObjectDestructionTracking(this);
                _disposedValue = true;
            }
        }

        ~RenderTargetImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Interface
        public void EnsureResourceStates(ResourceBarrierManager manager, ResourceStates requiredStates, bool toggle = false)
        {
            throw new NotSupportedException();
            if (toggle)
            {
                if (_depthStencilTexture != null && (_dstCurrentState & requiredStates) != requiredStates)
                {
                    ResourceStates newState = _dstCurrentState | requiredStates;
                    manager.AddTransition(_depthStencilTexture, _dstCurrentState, newState, ref _dstCurrentState);
                }
            }
            else
            {
                if (_renderTexture != null && (_rtCurrentState & requiredStates) != requiredStates)
                {
                    ResourceStates newState = _rtCurrentState | requiredStates;
                    manager.AddTransition(_renderTexture, _rtCurrentState, newState, ref _rtCurrentState);
                }
            }
        }
        #endregion

        internal bool IsRTNull => _renderTexture == null;
        internal bool IsDSTNull => _depthStencilTexture == null;

        internal CpuDescriptorHandle RTDescriptorHandle => _rtDescriptor.GetCpuHandle();
        internal CpuDescriptorHandle DSTDescriptorHandle => _dstDescriptor.GetCpuHandle();

        public override ref readonly RenderTargetDescription Description => ref _description;
        public override string Name
        {
            set
            {
                if (_renderTexture != null)
                    _renderTexture.Name = value + ".RT";
                if (_depthStencilTexture != null)
                    _depthStencilTexture.Name = value + ".DST";

                _device.InvokeObjectRenamingTracking(this, value);
            }
        }

        public override RenderTextureView? ColorTexture => _rtView;
        public override RenderTextureView? DepthTexture => _dstView;
        public override RenderTextureView? StencilTexture => _stencilView;

        public override nint Handle => _handlePtr;

        ICommandBufferRTView? ICommandBufferRT.ColorTexture => _rtView;
        ICommandBufferRTView? ICommandBufferRT.DepthTexture => _dstView;

        public RenderTargetFormat RTFormat => _description.ColorFormat;
        public DepthStencilFormat DSTFormat => _description.DepthFormat;

        Vortice.Mathematics.Viewport ICommandBufferRT.Viewport => new Vortice.Mathematics.Viewport(0, 0, _description.Dimensions.Width, _description.Dimensions.Height);

        private class RenderTextureViewImpl : RenderTextureView, ICommandBufferRTView
        {
            private readonly GraphicsDeviceImpl _device;

            private GCHandle _handle;
            private nint _handlePtr;

            private ID3D12Resource _resource;
            private DescriptorHeapAllocation _allocation;
            private DescriptorHeapAllocation _srvAllocation;

            private ResourceStates _defaultState;

            private ResourceStates _currentState;

            private bool _isShaderVisible;
            private int _type;

            private string _resourceName;

            private bool _hasInitialClearCompleted;

            internal RenderTextureViewImpl(GraphicsDeviceImpl device, ID3D12Resource resource, Format format, DescriptorHeapAllocation allocation, ResourceStates state, bool isShaderVisible, int type)
            {
                _device = device;

                _handle = GCHandle.Alloc(this, GCHandleType.Weak);
                _handlePtr = GCHandle.ToIntPtr(_handle);

                _resource = resource;
                _allocation = allocation;
                _defaultState = state;

                _currentState = state;

                _isShaderVisible = isShaderVisible;
                _type = type;

                _resourceName = _resource.Name;

                _hasInitialClearCompleted = false;

                if (isShaderVisible)
                {
                    _srvAllocation = device.CpuSRVCBVUAVDescriptors.Rent(1);
                    if (FormatHelper.IsTypeless(format))
                    {
                        _device.D3D12Device.CreateShaderResourceView(_resource, new ShaderResourceViewDescription
                        {
                            Format = format switch
                            {
                                Format.R32G8X24_Typeless => type == 2 ? Format.X32_Typeless_G8X24_UInt : Format.R32_Float_X8X24_Typeless,
                                Format.R32_Typeless => Format.R32_Float,
                                Format.R24G8_Typeless => type == 2 ? Format.X24_Typeless_G8_UInt : Format.R24_UNorm_X8_Typeless,
                                Format.R16_Typeless => Format.R16_Float,
                                _ => throw new NotImplementedException()
                            },
                            ViewDimension = ShaderResourceViewDimension.Texture2D,
                            Shader4ComponentMapping = 0x1688,
                            Texture2D = new Texture2DShaderResourceView
                            {
                                MostDetailedMip = 0,
                                MipLevels = 1,
                                PlaneSlice = type == 2 ? 1u : 0,
                                ResourceMinLODClamp = 0.0f
                            }
                        }, _srvAllocation.GetCpuHandle());
                    }
                    else
                    {
                        _device.D3D12Device.CreateShaderResourceView(_resource, null, _srvAllocation.GetCpuHandle());
                    }
                }
            }

            internal void ReleaseInternal()
            {
                if (!_srvAllocation.IsNull)
                    _device.CpuSRVCBVUAVDescriptors.Return(_srvAllocation);
            }

            public override void Dispose() => throw new NotSupportedException("Cannot dispose \"RenderTextureView\"!");

            public void EnsureResourceStates(ResourceBarrierManager manager, ResourceStates requiredStates, bool toggle = false)
            {
                if (requiredStates != _currentState)
                {
                    ResourceStates newState = requiredStates;
                    manager.AddTransition(_resource, _currentState, newState, ref _currentState);
                }
            }

            public void SetImplicitResourcePromotion(ResourceStates state)
            {
                _currentState = state;
            }

            public void TransitionImmediate(ID3D12GraphicsCommandList7 commandList, ResourceStates newState)
            {
                if (_currentState != newState)
                    commandList.ResourceBarrierTransition(_resource, _currentState, newState);
                _currentState = newState;
            }

            public void CopyTexture(ID3D12GraphicsCommandList7 commandList, ICommandBufferRTView view)
            {
                commandList.CopyResource(view.Resource, _resource);
            }

            public override nint Handle => _handlePtr;

            public override string Name { set => _resource.Name = value; }

            public CpuDescriptorHandle ViewCpuDescriptor => _allocation.GetCpuHandle();
            public ID3D12Resource Resource => _resource;

            public override bool IsShaderVisible => _isShaderVisible;
            public ResourceType Type => ResourceType.Texture;
            public string ResourceName => _resourceName;
            public CpuDescriptorHandle CpuDescriptor => _srvAllocation.GetCpuHandle();
            public ResourceStates GenericState => _type == 1 ? ResourceStates.DepthWrite : ResourceStates.RenderTarget;
            public ResourceStates CurrentState => _currentState;

            //it is fine to leave with default thread safety as it only ever gets set to true
            public bool HasInitialClearCompleted
            {
                get => _hasInitialClearCompleted;
                set
                {
                    _hasInitialClearCompleted = value;
                }
            }
        }

        private class RenderTextureViewStencilImpl : RenderTextureView, ICommandBufferRTView
        {
            private readonly GraphicsDeviceImpl _device;
            private readonly RenderTextureViewImpl _dsv;

            private GCHandle _handle;
            private nint _handlePtr;

            private ID3D12Resource _resource;
            private DescriptorHeapAllocation _srvAllocation;

            private bool _isShaderVisible;

            internal RenderTextureViewStencilImpl(GraphicsDeviceImpl device, RenderTextureViewImpl dsv, ID3D12Resource resource, Format format, ResourceStates state, bool isShaderVisible)
            {
                _device = device;
                _dsv = dsv;

                _handle = GCHandle.Alloc(this, GCHandleType.Weak);
                _handlePtr = GCHandle.ToIntPtr(_handle);

                _resource = resource;
                _isShaderVisible = isShaderVisible;

                if (isShaderVisible)
                {
                    _srvAllocation = device.CpuSRVCBVUAVDescriptors.Rent(1);
                    if (FormatHelper.IsTypeless(format))
                    {
                        _device.D3D12Device.CreateShaderResourceView(resource, new ShaderResourceViewDescription
                        {
                            Format = format switch
                            {
                                Format.R32G8X24_Typeless => Format.X32_Typeless_G8X24_UInt,
                                Format.R24G8_Typeless => Format.X24_Typeless_G8_UInt,
                                _ => throw new NotImplementedException()
                            },
                            ViewDimension = ShaderResourceViewDimension.Texture2D,
                            Shader4ComponentMapping = 0x1688,
                            Texture2D = new Texture2DShaderResourceView
                            {
                                MostDetailedMip = 0,
                                MipLevels = 1,
                                PlaneSlice = 1,
                                ResourceMinLODClamp = 0.0f
                            }
                        }, _srvAllocation.GetCpuHandle());
                    }
                    else
                    {
                        _device.D3D12Device.CreateShaderResourceView(resource, null, _srvAllocation.GetCpuHandle());
                    }
                }
            }

            internal void ReleaseInternal()
            {
                if (!_srvAllocation.IsNull)
                    _device.CpuSRVCBVUAVDescriptors.Return(_srvAllocation);
            }

            public override void Dispose() => throw new NotSupportedException("Cannot dispose \"RenderTextureView\"!");

            public void EnsureResourceStates(ResourceBarrierManager manager, ResourceStates requiredStates, bool toggle = false)
                => _dsv.EnsureResourceStates(manager, requiredStates, toggle);

            public void SetImplicitResourcePromotion(ResourceStates state)
                => _dsv.SetImplicitResourcePromotion(state);

            public void TransitionImmediate(ID3D12GraphicsCommandList7 commandList, ResourceStates newState)
                => _dsv.TransitionImmediate(commandList, newState);

            public void CopyTexture(ID3D12GraphicsCommandList7 commandList, ICommandBufferRTView view)
                => _dsv.CopyTexture(commandList, view);

            public override nint Handle => _handlePtr;

            public override string Name { set => _resource.Name = value; }

            public CpuDescriptorHandle ViewCpuDescriptor => _dsv.ViewCpuDescriptor;
            public ID3D12Resource Resource => _dsv.Resource;

            public override bool IsShaderVisible => _isShaderVisible;
            public ResourceType Type => ResourceType.Texture;
            public string ResourceName => _dsv.ResourceName;
            public CpuDescriptorHandle CpuDescriptor => _srvAllocation.GetCpuHandle();
            public ResourceStates GenericState => _dsv.GenericState;
            public ResourceStates CurrentState => _dsv.CurrentState;

            //it is fine to leave with default thread safety as it only ever gets set to true
            public bool HasInitialClearCompleted
            {
                get => _dsv.HasInitialClearCompleted;
                set => _dsv.HasInitialClearCompleted = value;
            }
        }
    }
}
