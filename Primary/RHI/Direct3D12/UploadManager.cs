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
using Primary.Memory.Native;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using System.Collections.Concurrent;
using Primary.Collections;
using Primary.Common;
using Primary.Interop;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    internal sealed unsafe class UploadManager : IDisposable
    {
        private readonly D3D12RHIDevice _device;

        private List<PendingDataUpload> _pendingUploads;
        private Lock _uploadListLock;

        private List<D3D12_BUFFER_BARRIER> _bufferBarriers;
        private List<D3D12_TEXTURE_BARRIER> _textureBarriers;

        private bool _disposedValue;

        internal UploadManager(D3D12RHIDevice device)
        {
            _device = device;

            _pendingUploads = new List<PendingDataUpload>();
            _uploadListLock = new Lock();

            _bufferBarriers = new List<D3D12_BUFFER_BARRIER>();
            _textureBarriers = new List<D3D12_TEXTURE_BARRIER>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (PendingDataUpload upload in _pendingUploads)
                    NativeMemory.Free(upload.RawData.Pointer);
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

        internal void AddBufferUpload(D3D12RHIBuffer buffer, ArrayPtr<byte> data)
        {
            RHIBufferDescription desc = buffer.Description;
            D3D12_RESOURCE_DESC1 resDesc = buffer.Resource.Get()->GetDesc1();

            Guard.IsEqualTo(desc.Width, data.Length);

            ArrayPtr<byte> rawData = new ArrayPtr<byte>((byte*)NativeMemory.Alloc(desc.Width), (int)desc.Width);
            data.CopyTo(rawData);

            lock (_uploadListLock)
                _pendingUploads.Add(new PendingDataUpload(rawData, buffer, 0, 0, (int)resDesc.Alignment));
        }

        internal void AddTextureUpload(D3D12RHITexture texture, ArrayPtr<byte> data, int subresource)
        {
            RHITextureDescription desc = texture.Description;
            RHIFormatInfo fi = RHIFormatInfo.Query(texture.Description.Format);

            (int arrayIndex, int mipLevel) = ResourceHelper.DecodeSubresource(subresource, desc.MipLevels);

            desc.Width = Math.Max(desc.Width / (1 << mipLevel), 1);
            desc.Height = Math.Max(desc.Height / (1 << mipLevel), 1);
            desc.DepthOrArraySize = Math.Max(desc.DepthOrArraySize / (ushort)(1 << mipLevel), 1);

            long resourcePitch = fi.CalculatePitch(desc.Width);
            long alignedPitch = ResourceHelper.Align(resourcePitch, D3D12.D3D12_TEXTURE_DATA_PITCH_ALIGNMENT);

            Guard.IsEqualTo(fi.CalculateSize(desc.Width, desc.Height, desc.DepthOrArraySize), data.Length);

            long alignedUploadByteSize = fi.CalculateSizeWithPitch(alignedPitch, desc.Height, desc.DepthOrArraySize);

            ArrayPtr<byte> rawData = new ArrayPtr<byte>((byte*)NativeMemory.Alloc((nuint)alignedUploadByteSize), (int)alignedUploadByteSize);
            if (resourcePitch == alignedPitch)
            {
                data.CopyTo(rawData);
            }
            else
            {
                int rowCount = desc.Height * desc.DepthOrArraySize;
                if (fi.IsBlockCompressed)
                    rowCount /= fi.BlockWidth;

                for (int i = 0; i < rowCount; i++)
                {
                    data.Slice(i * (int)resourcePitch, (int)resourcePitch).CopyTo(rawData.Slice(i * (int)alignedPitch, (int)alignedPitch));
                }
            }

            lock (_uploadListLock)
                _pendingUploads.Add(new PendingDataUpload(rawData, texture, subresource, alignedPitch, D3D12.D3D12_TEXTURE_DATA_PLACEMENT_ALIGNMENT));
        }

        internal void UploadPending(ID3D12GraphicsCommandList10* cmds)
        {
            if (_pendingUploads.Count == 0)
                return;

            //TODO: change max upload size from 2gb to available excess budget
            long maxUploadSize = int.MaxValue;

            ActivatePendingUploads(maxUploadSize, out RentedList<ActiveDataUpload> uploads, out long requiredUploadSize);
            if (uploads.IsEmpty)
                return;

            //PIX.PIXBeginEventOnCommandList((nint)cmds, 0xffffffff, "RHI-Upload");

            CreateAndFlushBarriers(cmds, uploads.AsSpan());
            CreateUploadBuffer(requiredUploadSize, out ComPtr<ID3D12Resource2> uploadBuffer, out D3D12MemAlloc.Allocation* uploadAllocation);

            ArrayPtr<byte> mapped = new ArrayPtr<byte>(null, (int)requiredUploadSize);
            HRESULT hr = uploadBuffer.Get()->Map(0, null, (void**)&mapped);
            if (hr.FAILED)
            {
                throw new RHIException($"Failed to map resource upload buffer: {hr.ToString()}");
            }

            foreach (ActiveDataUpload upload in uploads)
            {
                Debug.Assert(!upload.RawData.IsNullOrEmpty);

                if (upload.Resource is D3D12RHIBuffer buffer)
                {
                    if (upload.RawData.TryCopyTo(mapped.Slice((int)upload.BufferOffset)))
                    {
                        cmds->CopyBufferRegion(
                            (ID3D12Resource*)buffer.Resource.Get(),
                            0,
                            (ID3D12Resource*)uploadBuffer.Get(),
                            (ulong)upload.BufferOffset,
                            (ulong)upload.RawData.Length);
                    }
                    else
                        EngLog.RHI.Error("Failed to upload raw data to upload buffer ({res}): {src} -> {dst} ({dstReal})", upload.Resource.DebugName, upload.RawData, mapped.Slice((int)upload.BufferOffset), mapped);
                }
                else if (upload.Resource is D3D12RHITexture texture)
                {
                    if (upload.RawData.TryCopyTo(mapped.Slice((int)upload.BufferOffset)))
                    {
                        (int _, int mipLevel) = ResourceHelper.DecodeSubresource(upload.SubresourceIndex, texture.Description.MipLevels);

                        D3D12_TEXTURE_COPY_LOCATION destLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)texture.Resource.Get(), (uint)upload.SubresourceIndex);
                        D3D12_TEXTURE_COPY_LOCATION srcLoc = new D3D12_TEXTURE_COPY_LOCATION((ID3D12Resource*)uploadBuffer.Get(), new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
                        {
                            Offset = (ulong)upload.BufferOffset,
                            Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                            {
                                Format = texture.Description.Format.ToTextureFormat(),
                                Width = (uint)Math.Max(texture.Description.Width / (1 << mipLevel), 1),
                                Height = (uint)Math.Max(texture.Description.Height / (1 << mipLevel), 1),
                                Depth = (uint)Math.Max(texture.Description.DepthOrArraySize / (1 << mipLevel), 1),
                                RowPitch = (uint)upload.RowPitch
                            }
                        });

                        cmds->CopyTextureRegion(&destLoc, 0, 0, 0, &srcLoc, null);
                    }
                    else
                        EngLog.RHI.Error("Failed to upload raw data to upload texture ({res}): {src} -> {dst} ({dstReal})", upload.Resource.DebugName, upload.RawData, mapped.Slice((int)upload.BufferOffset), mapped);
                }

                NativeMemory.Free(upload.RawData.Pointer);
            }

            _device.AddResourceFreeNextFrame(() =>
            {
                uploadBuffer.Reset();
                uploadAllocation->Base.Release();
            });

            uploadBuffer.Get()->Unmap(0, null);
            //PIX.PIXEndEventOnCommandList((nint)cmds);

            uploads.Dispose();
        }

        private void ActivatePendingUploads(long maxUploadSize, out RentedList<ActiveDataUpload> active, out long uploadBufferSize)
        {
            active = new RentedList<ActiveDataUpload>();
            uploadBufferSize = 0;

            lock (_uploadListLock)
            {
                int baseIndex = 0;

                do
                {
                    PendingDataUpload upload = _pendingUploads[baseIndex];
                    long aligned = ResourceHelper.Align(uploadBufferSize, upload.Alignment);

                    if (aligned + upload.RawData.Length > maxUploadSize)
                    {
                        if (active.IsEmpty)
                        {
                            active.Add(new ActiveDataUpload(upload.RawData, upload.Resource, upload.SubresourceIndex, upload.RowPitch, aligned));
                            uploadBufferSize = aligned + upload.RawData.Length;

                            _pendingUploads.RemoveAt(baseIndex);
                            break;
                        }
                        else
                        {
                            ++baseIndex;
                            continue;
                        }
                    }
                    else
                    {
                        active.Add(new ActiveDataUpload(upload.RawData, upload.Resource, upload.SubresourceIndex, upload.RowPitch, aligned));
                        uploadBufferSize = aligned + upload.RawData.Length;

                        _pendingUploads.RemoveAt(baseIndex);
                    }
                } while (baseIndex < _pendingUploads.Count);
            }
        }

        private void CreateAndFlushBarriers(ID3D12GraphicsCommandList10* cmds, Span<ActiveDataUpload> uploads)
        {
            foreach (ActiveDataUpload upload in uploads)
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

            _bufferBarriers.Clear();
            _textureBarriers.Clear();
        }

        private void CreateUploadBuffer(long requiredSize, out ComPtr<ID3D12Resource2> resource, out D3D12MemAlloc.Allocation* allocation)
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
                Width = (ulong)requiredSize,
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
                throw new RHIException($"Failed to create resource upload buffer: {hr.ToString()}");
            }

            allocation = temp;
        }

        internal bool HasPendingUploads => _pendingUploads.Count > 0;

        private readonly record struct PendingDataUpload(ArrayPtr<byte> RawData, RHIResource Resource, int SubresourceIndex, long RowPitch, int Alignment);
        private readonly record struct ActiveDataUpload(ArrayPtr<byte> RawData, RHIResource Resource, int SubresourceIndex, long RowPitch, long BufferOffset);
    }
}
