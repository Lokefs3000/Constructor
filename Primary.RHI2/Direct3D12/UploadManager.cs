using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using CommunityToolkit.HighPerformance;

using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;

using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    internal sealed unsafe class UploadManager : IDisposable
    {
        private readonly D3D12RHIDevice _device;

        private List<PendingDataUpload> _pendingUploads;
        private long _uploadBufferOffset;

        private List<D3D12_BUFFER_BARRIER> _bufferBarriers;
        private List<D3D12_TEXTURE_BARRIER> _textureBarriers;

        private bool _disposedValue;

        internal UploadManager(D3D12RHIDevice device)
        {
            _device = device;

            _pendingUploads = new List<PendingDataUpload>();
            _uploadBufferOffset = 0;

            _bufferBarriers = new List<D3D12_BUFFER_BARRIER>();
            _textureBarriers = new List<D3D12_TEXTURE_BARRIER>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (PendingDataUpload upload in _pendingUploads)
                    NativeMemory.Free(upload.RawData.ToPointer());
                _pendingUploads.Clear();

                _disposedValue = true;
            }
        }

        ~UploadManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void RemoveWithResource(RHIResource resource)
        {
            for (int i = 0; i < _pendingUploads.Count; ++i)
            {
                if (_pendingUploads[i].Resource == resource)
                {
                    _pendingUploads.RemoveAt(i--);
                }
            }
        }

        internal void AddBufferUpload(D3D12RHIBuffer buffer, nint data)
        {
            D3D12_RESOURCE_DESC1 desc = buffer.Resource.Get()->GetDesc1();
            desc.Alignment = 0;

            D3D12_RESOURCE_ALLOCATION_INFO info = _device.Device.Get()->GetResourceAllocationInfo3(0, 1, &desc, null, null, null);

            long offset = _uploadBufferOffset + (-_uploadBufferOffset & ((long)info.Alignment - 1));
            _uploadBufferOffset = offset + (long)info.SizeInBytes;

            void* rawData = NativeMemory.Alloc((nuint)info.SizeInBytes);
            NativeMemory.Copy(data.ToPointer(), rawData, (nuint)info.SizeInBytes);

            _pendingUploads.Add(new PendingDataUpload((nint)rawData, buffer.Description.Width, offset, buffer, 0));
        }

        internal void AddTextureUpload(D3D12RHITexture texture, nint data, uint subresource, int rowPitch)
        {
            D3D12_RESOURCE_DESC1 desc = texture.Resource.Get()->GetDesc1();

            desc.Alignment = 0;
            desc.Width /= subresource + 1;
            desc.Height /= subresource + 1;
            if (desc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D)
                desc.DepthOrArraySize /= (ushort)(subresource + 1);
            desc.MipLevels = 1;

            D3D12_RESOURCE_ALLOCATION_INFO info = _device.Device.Get()->GetResourceAllocationInfo3(0, 1, &desc, null, null, null);

            long offset = _uploadBufferOffset + (-_uploadBufferOffset & ((long)info.Alignment - 1));
            _uploadBufferOffset = offset + (long)info.SizeInBytes;

            void* rawData = NativeMemory.Alloc((nuint)info.SizeInBytes);
            if (rowPitch % 256 == 0)
            {
                NativeMemory.Copy(data.ToPointer(), rawData, (nuint)info.SizeInBytes);
            }
            else
            {
                int paddedRowPitch = (int)(desc.Width * RHIFormatInfo.Query(texture.Description.Format).BytesPerPixel);
                //paddedRowPitch = paddedRowPitch + (-paddedRowPitch & ((int)info.Alignment - 1));

                int totalRows = (int)(desc.Height * (desc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D ? desc.DepthOrArraySize : 1));

                for (int i = 0; i < totalRows; i++)
                {
                    NativeMemory.Copy((data + i * rowPitch).ToPointer(), ((byte*)rawData) + i * paddedRowPitch, (nuint)rowPitch);
                }
            }

            _pendingUploads.Add(new PendingDataUpload((nint)rawData, (uint)info.SizeInBytes, offset, texture, subresource));
        }

        internal void UploadPending(ID3D12GraphicsCommandList10* cmds)
        {
            if (_pendingUploads.Count == 0)
                return;

            ulong requiredSize = (ulong)_uploadBufferOffset;
            if (requiredSize == 0)
            {
                _pendingUploads.Clear();
                return;
            }

            CreateAndFlushBarriers(cmds);
            CreateUploadBuffer(requiredSize, out ComPtr<ID3D12Resource2> uploadBuffer, out D3D12MemAlloc.Allocation* uploadAllocation);

            void* mapped = null;
            HRESULT hr = uploadBuffer.Get()->Map(0, null, &mapped);
            if (hr.FAILED)
            {
                throw new Exception(hr.ToString());
            }

            foreach (PendingDataUpload upload in _pendingUploads)
            {
                Debug.Assert(upload.RawData != nint.Zero);

                if (upload.Resource is D3D12RHIBuffer buffer)
                {
                    cmds->CopyBufferRegion(
                        (ID3D12Resource*)buffer.Resource.Get(),
                        0,
                        (ID3D12Resource*)uploadBuffer.Get(),
                        (ulong)upload.UploadDataOffset,
                        upload.RawDataSize);

                    NativeMemory.Copy(upload.RawData.ToPointer(), ((byte*)mapped) + upload.UploadDataOffset, upload.RawDataSize);
                }
                else if (upload.Resource is D3D12RHITexture texture)
                {
                    int mipDiv = (int)(upload.SubresourceIndex + 1);

                    D3D12_TEXTURE_COPY_LOCATION destLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)texture.Resource.Get(), upload.SubresourceIndex);
                    D3D12_TEXTURE_COPY_LOCATION srcLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)uploadBuffer.Get(), new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
                    {
                        Offset = (ulong)upload.UploadDataOffset,
                        Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                        {
                            Format = texture.Description.Format.ToTextureFormat(),
                            Width = (uint)(texture.Description.Width / mipDiv),
                            Height = (uint)(texture.Description.Height / mipDiv),
                            Depth = (uint)(texture.Description.Dimension == RHIDimension.Texture3D ? texture.Description.DepthOrArraySize / mipDiv : 1),
                            RowPitch = (uint)(((texture.Description.Width / mipDiv) + (-(texture.Description.Width / mipDiv) & 255)) * RHIFormatInfo.Query(texture.Description.Format).BytesPerPixel)
                        }
                    });

                    cmds->CopyTextureRegion(&destLoc, 0, 0, 0, &srcLoc, null);

                    NativeMemory.Copy(upload.RawData.ToPointer(), ((byte*)mapped) + upload.UploadDataOffset, upload.RawDataSize);
                }

                NativeMemory.Free(upload.RawData.ToPointer());
            }

            _device.AddResourceFreeNextFrame(() =>
            {
                uploadBuffer.Reset();
                uploadAllocation->Base.Release();
            });

            uploadBuffer.Get()->Unmap(0, null);

            _pendingUploads.Clear();
            _uploadBufferOffset = 0;
        }

        private void CreateAndFlushBarriers(ID3D12GraphicsCommandList10* cmds)
        {
            _bufferBarriers.Clear();
            _textureBarriers.Clear();

            foreach (PendingDataUpload upload in _pendingUploads)
            {
                if (upload.Resource is D3D12RHIBuffer buffer)
                {
                    D3D12RHIBufferNative* native = (D3D12RHIBufferNative*)buffer.GetAsNative();
                    if (native->BarrierSync == D3D12_BARRIER_SYNC_COPY &&
                        native->BarrierAccess == D3D12_BARRIER_ACCESS_COPY_DEST)
                        continue;

                    _bufferBarriers.Add(new D3D12_BUFFER_BARRIER(
                        native->BarrierSync,
                        D3D12_BARRIER_SYNC_COPY,
                        native->BarrierAccess,
                        D3D12_BARRIER_ACCESS_COPY_DEST,
                        (ID3D12Resource*)buffer.Resource.Get()));

                    native->BarrierSync = D3D12_BARRIER_SYNC_COPY;
                    native->BarrierAccess = D3D12_BARRIER_ACCESS_COPY_DEST;
                }
                else if (upload.Resource is D3D12RHITexture texture)
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)texture.GetAsNative();
                    if (native->BarrierSync == D3D12_BARRIER_SYNC_COPY &&
                        native->BarrierAccess == D3D12_BARRIER_ACCESS_COPY_DEST &&
                        native->BarrierLayout == D3D12_BARRIER_LAYOUT_COPY_DEST)
                        continue;

                    //TODO: reduce the amount of barriers by transitioning all subresources and ignoring repeats
                    _textureBarriers.Add(new D3D12_TEXTURE_BARRIER(
                        native->BarrierSync,
                        D3D12_BARRIER_SYNC_COPY,
                        native->BarrierAccess,
                        D3D12_BARRIER_ACCESS_COPY_DEST,
                        native->BarrierLayout,
                        D3D12_BARRIER_LAYOUT_COPY_DEST,
                        (ID3D12Resource*)texture.Resource.Get(),
                        new D3D12_BARRIER_SUBRESOURCE_RANGE(uint.MaxValue)));

                    native->BarrierSync = D3D12_BARRIER_SYNC_COPY;
                    native->BarrierAccess = D3D12_BARRIER_ACCESS_COPY_DEST;
                    native->BarrierLayout = D3D12_BARRIER_LAYOUT_COPY_DEST;
                }
            }

            //TODO: group multiple barrier groups into a single Barrier call

            if (_bufferBarriers.Count > 0)
            {
                fixed (D3D12_BUFFER_BARRIER* ptr = _bufferBarriers.AsSpan())
                {
                    D3D12_BARRIER_GROUP group = new D3D12_BARRIER_GROUP((uint)_bufferBarriers.Count, ptr);
                    cmds->Barrier(1, &group);
                }
            }

            if (_textureBarriers.Count > 0)
            {
                fixed (D3D12_TEXTURE_BARRIER* ptr = _textureBarriers.AsSpan())
                {
                    D3D12_BARRIER_GROUP group = new D3D12_BARRIER_GROUP((uint)_textureBarriers.Count, ptr);
                    cmds->Barrier(1, &group);
                }
            }
        }

        private void CreateUploadBuffer(ulong requiredSize, out ComPtr<ID3D12Resource2> resource, out D3D12MemAlloc.Allocation* allocation)
        {
            D3D12MemAlloc.ALLOCATION_DESC allocDesc = new D3D12MemAlloc.ALLOCATION_DESC
            {
                Flags = ALLOCATION_FLAG_NONE,
                HeapType = D3D12_HEAP_TYPE_UPLOAD,
                ExtraHeapFlags = D3D12_HEAP_FLAG_NONE,
                CustomPool = null,
                pPrivateData = null,
            };

            D3D12_RESOURCE_DESC1 resDesc = new D3D12_RESOURCE_DESC1
            {
                Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
                Alignment = 0,
                Width = requiredSize,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = DXGI_FORMAT_UNKNOWN,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                Flags = D3D12_RESOURCE_FLAG_NONE,
                SamplerFeedbackMipRegion = new D3D12_MIP_REGION { Width = 0, Height = 0, Depth = 0 }
            };

            resource = new ComPtr<ID3D12Resource2>();
            D3D12MemAlloc.Allocation* temp = null;

            HRESULT hr = D3D12MemAlloc.Allocator.CreateResource3(_device.Allocator, &allocDesc, &resDesc, D3D12_BARRIER_LAYOUT_UNDEFINED, null, 0, null, &temp, UuidOf.Get<ID3D12Resource2>(), (void**)resource.GetAddressOf());
            if (hr.FAILED)
            {
                _device.FlushPendingMessages();
                throw new Exception(hr.ToString());
            }

            allocation = temp;
        }

        internal bool HasPendingUploads => _pendingUploads.Count > 0;

        private readonly record struct PendingDataUpload(nint RawData, uint RawDataSize, long UploadDataOffset, RHIResource Resource, uint SubresourceIndex);
    }
}
