using Primary.Common;
using Primary.RHI.Direct3D12.Utility;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;
using Terra = TerraFX.Interop.DirectX;

namespace Primary.RHI.Direct3D12.Memory
{
    internal unsafe sealed class UploadManager : IDisposable
    {
        private readonly GraphicsDeviceImpl _device;

        private List<SubUploadBuffer> _uploadBuffers;

        private uint _baselineUploadSize;

        private bool _disposedValue;

        internal UploadManager(GraphicsDeviceImpl device, uint startSize)
        {
            _device = device;

            _uploadBuffers = new List<SubUploadBuffer>
            {
                new SubUploadBuffer(device, startSize)
            };

            _baselineUploadSize = startSize;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                for (int i = 0; i < _uploadBuffers.Count; i++)
                {
                    _uploadBuffers[i].Dispose();
                }

                _uploadBuffers.Clear();

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ReleasePreviousBuffers()
        {
            if (_uploadBuffers.Count > 1)
            {
                int limit = _uploadBuffers.Count - 1;
                for (int i = 0; i < limit; i++)
                {
                    _uploadBuffers[i].Dispose();
                }

                _uploadBuffers.RemoveRange(0, limit);
            }

            _uploadBuffers[0].ReleaseStaleAllocations();
        }

        private (ID3D12Resource Resource, uint Offset) UploadToInternalAllocation(nint dataPointer, uint dataSize)
        {
            Terra.D3D12MA_VirtualAllocation allocation = new Terra.D3D12MA_VirtualAllocation();

            SubUploadBuffer uploadBuffer = _uploadBuffers[_uploadBuffers.Count - 1];
            nint mapped = uploadBuffer.Rent(&allocation, dataSize);

            if (mapped == nint.Zero)
            {
                while (_baselineUploadSize < dataSize) _baselineUploadSize *= 2;

                uploadBuffer = new SubUploadBuffer(_device, _baselineUploadSize);
                _uploadBuffers.Add(uploadBuffer);

                mapped = uploadBuffer.Rent(&allocation, dataSize);
                ExceptionUtility.Assert(mapped != nint.Zero);
            }

            NativeMemory.Copy(dataPointer.ToPointer(), mapped.ToPointer(), dataSize);
            uploadBuffer.Return(allocation);

            return (uploadBuffer.Resource, uploadBuffer.CalculateOffset(mapped));
        }

        private (ID3D12Resource Resource, uint Offset) UploadToInternalAllocationSliced(Span<nint> dataSlices, PlacedSubresourceFootPrint[] layouts, uint[] rows, ulong[] rowSizesInBytes, ulong totalSizeInBytes, FormatInfo info)
        {
            Terra.D3D12MA_VirtualAllocation allocation = new Terra.D3D12MA_VirtualAllocation();

            SubUploadBuffer uploadBuffer = _uploadBuffers[_uploadBuffers.Count - 1];
            nint mapped = uploadBuffer.Rent(&allocation, (uint)totalSizeInBytes);

            if (mapped == nint.Zero)
            {
                while (_baselineUploadSize < totalSizeInBytes) _baselineUploadSize *= 2;

                uploadBuffer = new SubUploadBuffer(_device, _baselineUploadSize);
                _uploadBuffers.Add(uploadBuffer);

                mapped = uploadBuffer.Rent(&allocation, (uint)totalSizeInBytes);
                ExceptionUtility.Assert(mapped != nint.Zero);
            }

            for (int i = 0; i < layouts.Length; i++)
            {
                ref PlacedSubresourceFootPrint footPrint = ref layouts[i];
                CopyToDestination(mapped + (nint)footPrint.Offset, dataSlices[i], (uint)info.CalculatePitch(footPrint.Footprint.Width), rows[i], footPrint.Footprint.RowPitch);
            }

            uploadBuffer.Return(allocation);
            return (uploadBuffer.Resource, uploadBuffer.CalculateOffset(mapped));

            void CopyToDestination(nint dataPointer, nint sourcePointer, uint srcRowPitch, uint row, ulong rowSizeInBytes)
            {
                byte* dstSlice = (byte*)dataPointer.ToPointer();
                byte* srcSlice = (byte*)sourcePointer.ToPointer();

                for (uint y = 0; y < row; y++)
                {
                    NativeMemory.Copy(srcSlice + srcRowPitch * y, dstSlice + rowSizeInBytes * y, (nuint)rowSizeInBytes);
                }
            }
        }

        //TODO: improve this by caching using the TLSF allocator and do it all in one big buffer at frame start
        internal void UploadToBuffer(ID3D12Resource resource, ResourceStates currentState, nint dataPointer, uint dataSize)
        {
            (ID3D12Resource srcRes, uint srcOffset) = UploadToInternalAllocation(dataPointer, dataSize);

            using ID3D12CommandAllocator commandAllocator = _device.D3D12Device.CreateCommandAllocator(CommandListType.Copy);
            using ID3D12GraphicsCommandList commandList = _device.D3D12Device.CreateCommandList<ID3D12GraphicsCommandList>(CommandListType.Copy, commandAllocator);

            commandList.CopyBufferRegion(resource, 0, srcRes, srcOffset, dataSize);
            commandList.ResourceBarrierTransition(resource, currentState, ResourceStates.Common);

            commandList.Close();

            //pov: killing all performance and parallel lmao
            _device.CopyCommandQueue.ExecuteCommandList(commandList);
            _device.SynchronizeDevice(SynchronizeDeviceTargets.Copy);
        }

        //TODO: refer above
        internal void UploadToTexture(ID3D12Resource resource, ResourceStates currentState, FormatInfo info, Span<nint> dataSlices, uint dataSize, ref ResourceDescription desc)
        {
            UInt3 size = new UInt3((uint)desc.Width, desc.Height, desc.Depth);
            uint mask = D3D12.TextureDataPitchAlignment - 1;

            PlacedSubresourceFootPrint[] dstLayouts = new PlacedSubresourceFootPrint[dataSlices.Length];
            uint[] dstRows = new uint[dataSlices.Length];
            ulong[] dstRowSizeInBytes = new ulong[dataSlices.Length];
            _device.D3D12Device.GetCopyableFootprints(resource.Description, 0, (uint)dataSlices.Length, 0, dstLayouts.AsSpan(), dstRows.AsSpan(), dstRowSizeInBytes.AsSpan(), out ulong totalBytes);

            uint* dataSizes = stackalloc uint[dataSlices.Length];
            for (int i = 0; i < dataSlices.Length; i++)
            {
                UInt3 mipSize = new UInt3(size.X >> i, size.Y >> i, size.Z);

                //uint byteWidth = mipSize.X * (uint)info.BytesPerPixel;
                //uint rowPitch = (uint)info.CalculatePitch(mipSize.X);//(uint)(byteWidth + (-byteWidth & mask));
                uint sliceDataSize = (uint)info.CalculateSize(mipSize.X, mipSize.Y, mipSize.Z);

                dataSizes[i] = sliceDataSize;
            }

            (ID3D12Resource srcRes, uint srcOffset) = UploadToInternalAllocationSliced(dataSlices, dstLayouts, dstRows, dstRowSizeInBytes, totalBytes, info);

            using ID3D12CommandAllocator commandAllocator = _device.D3D12Device.CreateCommandAllocator(CommandListType.Copy);
            using ID3D12GraphicsCommandList commandList = _device.D3D12Device.CreateCommandList<ID3D12GraphicsCommandList>(CommandListType.Copy, commandAllocator);

            uint totalDataOffset = srcOffset;
            for (int i = 0; i < dataSlices.Length; i++)
            {
                ref PlacedSubresourceFootPrint footPrint = ref dstLayouts[i];
                footPrint.Offset += srcOffset;

                commandList.CopyTextureRegion(new TextureCopyLocation(resource, (uint)i), Int3.Zero, new TextureCopyLocation(srcRes, footPrint));
            }

            commandList.Close();

            //pov: killing all performance and parallel lmao
            _device.CopyCommandQueue.ExecuteCommandList(commandList);
            _device.SynchronizeDevice(SynchronizeDeviceTargets.Copy);
        }
    }

    internal unsafe sealed class SubUploadBuffer : IDisposable
    {
        private readonly GraphicsDeviceImpl _device;

        private Stack<Terra.D3D12MA_VirtualAllocation> _allocations;

        private Terra.D3D12MA_VirtualBlock* _virtualBlock;
        private ID3D12Resource _resource;

        private nint _dataPointer;

        private bool _disposedValue;

        internal SubUploadBuffer(GraphicsDeviceImpl device, uint size)
        {
            GraphicsDeviceImpl.Logger.Information("Create new sub upload buffer with size: {sz}mb", size / (1024.0 * 1024.0));

            _device = device;

            _allocations = new Stack<Terra.D3D12MA_VirtualAllocation>();

            Terra.D3D12MA_VIRTUAL_BLOCK_DESC blockDesc = new Terra.D3D12MA_VIRTUAL_BLOCK_DESC
            {
                Flags = Terra.D3D12MA_VIRTUAL_BLOCK_FLAGS.D3D12MA_VIRTUAL_BLOCK_FLAG_NONE,
                Size = size,
                pAllocationCallbacks = null
            };

            Terra.D3D12MA_VirtualBlock* block = null;
            ResultChecker.ThrowIfUnhandled(Terra.D3D12MemAlloc.D3D12MA_CreateVirtualBlock(&blockDesc, &block).Value, device);
            _virtualBlock = block;

            ResourceDescription resDesc = new ResourceDescription
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = size,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.Unknown,
                SampleDescription = SampleDescription.Default,
                Layout = TextureLayout.RowMajor,
                Flags = ResourceFlags.None
            };

            ResultChecker.ThrowIfUnhandled(_device.D3D12Device.CreateCommittedResource(HeapProperties.UploadHeapProperties, HeapFlags.None, resDesc, ResourceStates.GenericRead, null, out _resource!), device);

            void* ptr = null;
            ResultChecker.ThrowIfUnhandled(_resource.Map(0, &ptr));

            _dataPointer = (nint)ptr;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_dataPointer != nint.Zero)
                    _resource.Unmap(0);
                _resource?.Dispose();
                if (_virtualBlock != null)
                    _virtualBlock->Release();

                _disposedValue = true;
            }
        }

        ~SubUploadBuffer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal nint Rent(Terra.D3D12MA_VirtualAllocation* allocation, uint size, uint alignment = 0)
        {
            Terra.D3D12MA_VIRTUAL_ALLOCATION_DESC allocDesc = new Terra.D3D12MA_VIRTUAL_ALLOCATION_DESC
            {
                Flags = Terra.D3D12MA_VIRTUAL_ALLOCATION_FLAGS.D3D12MA_VIRTUAL_ALLOCATION_FLAG_NONE,
                Size = size,
                Alignment = alignment,
                pPrivateData = null
            };

            ulong offset = 0;
            if (_virtualBlock->Allocate(&allocDesc, allocation, &offset).SUCCEEDED)
                return (nint)((ulong)_dataPointer + offset);
            else
                return nint.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Return(Terra.D3D12MA_VirtualAllocation allocation)
        {
            _allocations.Push(allocation);
        }

        internal void ReleaseStaleAllocations()
        {
            while (_allocations.TryPop(out Terra.D3D12MA_VirtualAllocation allocation))
            {
                _virtualBlock->FreeAllocation(allocation);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint CalculateOffset(nint ptr) => (uint)(ptr - _dataPointer);

        internal ID3D12Resource Resource => _resource;
    }
}
