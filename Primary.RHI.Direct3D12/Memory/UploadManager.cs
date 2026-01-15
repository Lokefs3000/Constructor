using Primary.Common;
using Primary.RHI.Direct3D12.Utility;
using SharpGen.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

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
            D3D12MemAlloc.VirtualAllocation allocation = new D3D12MemAlloc.VirtualAllocation();

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
            D3D12MemAlloc.VirtualAllocation allocation = new D3D12MemAlloc.VirtualAllocation();

            SubUploadBuffer uploadBuffer = _uploadBuffers[_uploadBuffers.Count - 1];

            uint requiredDstSize = 0;
            for (int i = 0; i < layouts.Length; i++)
            {
                requiredDstSize += layouts[i].Footprint.RowPitch * layouts[i].Footprint.Height * layouts[i].Footprint.Depth;
            }

            nint mappedPtr = uploadBuffer.Rent(&allocation, requiredDstSize);

            if (mappedPtr == nint.Zero)
            {
                while (_baselineUploadSize < totalSizeInBytes) _baselineUploadSize *= 2;

                uploadBuffer = new SubUploadBuffer(_device, _baselineUploadSize);
                _uploadBuffers.Add(uploadBuffer);

                mappedPtr = uploadBuffer.Rent(&allocation, (uint)totalSizeInBytes);
                Checking.Assert(mappedPtr != nint.Zero);
            }

            Span<byte> mapped = new Span<byte>(mappedPtr.ToPointer(), (int)requiredDstSize);

            for (int i = 0; i < layouts.Length; i++)
            {
                ref PlacedSubresourceFootPrint footPrint = ref layouts[i];

                int totalSrcSize = (int)info.CalculateSize(footPrint.Footprint.Width, footPrint.Footprint.Height, footPrint.Footprint.Depth);
                int totalDstSize = (int)(footPrint.Footprint.RowPitch * footPrint.Footprint.Height * footPrint.Footprint.Depth);

                Span<byte> src = new Span<byte>(dataSlices[i].ToPointer(), totalSrcSize);
                Span<byte> dst = mapped.Slice((int)footPrint.Offset, totalDstSize);

                CopyToDestination(src, dst, (uint)info.CalculatePitch(footPrint.Footprint.Width), rows[i], footPrint.Footprint.RowPitch);
            }

            uploadBuffer.Return(allocation);
            return (uploadBuffer.Resource, uploadBuffer.CalculateOffset(mappedPtr));

            void CopyToDestination(Span<byte> src, Span<byte> dst, uint srcRowPitch, uint row, ulong rowSizeInBytes)
            {
                for (uint y = 0; y < row; y++)
                {
                    Span<byte> srcSlice = src.Slice((int)(srcRowPitch * y), (int)srcRowPitch);
                    Span<byte> dstSlice = dst.Slice((int)(rowSizeInBytes * y), (int)rowSizeInBytes);

                    srcSlice.CopyTo(dstSlice);
                }
            }
        }

        //TODO: improve this by caching using the TLSF allocator and do it all in one big buffer at frame start
        internal void UploadToBuffer(ID3D12Resource resource, ResourceStates currentState, nint dataPointer, uint dataSize)
        {
            (ID3D12Resource srcRes, uint srcOffset) = UploadToInternalAllocation(dataPointer, dataSize);

            ID3D12CommandAllocator commandAllocator = _device.D3D12Device.CreateCommandAllocator(CommandListType.Copy);
            using ID3D12GraphicsCommandList commandList = _device.D3D12Device.CreateCommandList<ID3D12GraphicsCommandList>(CommandListType.Copy, commandAllocator);

            commandList.CopyBufferRegion(resource, 0, srcRes, srcOffset, dataSize);
            commandList.ResourceBarrierTransition(resource, currentState, ResourceStates.Common);

            commandList.Close();

            //pov: killing all performance and parallel lmao
            _device.CopyCommandQueue.ExecuteCommandList(commandList);
            _device.SynchronizeDevice(SynchronizeDeviceTargets.Copy);

            _device.EnqueueDataFree(() =>
            {
                commandList.Dispose();
                commandAllocator.Dispose();
            });
        }

        //TODO: refer above
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UploadToTexture(ID3D12Resource resource, ResourceStates currentState, FormatInfo info, Span<nint> dataSlices, uint dataSize, ref ResourceDescription desc)
        {
            const ResourceFlags ResourceFlags_UseTightAlignment = (ResourceFlags)0x400;

            if (FlagUtility.HasFlag(desc.Flags, ResourceFlags_UseTightAlignment))
                UploadToTexture_TightAlign(resource, currentState, info, dataSlices, dataSize, ref desc);
            else
                UploadToTexture_Fallback(resource, currentState, info, dataSlices, dataSize, ref desc);
        }

        private void UploadToTexture_TightAlign(ID3D12Resource resource, ResourceStates currentState, FormatInfo info, Span<nint> dataSlices, uint dataSize, ref ResourceDescription desc)
        {
            UInt3 size = new UInt3((uint)desc.Width, desc.Height, desc.Depth);

            ResourceAllocationInfo allocationInfo = _device.D3D12Device.GetResourceAllocationInfo(desc);
            uint mask = (uint)(allocationInfo.Alignment - 1);

            ResourceDescription1 desc1 = new ResourceDescription1
            {
                Dimension = desc.Dimension,
                Alignment = desc.Alignment,
                Width = desc.Width,
                Height = desc.Height,
                DepthOrArraySize = desc.DepthOrArraySize,
                MipLevels = desc.MipLevels,
                Format = desc.Format,
                SampleDescription = desc.SampleDescription,
                Layout = desc.Layout,
                Flags = desc.Flags,
                SamplerFeedbackMipRegion = default,
            };

            PlacedSubresourceFootPrint[] dstLayouts = new PlacedSubresourceFootPrint[dataSlices.Length];
            uint[] dstRows = new uint[dataSlices.Length];
            ulong[] dstRowSizeInBytes = new ulong[dataSlices.Length];
            _device.D3D12Device.GetCopyableFootprints1(ref desc1, 0, (uint)dataSlices.Length, 0, dstLayouts, dstRows, dstRowSizeInBytes, out ulong totalBytes);

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

            ID3D12CommandAllocator commandAllocator = _device.D3D12Device.CreateCommandAllocator(CommandListType.Copy);
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

            _device.EnqueueDataFree(() =>
            {
                commandList.Dispose();
                commandAllocator.Dispose();
            });
        }

        private void UploadToTexture_Fallback(ID3D12Resource resource, ResourceStates currentState, FormatInfo info, Span<nint> dataSlices, uint dataSize, ref ResourceDescription desc)
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

            ID3D12CommandAllocator commandAllocator = _device.D3D12Device.CreateCommandAllocator(CommandListType.Copy);
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

            _device.EnqueueDataFree(() =>
            {
                commandList.Dispose();
                commandAllocator.Dispose();
            });
        }
    }

    internal unsafe sealed class SubUploadBuffer : IDisposable
    {
        private readonly GraphicsDeviceImpl _device;

        private Stack<D3D12MemAlloc.VirtualAllocation> _allocations;

        private D3D12MemAlloc.VirtualBlock* _virtualBlock;
        private ID3D12Resource _resource;

        private nint _dataPointer;

        private bool _disposedValue;

        internal SubUploadBuffer(GraphicsDeviceImpl device, uint size)
        {
            GraphicsDeviceImpl.Logger.Information("Create new sub upload buffer with size: {sz}mb", size / (1024.0 * 1024.0));

            _device = device;

            _allocations = new Stack<D3D12MemAlloc.VirtualAllocation>();

            D3D12MemAlloc.VIRTUAL_BLOCK_DESC blockDesc = new D3D12MemAlloc.VIRTUAL_BLOCK_DESC
            {
                Flags = D3D12MemAlloc.VIRTUAL_BLOCK_FLAGS.VIRTUAL_BLOCK_FLAG_NONE,
                Size = size,
                pAllocationCallbacks = null
            };

            D3D12MemAlloc.VirtualBlock* block = null;
            ResultChecker.ThrowIfUnhandled(D3D12MemAlloc.D3D12MA.CreateVirtualBlock(&blockDesc, &block), device);
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
                while (_allocations.TryPop(out D3D12MemAlloc.VirtualAllocation allocation))
                    D3D12MemAlloc.VirtualBlock.FreeAllocation(_virtualBlock, allocation);

                if (_dataPointer != nint.Zero)
                    _resource.Unmap(0);
                _resource?.Dispose();
                if (_virtualBlock != null)
                    _virtualBlock->Base.Release();

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

        internal nint Rent(D3D12MemAlloc.VirtualAllocation* allocation, uint size, uint alignment = 0)
        {
            D3D12MemAlloc.VIRTUAL_ALLOCATION_DESC allocDesc = new D3D12MemAlloc.VIRTUAL_ALLOCATION_DESC
            {
                Flags = D3D12MemAlloc.VIRTUAL_ALLOCATION_FLAGS.VIRTUAL_ALLOCATION_FLAG_NONE,
                Size = size,
                Alignment = alignment,
                pPrivateData = null
            };

            ulong offset = 0;

            Result r = new Result(D3D12MemAlloc.VirtualBlock.Allocate(_virtualBlock, &allocDesc, allocation, &offset));
            ResultChecker.PrintIfUnhandled(r, _device);

            if (r.Success)
                return (nint)((ulong)_dataPointer + offset);
            else
                return nint.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Return(D3D12MemAlloc.VirtualAllocation allocation)
        {
            _allocations.Push(allocation);
        }

        internal void ReleaseStaleAllocations()
        {
            while (_allocations.TryPop(out D3D12MemAlloc.VirtualAllocation allocation))
            {
                D3D12MemAlloc.VirtualBlock.FreeAllocation(_virtualBlock, allocation);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint CalculateOffset(nint ptr) => (uint)(ptr - _dataPointer);

        internal ID3D12Resource Resource => _resource;
    }
}
