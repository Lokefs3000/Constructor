using Primary.Common;
using Primary.RHI.Direct3D12.Descriptors;
using Primary.RHI.Direct3D12.Helpers;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Utility;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

using Terra = TerraFX.Interop.DirectX;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

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
        private D3D12MemAlloc.Allocation* _allocation;
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
                MemoryUsage.Readback => HeapType.Readback,
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

            const ResourceFlags ResourceFlags_UseTightAlignment = (ResourceFlags)0x400;

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
                Flags = ResourceFlags.None//ResourceFlags_UseTightAlignment
            };

            D3D12MemAlloc.ALLOCATION_DESC allocDesc = new D3D12MemAlloc.ALLOCATION_DESC
            {
                Flags = D3D12MemAlloc.ALLOCATION_FLAGS.ALLOCATION_FLAG_NONE,
                HeapType = (Terra.D3D12_HEAP_TYPE)heapType,
                ExtraHeapFlags = Terra.D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                CustomPool = null,
                pPrivateData = null,
            };

            D3D12MemAlloc.Allocation* ptr = null;
            void* outPtr = null;
            Guid guid = typeof(ID3D12Resource).GUID;
            
            ResultChecker.ThrowIfUnhandled(D3D12MemAlloc.Allocator.CreateResource(device.D3D12MAAllocator, &allocDesc, (Terra.D3D12_RESOURCE_DESC*)&resDesc, (Terra.D3D12_RESOURCE_STATES)usingState, null, &ptr, &guid, &outPtr), device);

            _allocation = ptr;
            _resource = new ID3D12Resource((nint)outPtr);

            if (FlagUtility.HasFlag(desc.Usage, TextureUsage.ShaderResource))
            {
                _descriptor = device.CpuSRVCBVUAVDescriptors.Rent(1);

                ShaderResourceViewDescription viewDesc = new ShaderResourceViewDescription
                {
                    ViewDimension = desc.Dimension switch
                    {
                        TextureDimension.Texture1D => ShaderResourceViewDimension.Texture1D,
                        TextureDimension.Texture2D => ShaderResourceViewDimension.Texture2D,
                        TextureDimension.Texture3D => ShaderResourceViewDimension.Texture3D,
                        TextureDimension.TextureCube => ShaderResourceViewDimension.TextureCube,
                    },
                    Format = resDesc.Format,
                    Shader4ComponentMapping = EncodeShader4ComponentMapping((uint)desc.Swizzle.R, (uint)desc.Swizzle.G, (uint)desc.Swizzle.B, (uint)desc.Swizzle.A),
                };

                switch (desc.Dimension)
                {
                    case TextureDimension.Texture1D:
                        {
                            viewDesc.Texture1D = new Texture1DShaderResourceView
                            {
                                MipLevels = desc.MipLevels,
                                MostDetailedMip = 0,
                                ResourceMinLODClamp = 0.0f,
                            };

                            break;
                        }
                    case TextureDimension.Texture2D:
                        {
                            viewDesc.Texture2D = new Texture2DShaderResourceView
                            {
                                MipLevels = desc.MipLevels,
                                MostDetailedMip = 0,
                                ResourceMinLODClamp = 0.0f,
                                PlaneSlice = 0
                            };

                            break;
                        }
                    case TextureDimension.Texture3D:
                        {
                            viewDesc.Texture3D = new Texture3DShaderResourceView
                            {
                                MipLevels = desc.MipLevels,
                                MostDetailedMip = 0,
                                ResourceMinLODClamp = 0.0f,
                            };

                            break;
                        }
                    case TextureDimension.TextureCube:
                        {
                            viewDesc.TextureCube = new TextureCubeShaderResourceView
                            {
                                MipLevels = desc.MipLevels,
                                MostDetailedMip = 0,
                                ResourceMinLODClamp = 0.0f,
                            };

                            break;
                        }
                }

                device.D3D12Device.CreateShaderResourceView(_resource, viewDesc, _descriptor.GetCpuHandle());
            }

            if (!dataPointer.IsEmpty)
            {
                _device.UploadManager.UploadToTexture(_resource, usingState, info, dataPointer, (uint)_totalSizeInBytes, ref resDesc);
                usingState = ResourceStates.Common;
            }

            _currentState = usingState;
            _resourceName = _resource.Name;

            device.InvokeObjectCreationTracking(this);
            device.InvokeObjectRenamingTracking(this, _resourceName);
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
                        _allocation->Base.Release();
                    _resource?.Dispose();
                });

                _device.InvokeObjectDestructionTracking(this);
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

        #region Public
        public override Descriptor AllocateDescriptor(TextureSRDescriptorDescription description)
        {
            throw new NotImplementedException();
        }
        #endregion
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
        public override string Name
        {
            set
            {
                _resource.Name = value;
                _device.InvokeObjectRenamingTracking(this, value);
            }
        }
        public override nint Handle => _handlePtr;

        public ulong TotalSizeInBytes => _totalSizeInBytes;

        internal ulong GPUVirtualAddress => ulong.MinValue;

        public bool IsShaderVisible => !_descriptor.IsNull;
        public ResourceType Type => ResourceType.Texture;
        public string ResourceName => _resourceName;
        public CpuDescriptorHandle CpuDescriptor => _descriptor.GetCpuHandle();
        public ResourceStates GenericState => ResourceStates.Common;
        public ResourceStates CurrentState => _currentState;

        //https://learn.microsoft.com/en-us/windows/win32/api/d3d12/ne-d3d12-d3d12_shader_component_mapping

        private const int ShaderComponentMappingMask = 0x7;
        private const int ShaderComponentMappingShift = 3;
        private const int ShaderComponentMappingAlwaysSetBitAvoidingZeroMemMistakes = 1 << (ShaderComponentMappingShift * 4);

        private static readonly uint DefaultShader4ComponentMapping = EncodeShader4ComponentMapping(0, 1, 2, 3);

        private static uint EncodeShader4ComponentMapping(uint src0, uint src1, uint src2, uint src3)
        {
            return ((((src0) & ShaderComponentMappingMask) |
                    (((src1) & ShaderComponentMappingMask) << ShaderComponentMappingShift) |
                    (((src2) & ShaderComponentMappingMask) << (ShaderComponentMappingShift * 2)) |
                    (((src3) & ShaderComponentMappingMask) << (ShaderComponentMappingShift * 3)) |
                    ShaderComponentMappingAlwaysSetBitAvoidingZeroMemMistakes));
        }
    }
}
