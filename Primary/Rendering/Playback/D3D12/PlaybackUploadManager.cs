using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Memory;
using Primary.Profiling;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.Rendering.Playback.D3D12
{
    internal unsafe sealed class PlaybackUploadManager : IDisposable, IJob
    {
        private readonly PlaybackDevice _device;
        private readonly PlaybackExecutor _executor;

        private Ptr<D3D12MemAlloc.Allocation> _uploadAllocation;
        private Ptr<ID3D12Resource2> _uploadResource;

        private int _uploadResourceSize;

        private uint[] _uploadByteWidths;

        private CompiledRenderPlan? _renderPlan;

        private bool _disposedValue;

        internal PlaybackUploadManager(PlaybackDevice device, PlaybackExecutor executor)
        {
            _device = device;
            _executor = executor;

            _uploadAllocation = Ptr<D3D12MemAlloc.Allocation>.Null;
            _uploadResource = Ptr<ID3D12Resource2>.Null;

            _uploadResourceSize = 0;

            _uploadByteWidths = [];

            _renderPlan = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (!_uploadAllocation.IsNull)
                {
                    _uploadAllocation.Pointer->Base.Release();
                    _uploadAllocation = Ptr<D3D12MemAlloc.Allocation>.Null;
                }

                if (!_uploadResource.IsNull)
                {
                    _uploadResource.Pointer->Release();
                    _uploadResource = Ptr<ID3D12Resource2>.Null;
                }

                _disposedValue = true;
            }
        }

        ~PlaybackUploadManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void SetJobArguments(CompiledRenderPlan renderPlan)
        {
            _renderPlan = renderPlan;
        }

        void IJob.Execute()
        {
            using (new ProfilingScope("HandleUploads"))
            {
                Debug.Assert(_renderPlan != null);

                D3D12MemAlloc.Allocator* allocator = _device.NativeDevice.Allocator;
                ID3D12Device14* device = *_device.NativeDevice.Device.GetAddressOf();

                using (new ProfilingScope("Allocate"))
                {
                    if (_uploadAllocation.IsNull || _uploadResource.IsNull || _uploadResourceSize < _renderPlan.RequiredUploadMemory)
                    {
                        int roundedUpSize = MemoryHelper.AlignUp(_renderPlan.RequiredUploadMemory, 0xffff);

                        if (!_uploadAllocation.IsNull)
                        {
                            _uploadAllocation.Pointer->Base.Release();
                            _uploadAllocation = Ptr<D3D12MemAlloc.Allocation>.Null;
                        }

                        if (!_uploadResource.IsNull)
                        {
                            _uploadResource.Pointer->Release();
                            _uploadResource = Ptr<ID3D12Resource2>.Null;
                        }

                        D3D12MemAlloc.ALLOCATION_DESC allocationDesc = new D3D12MemAlloc.ALLOCATION_DESC
                        {
                            Flags = D3D12MemAlloc.ALLOCATION_FLAGS.ALLOCATION_FLAG_COMMITTED,
                            HeapType = HeapType.Upload,
                            ExtraHeapFlags = HeapFlags.None
                        };

                        ResourceDesc1 resourceDesc = new ResourceDesc1
                        {
                            Dimension = ResourceDimension.Buffer,
                            Alignment = 0,
                            Width = (ulong)roundedUpSize,
                            Height = 1,
                            DepthOrArraySize = 1,
                            MipLevels = 1,
                            Format = Format.FormatUnknown,
                            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                            Layout = TextureLayout.LayoutRowMajor,
                            Flags = ResourceFlags.DenyShaderResource,
                            SamplerFeedbackMipRegion = new MipRegion { Width = 0, Height = 0, Depth = 0 }
                        };

                        D3D12MemAlloc.Allocation* allocation = null;
                        ID3D12Resource2* resource = null;
                        HResult hr = D3D12MemAlloc.Allocator.CreateResource3(
                            allocator,
                            &allocationDesc,
                            &resourceDesc,
                            BarrierLayout.Undefined,
                            null,
                            0,
                            null,
                            &allocation,
                            SilkMarshal.GuidPtrOf<ID3D12Resource2>(),
                            (void**)&resource);

                        if (hr.IsFailure)
                        {
                            EngLog.Render.Fatal("Failed to create upload resource for frame graph");
                            EngLog.Render.Fatal("    Allocation desc: {");
                            EngLog.Render.Fatal("       Flags: {v}", allocationDesc.Flags);
                            EngLog.Render.Fatal("       HeapType: {v}", allocationDesc.HeapType);
                            EngLog.Render.Fatal("       ExtraHeapFlags: {v}", allocationDesc.ExtraHeapFlags);
                            EngLog.Render.Fatal("       CustomPool: {v:x16}", (nint)allocationDesc.CustomPool);
                            EngLog.Render.Fatal("       pPrivateData: {v:x16}", (nint)allocationDesc.pPrivateData);
                            EngLog.Render.Fatal("    }");
                            EngLog.Render.Fatal("    Resource desc: {");
                            EngLog.Render.Fatal("       Dimension: {v}", resourceDesc.Dimension);
                            EngLog.Render.Fatal("       Alignment: {v}", resourceDesc.Alignment);
                            EngLog.Render.Fatal("       Width: {v}", resourceDesc.Width);
                            EngLog.Render.Fatal("       Height: {v}", resourceDesc.Height);
                            EngLog.Render.Fatal("       DepthOrArraySize: {v}", resourceDesc.DepthOrArraySize);
                            EngLog.Render.Fatal("       MipLevels: {v}", resourceDesc.MipLevels);
                            EngLog.Render.Fatal("       Format: {v}", resourceDesc.Format);
                            EngLog.Render.Fatal("       SampleDesc: {{ count:{v0}, quality:{v1} }}", resourceDesc.SampleDesc.Count, resourceDesc.SampleDesc.Quality);
                            EngLog.Render.Fatal("       Layout: {v}", resourceDesc.Layout);
                            EngLog.Render.Fatal("       Flags: {v}", resourceDesc.Flags);
                            EngLog.Render.Fatal("       SamplerFeedbackMipRegion: {{ w:{v0}, h:{v1}, d:{v2} }}", resourceDesc.SamplerFeedbackMipRegion.Width, resourceDesc.SamplerFeedbackMipRegion.Height, resourceDesc.SamplerFeedbackMipRegion.Depth);
                            EngLog.Render.Fatal("    }");
                            EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                            _device.NativeDevice.FlushPendingMessages();
                            EngLog.ThrowFatal();
                        }

                        _uploadAllocation = allocation;
                        _uploadResource = resource;

                        _uploadResourceSize = roundedUpSize;

                        ResidencyPriority priority = ResidencyPriority.High;
                        device->SetResidencyPriority(1, (ID3D12Pageable**)&resource, &priority);
                    }
                }

                using (new ProfilingScope("CopyAndPad"))
                {
                    void* mapData = null;
                    HResult hr = _uploadResource.Pointer->Map(0, null, &mapData);

                    if (hr.IsFailure || mapData == null)
                    {
                        EngLog.Render.Fatal("Failed to map upload resource or returned pointer is null");
                        EngLog.Render.Fatal("    Map pointer: {v:x16}", (nint)mapData);
                        EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                        _device.NativeDevice.FlushPendingMessages();
                        EngLog.ThrowFatal();
                    }

                    byte* byteMapData = (byte*)mapData;

                    ROList<RenderUploadInfo> uploads = _renderPlan.Uploads;
                    if (_uploadByteWidths.Length < uploads.Count)
                    {
                        _uploadByteWidths = new uint[MemoryHelper.AlignUp(uploads.Count, 32)];
                    }

                    for (int i = 0; i < uploads.Count; ++i)
                    {
                        RenderUploadInfo uploadInfo = uploads[i];

                        RenderResourceInfo resourceInfo = _renderPlan.Resources[_renderPlan.ResourceIndices[uploadInfo.ResourceIndex]];
                        FGResourceType resourceType = resourceInfo.ImportedResource != null ?
                            (resourceInfo.ImportedResource is RHIBuffer ? FGResourceType.Buffer : FGResourceType.Texture) :
                            _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Type;

                        if (resourceType == FGResourceType.Buffer)
                        {
                            Debug.Assert(uploadInfo.SourceData.Length == uploadInfo.DestBox.X2 - uploadInfo.DestBox.X1);

                            NativeMemory.Copy(
                                uploadInfo.SourceData.Pointer,
                                byteMapData + uploadInfo.ByteOffsetInHeap,
                                (nuint)(uploadInfo.DestBox.X2 - uploadInfo.DestBox.X1));

                            _uploadByteWidths[i] = (uint)(uploadInfo.DestBox.X2 - uploadInfo.DestBox.X1);
                        }
                        else
                        {
                            RHIFormatInfo formatInfo = RHIFormatInfo.Query(resourceInfo.ImportedResource != null ?
                                Unsafe.As<RHITexture>(resourceInfo.ImportedResource).Description.Format :
                                _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Resource.TextureDesc.Format);

                            long rowPitch = formatInfo.CalculatePitch(uploadInfo.DestBox.X2 - uploadInfo.DestBox.X1);
                            if ((rowPitch % Silk.NET.Direct3D12.D3D12.TextureDataPitchAlignment) == 0)
                            {
                                long totalData = formatInfo.CalculateSizeWithPitch(rowPitch, uploadInfo.DestBox.Y2 - uploadInfo.DestBox.Y1, uploadInfo.DestBox.Z2 - uploadInfo.DestBox.Z1);
                                Debug.Assert(uploadInfo.SourceData.Length == totalData);

                                NativeMemory.Copy(
                                    uploadInfo.SourceData.Pointer,
                                    byteMapData + uploadInfo.ByteOffsetInHeap,
                                    (nuint)totalData);

                                _uploadByteWidths[i] = (uint)totalData;
                            }
                            else
                            {
                                long alignedRowPitch = MemoryHelper.AlignUp((int)rowPitch, Silk.NET.Direct3D12.D3D12.TextureDataPitchAlignment);

                                int boxHeight = uploadInfo.DestBox.Y2 - uploadInfo.DestBox.Y1;
                                int boxDepth = uploadInfo.DestBox.Z2 - uploadInfo.DestBox.Z1;

                                int rowsInSourceData = boxHeight * boxDepth;
                                for (int row = 0; row < rowsInSourceData; ++row)
                                {
                                    byte* sourceDataPtr = uploadInfo.SourceData.Pointer + (row * rowPitch);
                                    byte* destinationDataPtr = byteMapData + uploadInfo.ByteOffsetInHeap + (row * alignedRowPitch);

                                    NativeMemory.Copy(
                                        sourceDataPtr,
                                        destinationDataPtr,
                                        (nuint)rowPitch);
                                }

                                _uploadByteWidths[i] = (uint)(alignedRowPitch * boxHeight * boxDepth);
                            }
                        }
                    }

                    _uploadResource.Pointer->Unmap(0, null);
                }
            }
        }

        internal uint GetUploadByteWidth(int uploadIndex) => _uploadByteWidths[uploadIndex];

        internal ID3D12Resource2* UploadBuffer => _uploadResource.Pointer;
    }
}
