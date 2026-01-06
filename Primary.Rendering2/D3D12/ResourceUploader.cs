using Arch.LowLevel;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
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

namespace Primary.Rendering2.D3D12
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
                    _device.RHIDevice.FlushMessageQueue();
                    throw new NotImplementedException("Add error message");
                }

                ResourceUtility.SetResourceNameStack((ID3D12Resource*)ptr2, "NRDUploadBuffer");

                _uploadAllocation = ptr1;
                _uploadResource = ptr2;

                void* mapPtr = null;
                r = _uploadResource->Map(0, null, &mapPtr);

                if (r.FAILED)
                {
                    _device.RHIDevice.FlushMessageQueue();
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
            FrameGraphBuffer buffer = upload.Resource.AsBuffer();

            NativeMemory.Copy(dataPtr.ToPointer(), (_mappedResourcePtr + upload.BufferOffset).ToPointer(), (nuint)dataSize);

            if (buffer.IsExternal)
            {
                throw new NotImplementedException();
            }
            else
            {
                _device.BarrierManager.AddBufferBarrier(buffer, D3D12_BARRIER_SYNC_COPY, D3D12_BARRIER_ACCESS_COPY_DEST);
                _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Buffer);

                ID3D12Resource* resource = ResourceUtility.GetResource(_device.ResourceManager, buffer);

                cmdList->CopyBufferRegion(resource, (ulong)dataOffset, (ID3D12Resource*)_uploadResource, (ulong)upload.BufferOffset, (ulong)dataSize);
            }
        }

        private readonly record struct DeferredUploadData(int index, nint DataPtr, int DataSize, int BufferOffset);
    }
}
