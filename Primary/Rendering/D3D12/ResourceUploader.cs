using Primary.Rendering.Pass;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class ResourceUploader : IDisposable
    {
        private readonly NRDDevice _device;

        private D3D12MemAlloc.Allocation* _uploadAllocation;
        private ComPtr<ID3D12Resource2> _uploadResource;

        private int _uploadResourceSize;
        private bool _needsNewBarrier;

        private nint _mappedResourcePtr;

        private List<DeferredUploadData> _pendingUploads;

        private bool _disposedValue;

        internal ResourceUploader(NRDDevice device)
        {
            _device = device;

            _uploadAllocation = null;
            _uploadResource = null;

            _uploadResourceSize = 0;

            _mappedResourcePtr = nint.Zero;

            _pendingUploads = new List<DeferredUploadData>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (!Unsafe.IsNullRef(in _uploadResource))
                {
                    _uploadResource.Dispose();
                    _uploadResource = default;
                }

                if (_uploadAllocation != null)
                    _uploadAllocation->Base.Release();

                _uploadAllocation = null;
                _uploadResource = null;

                _disposedValue = true;
            }
        }

        ~ResourceUploader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void PrepareUploadBuffers(FrameGraphResources resources)
        {
            if (_uploadResourceSize < resources.MinUploadSize)
            {
                _uploadResourceSize = resources.MinUploadSize;

                if (_mappedResourcePtr != nint.Zero)
                    _uploadResource.Unmap(0, (Silk.NET.Direct3D12.Range*)null);

                if (!Unsafe.IsNullRef(in _uploadResource))
                    _uploadResource.Dispose();
                if (_uploadAllocation != null)
                    _uploadAllocation->Base.Release();
                
                _mappedResourcePtr = nint.Zero;
                _uploadAllocation = null;
                _uploadResource = null;

                D3D12MemAlloc.ALLOCATION_DESC allocDesc = new D3D12MemAlloc.ALLOCATION_DESC
                {
                    HeapType = HeapType.Upload,
                };

                ResourceDesc1 resDesc = new ResourceDesc1
                {
                    Dimension = ResourceDimension.Buffer,
                    Alignment = 0,
                    Width = (ulong)_uploadResourceSize,
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = Format.FormatUnknown,
                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                    Layout = TextureLayout.LayoutRowMajor,
                    Flags = Unsafe.As<D3D12RHIDevice>(_device.RHIDevice).Setup.UseTightAlignment ? ResourceFlags.UseTightAlignment : ResourceFlags.None,
                    SamplerFeedbackMipRegion = new MipRegion
                    {
                        Width = 0,
                        Height = 0,
                        Depth = 0,
                    }
                };

                D3D12MemAlloc.Allocation* ptr1 = null;
                ComPtr<ID3D12Resource2> resource = new ComPtr<ID3D12Resource2>();

                HResult r = D3D12MemAlloc.Allocator.CreateResource3(_device.Allocator, &allocDesc, &resDesc, BarrierLayout.Undefined, null, 0, null, &ptr1, SilkMarshal.GuidPtrOf<ID3D12Resource2>(), (void**)resource.GetAddressOf());

                if (r.IsFailure)
                {
                    _device.RHIDevice.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                ResourceHelper.SetResourceName(ref resource.Get(), "NRDUploadBuffer");

                _uploadAllocation = ptr1;
                _uploadResource = resource;

                void* mapPtr = null;
                r = _uploadResource.Map(0, (Silk.NET.Direct3D12.Range*)null, &mapPtr);

                if (r.IsFailure)
                {
                    _device.RHIDevice.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                _needsNewBarrier = true;
                _mappedResourcePtr = (nint)mapPtr;
            }
        }

        internal void UploadBuffer(ref ID3D12GraphicsCommandList10 cmdList, FrameGraphResources resources, int index, nint dataPtr, int dataSize, int dataOffset)
        {
            if (_needsNewBarrier)
            {
                _device.BarrierManager.AddBufferBarrier(ref _uploadResource.Get(), BarrierSync.Copy, BarrierAccess.CopySource);
                _needsNewBarrier = false;
            }

            ref readonly FGResourceUpload upload = ref resources.Uploads[index];
            NRDResource buffer = ResourceUtility.AsNRDResource(upload.Resource);

            NativeMemory.Copy(dataPtr.ToPointer(), (_mappedResourcePtr + upload.BufferOffset).ToPointer(), (nuint)dataSize);

            if (buffer.IsExternal)
            {
                _device.BarrierManager.AddBufferBarrier(buffer, BarrierSync.Copy, BarrierAccess.CopyDest);
                _device.BarrierManager.FlushBarriers(ref cmdList, BarrierFlushTypes.Buffer);

                cmdList.CopyBufferRegion((ID3D12Resource*)buffer.GetNativeResource(_device.ResourceManager), (ulong)dataOffset, (ID3D12Resource*)Unsafe.AsPointer(ref _uploadResource.Get()), (ulong)upload.BufferOffset, (ulong)dataSize);
            }
            else
            {
                _device.BarrierManager.AddBufferBarrier(buffer, BarrierSync.Copy, BarrierAccess.CopyDest);
                _device.BarrierManager.FlushBarriers(ref cmdList, BarrierFlushTypes.Buffer);

                ID3D12Resource* resource = (ID3D12Resource*)_device.ResourceManager.GetResource(buffer);

                cmdList.CopyBufferRegion(resource, (ulong)dataOffset, (ID3D12Resource*)Unsafe.AsPointer(ref _uploadResource.Get()), (ulong)upload.BufferOffset, (ulong)dataSize);
            }
        }

        internal void UploadTexture(ref ID3D12GraphicsCommandList10 cmdList, FrameGraphResources resources, int index, FGBox? box, uint subresource, nint dataPtr, int dataSize, int dataRowPitch)
        {
            if (_needsNewBarrier)
            {
                _device.BarrierManager.AddBufferBarrier(ref _uploadResource.Get(), BarrierSync.Copy, BarrierAccess.CopySource);
                _needsNewBarrier = false;
            }

            Debug.Assert(dataRowPitch > 0);

            ref readonly FGResourceUpload upload = ref resources.Uploads[index];
            NRDResource texture = ResourceUtility.AsNRDResource(upload.Resource);

            FGBox destBox = box.GetValueOrDefault(ResourceUtility.GetTextureBox(texture, _device.ResourceManager));

            if (dataRowPitch % 256 == 0)
            {
                NativeMemory.Copy(dataPtr.ToPointer(), (_mappedResourcePtr + upload.BufferOffset).ToPointer(), (nuint)dataSize);
            }
            else
            {
                int actualRowPitch;
                if (texture.IsExternal)
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)texture.Native;
                    actualRowPitch = RHIFormatInfo.Query(native->Base.Description.Format).BytesPerPixel * destBox.Width;
                }
                else
                {
                    FrameGraphTexture fg = resources.FindFGTexture(texture.Index);
                    actualRowPitch = RHIFormatInfo.Query(fg.Description.Format).BytesPerPixel * destBox.Width;
                }

                int alignedRowPitch = (actualRowPitch + (-actualRowPitch & 255));

                nint currentDataPtr = dataPtr;
                nint currentMappedPtr = _mappedResourcePtr + upload.BufferOffset;

                int rows = destBox.Height * destBox.Depth;
                for (int y = 0; y < rows; y++)
                {
                    NativeMemory.Copy(currentDataPtr.ToPointer(), currentMappedPtr.ToPointer(), (nuint)actualRowPitch);

                    currentDataPtr += dataRowPitch;
                    currentMappedPtr += alignedRowPitch;
                }

                dataRowPitch = alignedRowPitch;
            }

            _device.BarrierManager.AddTextureBarrier(texture, BarrierSync.Copy, BarrierAccess.CopyDest, BarrierLayout.CopyDest, new BarrierSubresourceRange(subresource));
            _device.BarrierManager.FlushBarriers(ref cmdList, BarrierFlushTypes.Texture);

            ID3D12Resource* resource = (ID3D12Resource*)_device.ResourceManager.GetResource(texture);

            PlacedSubresourceFootprint srcFootprint = new PlacedSubresourceFootprint
            {
                Offset = (ulong)upload.BufferOffset,
                Footprint = new SubresourceFootprint
                {
                    Format = (texture.IsExternal ? ((D3D12RHITextureNative*)texture.Native)->Base.Description.Format : _device.ResourceManager.FindFGTexture(texture).Description.Format).ToTextureFormat(),
                    Width = (uint)destBox.Width,
                    Height = (uint)destBox.Height,
                    Depth = (uint)destBox.Depth,
                    RowPitch = (uint)dataRowPitch
                }
            };

            TextureCopyLocation destLoc = new TextureCopyLocation(resource, type: TextureCopyType.SubresourceIndex, subresourceIndex: 0);
            TextureCopyLocation srcLoc = new TextureCopyLocation((ID3D12Resource*)Unsafe.AsPointer(ref _uploadResource.Get()), type: TextureCopyType.PlacedFootprint, placedFootprint: srcFootprint);

            Box srcBox = new Box(0, 0, 0, (uint)destBox.Width, (uint)destBox.Height, (uint)destBox.Depth);

            cmdList.CopyTextureRegion(&destLoc, (uint)destBox.X, (uint)destBox.Y, (uint)destBox.Z, &srcLoc, &srcBox);
        }

        private readonly record struct DeferredUploadData(int index, nint DataPtr, int DataSize, int BufferOffset);
    }
}
