using System.Diagnostics;
using System.Runtime.CompilerServices;
using Primary.Common;
using Primary.Common.Memory;
using Primary.Mathematics;
using Primary.Memory;
using Primary.Memory.Native;
using Primary.Profiling;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Utility;
using Primary.RHI;

namespace Primary.Rendering.Memory
{
    public sealed class UploadManager : IDisposable
    {
        private readonly FrameGraph _frameGraph;

        private readonly LinearResizingAllocator _transientAllocator;
        private readonly VirtualAllocator _virtualAllocator;

        private readonly List<DataUploadRequest> _uploadRequests;

        private bool _disposedValue;

        internal UploadManager(FrameGraph frameGraph)
        {
            _frameGraph = frameGraph;

            _transientAllocator = new LinearResizingAllocator(4096);
            _virtualAllocator = new VirtualAllocator();

            _uploadRequests = new List<DataUploadRequest>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _transientAllocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void CleanupUploadData()
        {
            _transientAllocator.Reset();
            _virtualAllocator.ResetAllocator();

            _uploadRequests.Clear();
        }

        internal unsafe void FlushUploads(CompiledRenderPlan renderPlan)
        {
            using (new ProfilingScope("FlushUploads"))
            {
                renderPlan.RequiredUploadMemory = _virtualAllocator.AllocatedMemory;

                foreach (DataUploadRequest uploadRequest in _uploadRequests)
                {
                    renderPlan.AddUpload(uploadRequest.OffsetInHeap, new ArrayPtr<byte>((byte*)_transientAllocator.GetAtOffset(uploadRequest.UploadBaseOffset), uploadRequest.UploadSize), uploadRequest.DestSlice, uploadRequest.DestBox, uploadRequest.Resource.Index);
                }
            }
        }

        internal unsafe int RequestDataUpload(BufferHandle buffer, uint byteOffset, ReadOnlySpan<byte> uploadBytes)
        {
            Debug.Assert(!uploadBytes.IsEmpty);

            int uploadOffset = _transientAllocator.CurrentOffset;

            ArrayPtr<byte> rawPointer = new ArrayPtr<byte>((byte*)_transientAllocator.Allocate(uploadBytes.Length), uploadBytes.Length);
            uploadBytes.CopyTo(rawPointer.AsSpan());

            int alignment = 1;

            if (buffer.IsImported)
            {
                ref readonly RHIBufferDescription bufferDesc = ref _frameGraph.ResourceManager.GetRHIResource(buffer).Description;

                if (bufferDesc.Usage.HasFlags(RHIResourceUsage.IndexInput))
                {
                    // If no stride default to the highest alignment for safety
                    if (bufferDesc.Stride == 0)
                        alignment = 4;
                    else
                        alignment = bufferDesc.Stride;
                }
            }
            else
            {
                ref readonly FGBufferDesc bufferDesc = ref _frameGraph.ResourceManager.GetDescription(buffer);

                if (bufferDesc.Target.HasFlags(FGBufferTarget.IndexInput))
                {
                    // If no stride default to the highest alignment for safety
                    if (bufferDesc.Stride == 0)
                        alignment = 4;
                    else
                        alignment = bufferDesc.Stride;
                }
            }

            VirtualAllocation allocation = _virtualAllocator.Allocate(alignment, uploadBytes.Length);
            DataUploadRequest uploadRequest = new DataUploadRequest(buffer, allocation.MemoryOffset, allocation.MemorySize, uploadOffset, rawPointer.Length, byteOffset, new FGBox((int)byteOffset, 0, (int)byteOffset + rawPointer.Length, 0));

            int index = _uploadRequests.Count;
            _uploadRequests.Add(uploadRequest);

            return index;
        }

        internal unsafe int RequestDataUpload(TextureHandle texture, uint slice, FGBox? box, ReadOnlySpan<byte> uploadBytes)
        {
            Debug.Assert(!uploadBytes.IsEmpty);

            int uploadOffset = _transientAllocator.CurrentOffset;

            ArrayPtr<byte> rawPointer = new ArrayPtr<byte>((byte*)_transientAllocator.Allocate(uploadBytes.Length), uploadBytes.Length);
            uploadBytes.CopyTo(rawPointer.AsSpan());

            Int3 sliceSize;
            RHIFormat format;

            if (texture.IsImported)
            {
                ref readonly RHITextureDescription textureDesc = ref _frameGraph.ResourceManager.GetRHIResource(texture).Description;

                sliceSize = box?.Size ?? ResourceUtility.CalculateSizeForSlice(in textureDesc, (int)slice);
                format = textureDesc.Format;
            }
            else
            {
                ref readonly FGTextureDesc textureDesc = ref _frameGraph.ResourceManager.GetDescription(texture);

                sliceSize = box?.Size ?? ResourceUtility.CalculateSizeForSlice(in textureDesc, (int)slice);
                format = textureDesc.Format;
            }

            int alignment = 512;
            int alignedPitch = MemoryHelper.AlignUp((int)RHIFormatInfo.Query(format).CalculatePitch(sliceSize.X), 256);
            int memorySize = alignedPitch * sliceSize.Y * sliceSize.Z;

            VirtualAllocation allocation = _virtualAllocator.Allocate(alignment, memorySize);
            DataUploadRequest uploadRequest = new DataUploadRequest(texture, allocation.MemoryOffset, allocation.MemorySize, uploadOffset, rawPointer.Length, slice, box ?? new FGBox(Int3.Zero, sliceSize));

            int index = _uploadRequests.Count;
            _uploadRequests.Add(uploadRequest);

            return index;
        }

        internal unsafe (ArrayPtr<T> MapData, DataMapInfo MapInfo) MapForWriting<T>(BufferHandle buffer, FGRange? writeRange) where T : unmanaged
        {
            FGRange range = writeRange.GetValueOrDefault();
            if (!writeRange.HasValue)
            {
                ref readonly FGBufferDesc bufferDesc = ref _frameGraph.ResourceManager.GetDescription(buffer);
                range = new FGRange(bufferDesc.ElementCount * Math.Max(bufferDesc.Stride, 1));
            }

            int uploadOffset = _transientAllocator.CurrentOffset;
            int uploadElementCount = Math.Max(range.Length / Unsafe.SizeOf<T>(), 1);

            ArrayPtr<T> rawPointer = new ArrayPtr<T>((T*)_transientAllocator.Allocate(uploadElementCount * Unsafe.SizeOf<T>()), uploadElementCount);
            return (rawPointer, new DataMapInfo(buffer, uploadOffset, range.Length, (uint)range.Mininmum, new FGBox(range.Mininmum, 0, range.Maximum, 0)));
        }

        internal unsafe (ArrayPtr<T> MapData, DataMapInfo MapInfo) MapForWriting<T>(TextureHandle texture, uint subresource, FGBox? writeBox) where T : unmanaged
        {
            FGBox box = writeBox.GetValueOrDefault();
            RHIFormat format;

            if (texture.IsImported)
            {
                ref readonly RHITextureDescription textureDesc = ref _frameGraph.ResourceManager.GetRHIResource(texture).Description;

                format = textureDesc.Format;

                if (!writeBox.HasValue)
                {
                    box = new FGBox(Int3.Zero, ResourceUtility.CalculateSizeForSlice(in textureDesc, (int)subresource));
                }
            }
            else
            {
                ref readonly FGTextureDesc textureDesc = ref _frameGraph.ResourceManager.GetDescription(texture);

                format = textureDesc.Format;

                if (!writeBox.HasValue)
                {
                    box = new FGBox(Int3.Zero, ResourceUtility.CalculateSizeForSlice(in textureDesc, (int)subresource));
                }
            }

            int totalDataSize = (int)RHIFormatInfo.Query(format).CalculateSize(box.X2 - box.X1, box.Y2 - box.Y1, box.Z2 - box.Z1);

            int uploadOffset = _transientAllocator.CurrentOffset;
            int uploadElementCount = Math.Max(totalDataSize / Unsafe.SizeOf<T>(), 1);

            ArrayPtr<T> rawPointer = new ArrayPtr<T>((T*)_transientAllocator.Allocate(uploadElementCount * Unsafe.SizeOf<T>()), uploadElementCount);
            return (rawPointer, new DataMapInfo(texture, uploadOffset, totalDataSize, subresource, box));
        }

        internal unsafe int UnmapForWriting(DataMapInfo mapInfo)
        {
            if (mapInfo.Resource.ResourceType == FGResourceType.Buffer)
            {
                BufferHandle buffer = mapInfo.Resource.AsBuffer();

                int alignment = 1;

                if (buffer.IsImported)
                {
                    ref readonly RHIBufferDescription bufferDesc = ref _frameGraph.ResourceManager.GetRHIResource(buffer).Description;

                    if (bufferDesc.Usage.HasFlags(RHIResourceUsage.IndexInput))
                    {
                        // If no stride default to the highest alignment for safety
                        if (bufferDesc.Stride == 0)
                            alignment = 4;
                        else
                            alignment = bufferDesc.Stride;
                    }
                }
                else
                {
                    ref readonly FGBufferDesc bufferDesc = ref _frameGraph.ResourceManager.GetDescription(buffer);

                    if (bufferDesc.Target.HasFlags(FGBufferTarget.IndexInput))
                    {
                        // If no stride default to the highest alignment for safety
                        if (bufferDesc.Stride == 0)
                            alignment = 4;
                        else
                            alignment = bufferDesc.Stride;
                    }
                }

                VirtualAllocation allocation = _virtualAllocator.Allocate(alignment, mapInfo.UploadSize);
                DataUploadRequest uploadRequest = new DataUploadRequest(mapInfo.Resource, allocation.MemoryOffset, allocation.MemorySize, mapInfo.UploadBaseOffset, mapInfo.UploadSize, mapInfo.DestSlice, mapInfo.DestBox);

                int index = _uploadRequests.Count;
                _uploadRequests.Add(uploadRequest);

                return index;
            }
            else
            {
                TextureHandle texture = mapInfo.Resource.AsTexture();

                Int3 sliceSize;
                RHIFormat format;

                if (texture.IsImported)
                {
                    ref readonly RHITextureDescription textureDesc = ref _frameGraph.ResourceManager.GetRHIResource(texture).Description;

                    sliceSize = mapInfo.DestBox.Size;
                    format = textureDesc.Format;
                }
                else
                {
                    ref readonly FGTextureDesc textureDesc = ref _frameGraph.ResourceManager.GetDescription(texture);

                    sliceSize = mapInfo.DestBox.Size;
                    format = textureDesc.Format;
                }

                int alignment = 512;
                int alignedPitch = MemoryHelper.AlignUp((int)RHIFormatInfo.Query(format).CalculatePitch(sliceSize.X), 256);
                int memorySize = alignedPitch * sliceSize.Y * sliceSize.Z;

                VirtualAllocation allocation = _virtualAllocator.Allocate(alignment, memorySize);
                DataUploadRequest uploadRequest = new DataUploadRequest(mapInfo.Resource, allocation.MemoryOffset, allocation.MemorySize, mapInfo.UploadBaseOffset, mapInfo.UploadSize, mapInfo.DestSlice, mapInfo.DestBox);

                int index = _uploadRequests.Count;
                _uploadRequests.Add(uploadRequest);

                return index;
            }
        }

        private readonly record struct DataUploadRequest(ResourceHandle Resource, int OffsetInHeap, int ReservationSize, int UploadBaseOffset, int UploadSize, uint DestSlice, FGBox DestBox);
    }

    internal readonly record struct DataMapInfo(ResourceHandle Resource, int UploadBaseOffset, int UploadSize, uint DestSlice, FGBox DestBox);
}
