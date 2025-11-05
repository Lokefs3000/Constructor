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

using Terra = TerraFX.Interop.DirectX;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class BufferImpl : Buffer, ICommandBufferMappable, ICommandBufferResource
    {
        private readonly GraphicsDeviceImpl _device;
        private BufferDescription _description;

        private string _resourceName;

        private GCHandle _handle;
        private nint _handlePtr;

        private ID3D12Resource _resource;
        private D3D12MemAlloc.Allocation* _allocation;
        private DescriptorHeapAllocation _descriptor;
        private ResourceStates _defaultState;
        private Format _indexStrideFormat;

        private Dictionary<DescriptorKey, DescriptorImpl> _allocatedDescriptors;

        //TODO: convert all to [ThreadStatic]!!
        private ResourceStates _currentState;

        private ulong _gpuVirtualAddress;

        private bool _disposedValue;

        internal BufferImpl(GraphicsDeviceImpl device, BufferDescription desc, nint dataPointer)
        {
            //somewhat unhappy with this
            Checking.Assert(desc.ByteWidth > 0, "Buffer byte width must be larger then 0 bytes");

            _device = device;
            _description = desc;

            _handle = GCHandle.Alloc(this, GCHandleType.Weak);
            _handlePtr = GCHandle.ToIntPtr(_handle);

            _allocatedDescriptors = new Dictionary<DescriptorKey, DescriptorImpl>();

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
                BufferUsage.VertexBuffer => ResourceStates.VertexAndConstantBuffer,
                BufferUsage.IndexBuffer => ResourceStates.IndexBuffer,
                BufferUsage.ConstantBuffer => ResourceStates.VertexAndConstantBuffer,
                BufferUsage.ShaderResource => ResourceStates.AllShaderResource,
                _ => ResourceStates.Common
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

            if (dataPointer != nint.Zero && (desc.Memory != MemoryUsage.Staging))
                usingState |= ResourceStates.CopyDest;

            ResourceDescription resDesc = new ResourceDescription
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = desc.ByteWidth,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.Unknown,
                SampleDescription = SampleDescription.Default,
                Layout = TextureLayout.RowMajor,
                Flags = ResourceFlags.None
            };

            if (desc.Usage == BufferUsage.ConstantBuffer)
            {
                uint mask = D3D12.ConstantBufferDataPlacementAlignment - 1;
                resDesc.Width = (ulong)(desc.ByteWidth + (-desc.ByteWidth & mask));
            }

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

            if (FlagUtility.HasFlag(desc.Usage, BufferUsage.ConstantBuffer) || FlagUtility.HasFlag(desc.Usage, BufferUsage.ShaderResource))
            {
                _descriptor = device.CpuSRVCBVUAVDescriptors.Rent(1);

                if (FlagUtility.HasFlag(desc.Usage, BufferUsage.ConstantBuffer))
                {
                    device.D3D12Device.CreateConstantBufferView(new ConstantBufferViewDescription
                    {
                        BufferLocation = _resource.GPUVirtualAddress,
                        SizeInBytes = (uint)resDesc.Width
                    }, _descriptor.GetCpuHandle());
                }
                else if (FlagUtility.HasFlag(desc.Usage, BufferUsage.ShaderResource))
                {
                    Checking.Assert(desc.Stride > 0 && desc.Stride <= desc.ByteWidth, $"Buffer stride must be more then 0 and less than the total byte width of the buffer (width: {desc.ByteWidth}, stride: {desc.Stride})");

                    //TODO: this is actually not good to use internal "resDesc" because its confusing on the other end
                    if ((resDesc.Width / desc.Stride) * desc.Stride != resDesc.Width)
                        GraphicsDeviceImpl.Logger.Warning("Buffer byte width is not evenly divisable by stride and will be rounded down (width: {w}, stride: {s})", resDesc.Width, desc.Stride);

                    device.D3D12Device.CreateShaderResourceView(_resource, new ShaderResourceViewDescription
                    {
                        ViewDimension = ShaderResourceViewDimension.Buffer,
                        Format = Format.Unknown,
                        Shader4ComponentMapping = Terra.D3D12.D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
                        Buffer = new BufferShaderResourceView
                        {
                            FirstElement = 0,
                            NumElements = (uint)(resDesc.Width / desc.Stride),
                            StructureByteStride = desc.Stride,
                            Flags = BufferShaderResourceViewFlags.None
                        }
                    }, _descriptor.GetCpuHandle());
                }
            }

            if (dataPointer != nint.Zero)
            {
                _device.UploadManager.UploadToBuffer(_resource, usingState, dataPointer, desc.ByteWidth);
                usingState = ResourceStates.Common;
            }

            _currentState = usingState;
            _gpuVirtualAddress = _resource.GPUVirtualAddress;
            _resourceName = _resource.Name;

            if (FlagUtility.HasFlag(desc.Usage, BufferUsage.IndexBuffer))
            {
                _indexStrideFormat = desc.Stride switch
                {
                    sizeof(ushort) => Format.R16_UInt,
                    sizeof(uint) => Format.R32_UInt,
                    _ => throw new ArgumentException(nameof(desc.Stride))
                };
            }

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

                    foreach (var kvp in _allocatedDescriptors)
                    {
                        kvp.Value.ForceDisposeEOL();
                    }

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

        ~BufferImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Public
        public override Descriptor AllocateDescriptor(BufferCBDescriptorDescription description)
        {
            DescriptorKey key = new DescriptorKey(description);
            if (_allocatedDescriptors.TryGetValue(key, out DescriptorImpl? descriptor))
            {
                descriptor.IncrementRefCount();
                return descriptor;
            }

            descriptor = new DescriptorImpl(this, key, description);
            descriptor.IncrementRefCount();

            _allocatedDescriptors.Add(key, descriptor);
            return descriptor;
        }

        public override Descriptor AllocateDescriptor(BufferSRDescriptorDescription description)
        {
            DescriptorKey key = new DescriptorKey(description);
            if (_allocatedDescriptors.TryGetValue(key, out DescriptorImpl? descriptor))
            {
                descriptor.IncrementRefCount();
                return descriptor;
            }

            descriptor = new DescriptorImpl(this, key, description);
            descriptor.IncrementRefCount();

            _allocatedDescriptors.Add(key, descriptor);
            return descriptor;
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
        internal void CopyToBuffer(ID3D12GraphicsCommandList7 commandList, BufferImpl dst, uint srcOffset, uint dstOffset, uint size)
        {
            commandList.CopyBufferRegion(dst._resource, dstOffset, _resource, srcOffset, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MappableCopyDataTo(ID3D12GraphicsCommandList7 commandList, ref SimpleMapInfo allocation)
        {
            commandList.CopyBufferRegion(_resource, allocation.DestinationOffset, allocation.Allocation.Buffer, allocation.Allocation.Offset, allocation.Allocation.Size);
        }

        internal void ReleaseDescriptor(DescriptorKey key)
        {
            _allocatedDescriptors.Remove(key);
        }
        #endregion

        public override ref readonly BufferDescription Description => ref _description;
        public override string Name
        {
            set
            {
                _resource.Name = value;
                _device.InvokeObjectRenamingTracking(this, value);
            }
        }
        public override nint Handle => _handlePtr;

        public ulong TotalSizeInBytes => _description.ByteWidth;

        internal ulong GPUVirtualAddress => _gpuVirtualAddress;
        internal Format IndexStrideFormat => _indexStrideFormat;

        public bool IsShaderVisible => !_descriptor.IsNull;
        public ResourceType Type => FlagUtility.HasFlag(_description.Usage, BufferUsage.ShaderResource) ? ResourceType.ShaderBuffer : ResourceType.ConstantBuffer;
        public string ResourceName => _resourceName;
        public CpuDescriptorHandle CpuDescriptor => _descriptor.GetCpuHandle();
        public ResourceStates GenericState => ResourceStates.Common;
        public ResourceStates CurrentState => _currentState;

        internal class DescriptorImpl : Descriptor, ICommandDescriptor
        {
            private readonly GraphicsDeviceImpl _device;
            private readonly BufferImpl _buffer;

            private readonly DescriptorKey _key;
            private readonly DescriptorDescription _description;

            private readonly ResourceType _bindType;
            private readonly bool _isDynamic;

            private DescriptorHeapAllocation _descriptorAlloc;

            private int _refCount;
            private bool _disposedValue;

            private DescriptorImpl(BufferImpl owner, DescriptorKey key, DescriptorDescription description)
            {
                _device = owner._device;
                _buffer = owner;

                _description = description;

                _descriptorAlloc = DescriptorHeapAllocation.Null;
            }

            internal DescriptorImpl(BufferImpl owner, DescriptorKey key, BufferCBDescriptorDescription description) : this(owner, key, new DescriptorDescription { BufferCB = description })
            {
                _bindType = ResourceType.ConstantBuffer;
                _isDynamic = FlagUtility.HasFlag(description.Flags, DescriptorFlags.Dynamic);

                if (!_isDynamic)
                {
                    _descriptorAlloc = _device.CpuSRVCBVUAVDescriptors.Rent(1);

                    //TODO: add validation to "description.ByteOffset"
                    _device.D3D12Device.CreateConstantBufferView(new ConstantBufferViewDescription
                    {
                        BufferLocation = owner._gpuVirtualAddress + description.ByteOffset,
                        SizeInBytes = description.SizeInBytes
                    }, _descriptorAlloc.GetCpuHandle());
                }
            }

            internal DescriptorImpl(BufferImpl owner, DescriptorKey key, BufferSRDescriptorDescription description) : this(owner, key, new DescriptorDescription { BufferSR = description })
            {
                _bindType = ResourceType.ShaderBuffer;
                _isDynamic = false;

                _descriptorAlloc = _device.CpuSRVCBVUAVDescriptors.Rent(1);
                _device.D3D12Device.CreateShaderResourceView(owner._resource, new ShaderResourceViewDescription
                {
                    Format = Format.Unknown,
                    ViewDimension = ShaderResourceViewDimension.Texture2D,
                    Shader4ComponentMapping = Terra.D3D12.D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
                    Buffer = new BufferShaderResourceView
                    {
                        FirstElement = description.FirstElement,
                        NumElements = description.NumberOfElements,
                        StructureByteStride = description.StructureByteStride,
                        Flags = BufferShaderResourceViewFlags.None
                    }
                }, _descriptorAlloc.GetCpuHandle());
            }

            private void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (Interlocked.Decrement(ref _refCount) != 0)
                        return;

                    _device.EnqueueDataFree(() =>
                    {
                        if (!_descriptorAlloc.IsNull)
                            _device.CpuSRVCBVUAVDescriptors.Return(_descriptorAlloc);

                        _buffer.ReleaseDescriptor(_key);
                    });

                    _disposedValue = true;
                }
            }

            public override void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            internal void ForceDisposeEOL()
            {
                if (_disposedValue)
                    return;

                if (!_descriptorAlloc.IsNull)
                    _device.CpuSRVCBVUAVDescriptors.Return(_descriptorAlloc);

                Interlocked.Exchange(ref _refCount, -1);
                _disposedValue = true;
            }

            internal void IncrementRefCount()
            {
                if (!_disposedValue)
                {
                    Interlocked.Increment(ref _refCount);
                }
            }

            public void AllocateDynamic(uint offset, CpuDescriptorHandle handle)
            {
                Debug.Assert(_isDynamic, "Descriptor must be dynamic in order to allow for \"AllocateDynamic\" call to be valid");
                Debug.Assert(_bindType == ResourceType.ConstantBuffer, "Descriptor must be constant buffer for \"AllocateDynamic\" to be valid");

                _device.D3D12Device.CreateConstantBufferView(new ConstantBufferViewDescription
                {
                    BufferLocation = _buffer._gpuVirtualAddress + offset,
                    SizeInBytes = _description.BufferCB.SizeInBytes
                }, handle);
            }

            public override ref readonly DescriptorDescription Description => ref _description;
            public override Resource Owner => _buffer;

            public CpuDescriptorHandle CpuDescriptor => _descriptorAlloc.GetCpuHandle();
            public ResourceType BindType => _bindType;
            public bool IsDynamic => _isDynamic;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal readonly struct DescriptorKey
        {
            [FieldOffset(0)]
            public readonly DescriptorKeyType Type;

            [FieldOffset(1)]
            public readonly BufferCBDescriptorDescription ConstantBuffer;

            [FieldOffset(1)]
            public readonly BufferSRDescriptorDescription ShaderResource;

            public DescriptorKey(BufferCBDescriptorDescription cb)
            {
                Type = DescriptorKeyType.ConstantBuffer;
                ConstantBuffer = cb;
            }

            public DescriptorKey(BufferSRDescriptorDescription sr)
            {
                Type = DescriptorKeyType.ShaderResource;
                ShaderResource = sr;
            }
        }

        internal enum DescriptorKeyType : byte
        {
            ConstantBuffer,
            ShaderResource
        }
    }
}
