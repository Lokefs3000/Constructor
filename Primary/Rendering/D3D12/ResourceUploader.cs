using Primary.Rendering.Pass;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class ResourceUploader : IDisposable
    {
        private readonly NRDDevice _device;

        private D3D12MemAlloc.Allocation* _uploadAllocation;
        private ID3D12Resource2* _uploadResource;

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

                if (_uploadAllocation != null)
                    _uploadAllocation->Base.Release();
                if (_uploadResource != null)
                    _uploadResource->Release();

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
                    _uploadResource->Unmap(0, null);

                if (_uploadAllocation != null)
                    _uploadAllocation->Base.Release();
                if (_uploadResource != null)
                    _uploadResource->Release();

                _mappedResourcePtr = nint.Zero;
                _uploadAllocation = null;
                _uploadResource = null;

                D3D12MemAlloc.ALLOCATION_DESC allocDesc = new D3D12MemAlloc.ALLOCATION_DESC
                {
                    HeapType = D3D12_HEAP_TYPE_UPLOAD,
                };

                D3D12_RESOURCE_DESC1 resDesc = new D3D12_RESOURCE_DESC1
                {
                    Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
                    Alignment = 0,
                    Width = (ulong)_uploadResourceSize,
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = DXGI_FORMAT_UNKNOWN,
                    SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                    Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                    Flags = D3D12_RESOURCE_FLAG_USE_TIGHT_ALIGNMENT,
                    SamplerFeedbackMipRegion = new D3D12_MIP_REGION
                    {
                        Width = 0,
                        Height = 0,
                        Depth = 0,
                    }
                };

                D3D12MemAlloc.Allocation* ptr1 = null;
                ID3D12Resource2* ptr2 = null;

                HRESULT r = D3D12MemAlloc.Allocator.CreateResource3(_device.Allocator, &allocDesc, &resDesc, D3D12_BARRIER_LAYOUT_UNDEFINED, null, 0, null, &ptr1, UuidOf.Get<ID3D12Resource2>(), (void**)&ptr2);

                if (r.FAILED)
                {
                    _device.RHIDevice.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                ResourceUtility.SetResourceNameStack((ID3D12Resource*)ptr2, "NRDUploadBuffer");

                _uploadAllocation = ptr1;
                _uploadResource = ptr2;

                void* mapPtr = null;
                r = _uploadResource->Map(0, null, &mapPtr);

                if (r.FAILED)
                {
                    _device.RHIDevice.FlushPendingMessages();
                    throw new NotImplementedException("Add error message");
                }

                _needsNewBarrier = true;
                _mappedResourcePtr = (nint)mapPtr;
            }
        }

        internal void UploadBuffer(ID3D12GraphicsCommandList10* cmdList, FrameGraphResources resources, int index, nint dataPtr, int dataSize, int dataOffset)
        {
            if (_needsNewBarrier)
            {
                _device.BarrierManager.AddBufferBarrier((ID3D12Resource*)_uploadResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE);
                _needsNewBarrier = false;
            }

            ref readonly FGResourceUpload upload = ref resources.Uploads[index];
            NRDResource buffer = ResourceUtility.AsNRDResource(upload.Resource);

            NativeMemory.Copy(dataPtr.ToPointer(), (_mappedResourcePtr + upload.BufferOffset).ToPointer(), (nuint)dataSize);

            if (buffer.IsExternal)
            {
                throw new NotImplementedException();
            }
            else
            {
                _device.BarrierManager.AddBufferBarrier(buffer, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST);
                _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Buffer);

                ID3D12Resource* resource = (ID3D12Resource*)_device.ResourceManager.GetResource(buffer);

                cmdList->CopyBufferRegion(resource, (ulong)dataOffset, (ID3D12Resource*)_uploadResource, (ulong)upload.BufferOffset, (ulong)dataSize);
            }
        }

        internal void UploadTexture(ID3D12GraphicsCommandList10* cmdList, FrameGraphResources resources, int index, FGBox? box, uint subresource, nint dataPtr, int dataSize, int dataRowPitch)
        {
            if (_needsNewBarrier)
            {
                _device.BarrierManager.AddBufferBarrier((ID3D12Resource*)_uploadResource, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_SOURCE);
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

            _device.BarrierManager.AddTextureBarrier(texture, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST, D3D12_BARRIER_LAYOUT_COPY_DEST, new D3D12_BARRIER_SUBRESOURCE_RANGE(subresource));
            _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Texture);

            ID3D12Resource* resource = (ID3D12Resource*)_device.ResourceManager.GetResource(texture);

            D3D12_PLACED_SUBRESOURCE_FOOTPRINT srcFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
            {
                Offset = (ulong)upload.BufferOffset,
                Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                {
                    Format = (texture.IsExternal ? ((D3D12RHITextureNative*)texture.Native)->Base.Description.Format : _device.ResourceManager.FindFGTexture(texture).Description.Format).ToTextureFormat(),
                    Width = (uint)destBox.Width,
                    Height = (uint)destBox.Height,
                    Depth = (uint)destBox.Depth,
                    RowPitch = (uint)dataRowPitch
                }
            };

            D3D12_TEXTURE_COPY_LOCATION destLoc = new D3D12_TEXTURE_COPY_LOCATION(resource);
            D3D12_TEXTURE_COPY_LOCATION srcLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)_uploadResource, &srcFootprint);

            D3D12_BOX srcBox = new D3D12_BOX(0, 0, 0, destBox.Width, destBox.Height, destBox.Depth);

            cmdList->CopyTextureRegion(&destLoc, (uint)destBox.X, (uint)destBox.Y, (uint)destBox.Z, &srcLoc, &srcBox);
        }

        private readonly record struct DeferredUploadData(int index, nint DataPtr, int DataSize, int BufferOffset);
    }
}
