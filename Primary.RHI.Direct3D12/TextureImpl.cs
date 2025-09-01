using Primary.Common;
using Primary.RHI.Direct3D12.Descriptors;
using Primary.RHI.Direct3D12.Helpers;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Memory;
using Primary.RHI.Direct3D12.Utility;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;
using Terra = TerraFX.Interop.DirectX;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class TextureImpl : Texture, ICommandBufferResource, ICommandBufferMappable
    {
        private readonly GraphicsDeviceImpl _device;
        private TextureDescription _description;

        private string _resourceName;

        private GCHandle _handle;
        private nint _handlePtr;

        private ulong _totalSizeInBytes;

        private ID3D12Resource _resource;
        private Terra.D3D12MA_Allocation* _allocation;
        private DescriptorHeapAllocation _descriptor;
        private ResourceStates _defaultState;

        //TODO: convert all to [ThreadStatic]!!
        private ResourceStates _currentState;

        private bool _disposedValue;

        internal TextureImpl(GraphicsDeviceImpl device, TextureDescription desc, Span<nint> dataPointer)
        {
            _device = device;
            _description = desc;

            _handle = GCHandle.Alloc(this, GCHandleType.Weak);
            _handlePtr = GCHandle.ToIntPtr(_handle);

            FormatInfo info = FormatStatistics.Query(desc.Format);
            _totalSizeInBytes = (ulong)info.BytesPerPixel * (ulong)(desc.Width * desc.Height * desc.Depth);

            HeapType heapType = desc.Memory switch
            {
                MemoryUsage.Default => HeapType.Default,
                MemoryUsage.Immutable => HeapType.Default,
                MemoryUsage.Dynamic => HeapType.Default,
                MemoryUsage.Staging => HeapType.Upload,
                _ => HeapType.Default
            };

            _defaultState = desc.Usage switch
            {
                TextureUsage.ShaderResource => ResourceStates.AllShaderResource,
                _ => ResourceStates.None
            };

            if (desc.Memory == MemoryUsage.Staging)
                _defaultState |= ResourceStates.GenericRead;

            if (FlagUtility.HasFlag(desc.CpuAccessFlags, CPUAccessFlags.Read))
            {
                heapType = HeapType.Readback;
                _defaultState |= ResourceStates.CopySource;
            }

            ResourceStates usingState = ResourceStates.Common;

            //if (desc.Memory == MemoryUsage.Dynamic || desc.Memory == MemoryUsage.Default /*TODO: consider if we should use a different strategy.*/)
            //    usingState |= ResourceStates.CopyDest;

            //if (dataPointer != nint.Zero && (desc.Memory != MemoryUsage.Staging))
            //    usingState |= ResourceStates.CopyDest;

            //TODO: handle empty texture (w0,h0,d0)
            ResourceDescription resDesc = new ResourceDescription
            {
                Dimension = desc.Dimension switch
                {
                    TextureDimension.Texture1D => ResourceDimension.Texture1D,
                    TextureDimension.Texture2D => ResourceDimension.Texture2D,
                    TextureDimension.Texture3D => ResourceDimension.Texture3D,
                    TextureDimension.TextureCube => ResourceDimension.Texture2D,
                    _ => throw new ArgumentException(nameof(desc.Dimension))
                },
                Alignment = 0,
                Width = desc.Width,
                Height = desc.Height,
                DepthOrArraySize = (ushort)desc.Depth,
                MipLevels = (ushort)desc.MipLevels,
                Format = FormatConverter.Convert(desc.Format),
                SampleDescription = SampleDescription.Default,
                Layout = TextureLayout.Unknown,
                Flags = ResourceFlags.None
            };

            Terra.D3D12MA_ALLOCATION_DESC allocDesc = new Terra.D3D12MA_ALLOCATION_DESC
            {
                Flags = Terra.D3D12MA_ALLOCATION_FLAGS.D3D12MA_ALLOCATION_FLAG_NONE,
                HeapType = (Terra.D3D12_HEAP_TYPE)heapType,
                ExtraHeapFlags = Terra.D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                CustomPool = null,
                pPrivateData = null,
            };

            Terra.D3D12MA_Allocation* ptr = null;
            void* outPtr = null;
            Guid guid = typeof(ID3D12Resource).GUID;

            ResultChecker.ThrowIfUnhandled(device.D3D12MAAllocator->CreateResource(&allocDesc, (Terra.D3D12_RESOURCE_DESC*)&resDesc, (Terra.D3D12_RESOURCE_STATES)usingState, null, &ptr, &guid, &outPtr).Value, device);

            _allocation = ptr;
            _resource = new ID3D12Resource((nint)outPtr);

            if (FlagUtility.HasFlag(desc.Usage, TextureUsage.ShaderResource))
            {
                _descriptor = device.CpuSRVCBVUAVDescriptors.Rent(1);
                device.D3D12Device.CreateShaderResourceView(_resource, new ShaderResourceViewDescription
                {
                    ViewDimension = ShaderResourceViewDimension.Texture2D,
                    Format = resDesc.Format,
                    Shader4ComponentMapping = Terra.D3D12.D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
                    Texture2D = new Texture2DShaderResourceView
                    {
                        MipLevels = desc.MipLevels,
                        MostDetailedMip = 0,
                        ResourceMinLODClamp = 0.0f,
                        PlaneSlice = 0
                    }
                }, _descriptor.GetCpuHandle());
            }

            if (!dataPointer.IsEmpty)
            {
                _device.UploadManager.UploadToTexture(_resource, usingState, info, dataPointer, (uint)_totalSizeInBytes, ref resDesc);
                usingState = ResourceStates.Common;
            }

            _currentState = usingState;
            _resourceName = _resource.Name;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    _handle.Free();
                    _handlePtr = nint.Zero;

                    if (!_descriptor.IsNull)
                        _device.CpuSRVCBVUAVDescriptors.Return(_descriptor);
                    if (_allocation != null)
                        _allocation->Release();
                    _resource?.Dispose();
                });

                _disposedValue = true;
            }
        }

        ~TextureImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Interface
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureResourceStates(ResourceBarrierManager manager, ResourceStates requiredStates, bool toggle = false)
        {
            if (toggle)
                requiredStates = _defaultState;
            if (_currentState != requiredStates)
            {
                ResourceStates newState = requiredStates;
                manager.AddTransition(_resource, _currentState, newState, ref _currentState);

                //GraphicsDeviceImpl.Logger.Information("{n}: {a} -> {b}", _resource.Name, _currentState, newState);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetImplicitResourcePromotion(ResourceStates state)
        {
            _currentState = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TransitionImmediate(ID3D12GraphicsCommandList7 commandList, ResourceStates newState)
        {
            if (_currentState != newState)
                commandList.ResourceBarrierTransition(_resource, _currentState, newState);
            _currentState = newState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MappableCopyDataTo(ID3D12GraphicsCommandList7 commandList, ref SimpleMapInfo allocation)
        {
            throw new NotImplementedException("i am yet to figure this shit out");
        }

        public void MappableCopyTextureDataTo(ID3D12GraphicsCommandList7 commandList, ref TextureMapInfo mapInfo)
        {
            Format format = FormatConverter.Convert(_description.Format);

            commandList.CopyTextureRegion(new TextureCopyLocation(_resource, mapInfo.Subresource), new Int3(mapInfo.Location.X, mapInfo.Location.Y, mapInfo.Location.Z), new TextureCopyLocation(mapInfo.Allocation.Buffer!, new PlacedSubresourceFootPrint
            {
                Offset = mapInfo.Allocation.Offset,
                Footprint = new SubresourceFootPrint(format, mapInfo.Location.Width, mapInfo.Location.Height, mapInfo.Location.Depth, mapInfo.RowPitch)
            }), new Box(0, 0, 0, (int)mapInfo.Location.Width, (int)mapInfo.Location.Height, (int)mapInfo.Location.Depth));
        }
        #endregion

        public override ref readonly TextureDescription Description => ref _description;
        public override string Name { set => _resource.Name = value; }
        public override nint Handle => _handlePtr;

        public ulong TotalSizeInBytes => _totalSizeInBytes;

        internal ulong GPUVirtualAddress => ulong.MinValue;

        public bool IsShaderVisible => !_descriptor.IsNull;
        public ResourceType Type => ResourceType.Texture;
        public string ResourceName => _resourceName;
        public CpuDescriptorHandle CpuDescriptor => _descriptor.GetCpuHandle();
        public ResourceStates GenericState => ResourceStates.Common;
        public ResourceStates CurrentState => _currentState;
    }
}
