﻿using Primary.Common;
using Primary.RHI.Direct3D12.Descriptors;
using Primary.RHI.Direct3D12.Helpers;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Memory;
using Primary.RHI.Direct3D12.Utility;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Terra = TerraFX.Interop.DirectX;

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
        private Terra.D3D12MA_Allocation* _allocation;
        private DescriptorHeapAllocation _descriptor;
        private ResourceStates _defaultState;
        private Format _indexStrideFormat;

        //TODO: convert all to [ThreadStatic]!!
        private ResourceStates _currentState;

        private ulong _gpuVirtualAddress;

        private bool _disposedValue;

        internal BufferImpl(GraphicsDeviceImpl device, BufferDescription desc, nint dataPointer)
        {
            _device = device;
            _description = desc;

            _handle = GCHandle.Alloc(this, GCHandleType.Weak);
            _handlePtr = GCHandle.ToIntPtr(_handle);

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
                BufferUsage.VertexBuffer => ResourceStates.VertexAndConstantBuffer,
                BufferUsage.IndexBuffer => ResourceStates.IndexBuffer,
                BufferUsage.ConstantBuffer => ResourceStates.VertexAndConstantBuffer,
                BufferUsage.ShaderResource => ResourceStates.AllShaderResource,
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

            if (dataPointer != nint.Zero && (desc.Memory != MemoryUsage.Staging))
                usingState |= ResourceStates.CopyDest;

            //TODO: handle 0 size buffers
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
                    device.D3D12Device.CreateShaderResourceView(_resource, new ShaderResourceViewDescription
                    {
                        ViewDimension = ShaderResourceViewDimension.Buffer,
                        Format = Format.Unknown,
                        Shader4ComponentMapping = 0x1688,
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

        ~BufferImpl()
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
        internal void CopyToBuffer(ID3D12GraphicsCommandList7 commandList, BufferImpl dst, uint srcOffset, uint dstOffset, uint size)
        {
            commandList.CopyBufferRegion(dst._resource, dstOffset, _resource, srcOffset, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MappableCopyDataTo(ID3D12GraphicsCommandList7 commandList, ref SimpleMapInfo allocation)
        {
            commandList.CopyBufferRegion(_resource, allocation.DestinationOffset, allocation.Allocation.Buffer, allocation.Allocation.Offset, allocation.Allocation.Size);
        }
        #endregion

        public override ref readonly BufferDescription Description => ref _description;
        public override string Name { set => _resource.Name = value; }
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
    }
}
