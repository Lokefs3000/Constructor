using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Memory.Native;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal sealed unsafe class UploadManager : IDisposable
    {
        private readonly D3D12RHIDevice _device;

        private List<PendingDataUpload> _pendingUploads;
        private Lock _uploadListLock;

        private List<BufferBarrier> _bufferBarriers;
        private List<TextureBarrier> _textureBarriers;

        private bool _disposedValue;

        internal UploadManager(D3D12RHIDevice device)
        {
            _device = device;

            _pendingUploads = new List<PendingDataUpload>();
            _uploadListLock = new Lock();

            _bufferBarriers = new List<BufferBarrier>();
            _textureBarriers = new List<TextureBarrier>();
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
            ResourceDesc1 resDesc = buffer.Resource.GetDesc1();

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
            long alignedPitch = ResourceHelper.Align(resourcePitch, D3D12.TextureDataPitchAlignment);

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
                _pendingUploads.Add(new PendingDataUpload(rawData, texture, subresource, alignedPitch, D3D12.TextureDataPlacementAlignment));
        }

        internal void UploadPending(ref ID3D12GraphicsCommandList10 cmds)
        {
            if (_pendingUploads.Count == 0)
                return;

            //TODO: change max upload size from 2gb to available excess budget
            long maxUploadSize = int.MaxValue;

            ActivatePendingUploads(maxUploadSize, out RentedList<ActiveDataUpload> uploads, out long requiredUploadSize);
            if (uploads.IsEmpty)
                return;

            //PIX.PIXBeginEventOnCommandList((nint)cmds, 0xffffffff, "RHI-Upload");

            CreateAndFlushBarriers(ref cmds, uploads.AsSpan());
            CreateUploadBuffer(requiredUploadSize, out ComPtr<ID3D12Resource2> uploadBuffer, out D3D12MemAlloc.Allocation* uploadAllocation);

            if (Unsafe.IsNullRef(in uploadBuffer.Get()) || uploadAllocation == null)
                return;

            ArrayPtr<byte> mapped = new ArrayPtr<byte>(null, (int)requiredUploadSize);
            HResult hr = uploadBuffer.Map(0, (Silk.NET.Direct3D12.Range*)null, (void**)&mapped);
            if (hr.IsFailure)
            {
                throw new D3D12RHIException($"Failed to map resource upload buffer", hr.Value);
            }

            foreach (ActiveDataUpload upload in uploads)
            {
                Debug.Assert(!upload.RawData.IsNullOrEmpty);

                if (upload.Resource is D3D12RHIBuffer buffer)
                {
                    if (upload.RawData.TryCopyTo(mapped.Slice((int)upload.BufferOffset)))
                    {
                        cmds.CopyBufferRegion(
                            (ID3D12Resource*)Unsafe.AsPointer(ref buffer.Resource.Get()),
                            0,
                            (ID3D12Resource*)Unsafe.AsPointer(ref uploadBuffer.Get()),
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

                        TextureCopyLocation destLoc = new TextureCopyLocation((ID3D12Resource*)Unsafe.AsPointer(ref texture.Resource.Get()), type: TextureCopyType.SubresourceIndex, subresourceIndex: (uint)upload.SubresourceIndex);
                        TextureCopyLocation srcLoc = new TextureCopyLocation((ID3D12Resource*)Unsafe.AsPointer(ref uploadBuffer.Get()), type: TextureCopyType.PlacedFootprint, placedFootprint: new PlacedSubresourceFootprint
                        {
                            Offset = (ulong)upload.BufferOffset,
                            Footprint = new SubresourceFootprint
                            {
                                Format = texture.Description.Format.ToTextureFormat(),
                                Width = (uint)Math.Max(texture.Description.Width / (1 << mipLevel), 1),
                                Height = (uint)Math.Max(texture.Description.Height / (1 << mipLevel), 1),
                                Depth = (uint)Math.Max(texture.Description.DepthOrArraySize / (1 << mipLevel), 1),
                                RowPitch = (uint)upload.RowPitch
                            }
                        });

                        cmds.CopyTextureRegion(&destLoc, 0, 0, 0, &srcLoc, null);
                    }
                    else
                        EngLog.RHI.Error("Failed to upload raw data to upload texture ({res}): {src} -> {dst} ({dstReal})", upload.Resource.DebugName, upload.RawData, mapped.Slice((int)upload.BufferOffset), mapped);
                }

                NativeMemory.Free(upload.RawData.Pointer);
            }

            _device.AddResourceFreeNextFrame(() =>
            {
                uploadBuffer.Dispose();
                uploadAllocation->Base.Release();
            });

            uploadBuffer.Unmap(0, (Silk.NET.Direct3D12.Range*)null);
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

        private void CreateAndFlushBarriers(ref ID3D12GraphicsCommandList10 cmds, Span<ActiveDataUpload> uploads)
        {
            foreach (ActiveDataUpload upload in uploads)
            {
                if (upload.Resource is D3D12RHIBuffer buffer)
                {
                    D3D12RHIBufferNative* native = (D3D12RHIBufferNative*)buffer.GetAsNative();
                    if (native->BarrierSync == BarrierSync.Copy &&
                        native->BarrierAccess == BarrierAccess.CopyDest)
                        continue;

                    _bufferBarriers.Add(new BufferBarrier(
                        native->BarrierSync,
                        BarrierSync.Copy,
                        native->BarrierAccess,
                        BarrierAccess.CopyDest,
                        (ID3D12Resource*)Unsafe.AsPointer(ref buffer.Resource.Get())));

                    native->BarrierSync = BarrierSync.Copy;
                    native->BarrierAccess = BarrierAccess.CopyDest;
                }
                else if (upload.Resource is D3D12RHITexture texture)
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)texture.GetAsNative();
                    if (native->BarrierSync == BarrierSync.Copy &&
                        native->BarrierAccess == BarrierAccess.CopyDest &&
                        native->BarrierLayout == BarrierLayout.CopyDest)
                        continue;

                    //TODO: reduce the amount of barriers by transitioning all subresources and ignoring repeats
                    _textureBarriers.Add(new TextureBarrier(
                        native->BarrierSync,
                        BarrierSync.Copy,
                        native->BarrierAccess,
                        BarrierAccess.CopyDest,
                        native->BarrierLayout,
                        BarrierLayout.CopyDest,
                        (ID3D12Resource*)Unsafe.AsPointer(ref texture.Resource.Get()),
                        new BarrierSubresourceRange(uint.MaxValue)));

                    native->BarrierSync = BarrierSync.Copy;
                    native->BarrierAccess = BarrierAccess.CopyDest;
                    native->BarrierLayout = BarrierLayout.CopyDest;
                }
            }

            //TODO: group multiple barrier groups into a single Barrier call

            if (_bufferBarriers.Count > 0)
            {
                fixed (BufferBarrier* ptr = _bufferBarriers.AsSpan())
                {
                    BarrierGroup group = new BarrierGroup(type: BarrierType.Buffer, numBarriers: (uint)_bufferBarriers.Count, pBufferBarriers: ptr);
                    cmds.Barrier(1, &group);
                }
            }

            if (_textureBarriers.Count > 0)
            {
                fixed (TextureBarrier* ptr = _textureBarriers.AsSpan())
                {
                    BarrierGroup group = new BarrierGroup(type: BarrierType.Texture, numBarriers: (uint)_textureBarriers.Count, pTextureBarriers: ptr);
                    cmds.Barrier(1, &group);
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
                HeapType = HeapType.Upload,
                ExtraHeapFlags = HeapFlags.None,
                CustomPool = null,
                pPrivateData = null,
            };

            ResourceDesc1 resDesc = new ResourceDesc1
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = (ulong)requiredSize,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.FormatUnknown,
                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                Layout = TextureLayout.LayoutRowMajor,
                Flags = _device.Setup.UseTightAlignment ? ResourceFlags.UseTightAlignment : ResourceFlags.None,
                SamplerFeedbackMipRegion = new MipRegion { Width = 0, Height = 0, Depth = 0 }
            };

            resource = new ComPtr<ID3D12Resource2>();
            D3D12MemAlloc.Allocation* temp = null;

            HResult hr = D3D12MemAlloc.Allocator.CreateResource3(_device.Allocator, &allocDesc, &resDesc, BarrierLayout.Undefined, null, 0, null, &temp, SilkMarshal.GuidPtrOf<ID3D12Resource2>(), (void**)resource.GetAddressOf());
            if (hr.IsFailure)
            {
                _device.FlushPendingMessages();
                _device.Logger?.Error($"Failed to create resource upload buffer: {hr.ToString()}");

                resource = null;
                allocation = null;
                return;
            }

            allocation = temp;

            ResourceHelper.SetResourceName(ref resource.Get(), "RHIUploadBuffer");
        }

        internal bool HasPendingUploads => _pendingUploads.Count > 0;

        private readonly record struct PendingDataUpload(ArrayPtr<byte> RawData, RHIResource Resource, int SubresourceIndex, long RowPitch, int Alignment);
        private readonly record struct ActiveDataUpload(ArrayPtr<byte> RawData, RHIResource Resource, int SubresourceIndex, long RowPitch, long BufferOffset);
    }
}
