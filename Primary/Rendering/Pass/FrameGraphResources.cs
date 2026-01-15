using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Rendering.NRD;
using Primary.Rendering.Resources;
using Primary.RHI2;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Primary.Rendering.Pass
{
    public sealed class FrameGraphResources : IDisposable
    {
        private readonly RenderingManager _manager;

        private RHISampler _defaultSampler;

        private PooledList<FGResourceEvent> _events;

        private PooledList<IndexRange> _virtualFreeSpace;
        private Dictionary<FrameGraphResource, MemoryRange> _allocatedResources;
        private int _currentMemoryExtent;
        private int _highestMemoryUsage;

        private Dictionary<FrameGraphResource, NRDResourceInfo> _cachedResourceInfo;
        private int _uploadBufferLength;

        private PooledList<FGResourceLocation> _locations;
        private PooledList<FGResourceUpload> _uploads;

        private FrameGraphResource[] _resourceArray;

        private Dictionary<object, int> _usedPipelinesDict;
        private List<object> _usedPipelines;

        private bool _disposedValue;

        internal FrameGraphResources(RenderingManager manager)
        {
            _manager = manager;

            _defaultSampler = manager.GraphicsDevice.CreateSampler(new RHISamplerDescription())!;

            _events = new PooledList<FGResourceEvent>();

            _virtualFreeSpace = new PooledList<IndexRange>();
            _allocatedResources = new Dictionary<FrameGraphResource, MemoryRange>();
            _currentMemoryExtent = 0;
            _highestMemoryUsage = 0;

            _cachedResourceInfo = new Dictionary<FrameGraphResource, NRDResourceInfo>();
            _uploadBufferLength = 0;

            _locations = new PooledList<FGResourceLocation>();
            _uploads = new PooledList<FGResourceUpload>();

            _resourceArray = Array.Empty<FrameGraphResource>();

            _usedPipelinesDict = new Dictionary<object, int>();
            _usedPipelines = new List<object>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _defaultSampler.Dispose();
                }

                _events.Dispose();
                _locations.Dispose();
                _uploads.Dispose();

                _disposedValue = true;
            }
        }

        ~FrameGraphResources()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearNewFrame()
        {
            Array.Fill(_resourceArray, FrameGraphResource.Invalid);

            ClearResources();
        }

        internal void ClearResources()
        {
            _events.Clear();

            _virtualFreeSpace.Clear();
            _allocatedResources.Clear();
            _currentMemoryExtent = 0;
            _highestMemoryUsage = 0;

            _cachedResourceInfo.Clear();
            _uploadBufferLength = 0;

            _locations.Clear();
            _uploads.Clear();

            //Array.Fill(_resourceArray, FrameGraphResource.Invalid);

            _usedPipelinesDict.Clear();
            _usedPipelines.Clear();
        }

        internal void SortAndFinish()
        {
            _events.Sort();

            _currentMemoryExtent = 0;

            foreach (FGResourceEvent @event in _events.Span)
            {
                if (@event.Action == FGResourceAction.Create)
                {
                    switch (@event.Resource.ResourceId)
                    {
                        case FGResourceId.Texture:
                            {
                                FrameGraphTexture texture = @event.Resource.AsTexture();
                                Debug.Assert(!texture.Description.Format.IsBlockCompressed(), "BC formats not supported yet");

                                NRDResourceInfo resourceInfo = _manager.NRDDevice.QueryResourceInfo(texture);
                                MemoryRange range = AllocateVirtualSpace(resourceInfo.SizeInBytes, resourceInfo.Alignment);

                                _allocatedResources.Add(texture, range);
                                _locations.Add(new FGResourceLocation(texture, range.AlignedStart, resourceInfo.SizeInBytes, resourceInfo.Alignment));

                                break;
                            }
                        case FGResourceId.Buffer:
                            {
                                FrameGraphBuffer buffer = @event.Resource.AsBuffer();

                                NRDResourceInfo resourceInfo = _manager.NRDDevice.QueryResourceInfo(buffer);
                                MemoryRange range = AllocateVirtualSpace(resourceInfo.SizeInBytes, resourceInfo.Alignment);

                                _allocatedResources.Add(buffer, range);
                                _locations.Add(new FGResourceLocation(buffer, range.AlignedStart, resourceInfo.SizeInBytes, resourceInfo.Alignment));

                                break;
                            }
                        default: continue;
                    }
                }
                else
                {
                    if (_allocatedResources.TryGetValue(@event.Resource, out MemoryRange range))
                    {
                        FreeVirtualSpace(range);
                    }
                    else
                    {
                        //TODO: implement actual error messaging
                        EngLog.Render.Error("Resource: '{r}' is not present in allocation dictionary", @event.Resource);
                    }
                }
            }

            _highestMemoryUsage = _currentMemoryExtent;
        }

        private MemoryRange AllocateVirtualSpace(int minimumMemorySize, int resourceAlignment)
        {
            resourceAlignment -= 1;

            Span<IndexRange> virtualSpaceSpan = _virtualFreeSpace.Span;
            if (!virtualSpaceSpan.IsEmpty)
            {
                for (int i = 0; i < _virtualFreeSpace.Count; i++)
                {
                    ref IndexRange range = ref virtualSpaceSpan.DangerousGetReferenceAt(i);

                    int alignedStart = range.Start + (-range.Start & (resourceAlignment));
                    int length = range.End - alignedStart;

                    if (length > minimumMemorySize)
                    {
                        IndexRange outputRange = new IndexRange(range.Start, range.Start + minimumMemorySize);

                        if (length == minimumMemorySize)
                            _virtualFreeSpace.RemoveAt(i);
                        else
                            range.Start = alignedStart + minimumMemorySize; //account for alignment

                        return new MemoryRange(outputRange.Start, alignedStart, outputRange.End);
                    }
                }
            }

            int previousMemoryExtent = _currentMemoryExtent;
            int alignedMemoryExtent = _currentMemoryExtent + (-_currentMemoryExtent & (resourceAlignment));

            _currentMemoryExtent = alignedMemoryExtent + minimumMemorySize; //account for alignment

            return new MemoryRange(previousMemoryExtent, alignedMemoryExtent, _currentMemoryExtent);
        }

        private void FreeVirtualSpace(MemoryRange usedRange)
        {
            Span<IndexRange> virtualSpaceSpan = _virtualFreeSpace.Span;
            if (!virtualSpaceSpan.IsEmpty)
            {
                for (int i = 0; i < _virtualFreeSpace.Count; i++)
                {
                    ref IndexRange range = ref virtualSpaceSpan.DangerousGetReferenceAt(i);

                    if (range.Start > usedRange.End)
                    {
                        if (i > 0)
                            _virtualFreeSpace.Insert(i - 1, usedRange.AsIndexRange());
                        else
                            _virtualFreeSpace.Insert(0, usedRange.AsIndexRange());

                        return;
                    }
                    else if (range.Start == usedRange.End)
                    {
                        range.Start = usedRange.TrueStart;
                        return;
                    }
                    else if (range.End == usedRange.TrueStart)
                    {
                        range.End = usedRange.End;
                        return;
                    }
                }
            }

            _virtualFreeSpace.Add(usedRange.AsIndexRange());
        }

        internal void AddResourceWithLifetime(FrameGraphResource resource, IndexRange passLifetimeRange)
        {
            Debug.Assert(resource.IsValidAndRenderGraph);

            _events.Add(new FGResourceEvent(FGResourceAction.Create, passLifetimeRange.Start, resource));
            _events.Add(new FGResourceEvent(FGResourceAction.Destroy, passLifetimeRange.End + 1, resource));
        }

        internal int AddBufferUpload(FrameGraphBuffer buffer, int uploadOffset, int uploadSize)
        {
            if (!_cachedResourceInfo.TryGetValue(buffer, out NRDResourceInfo info))
            {
                info = _manager.NRDDevice.QueryBufferInfo(buffer, uploadOffset, uploadSize);
                _cachedResourceInfo[buffer] = info;
            }

            int alignedOffset = _uploadBufferLength + (-_uploadBufferLength & (info.Alignment - 1));
            _uploadBufferLength = alignedOffset + info.SizeInBytes;

            _uploads.Add(new FGResourceUpload(buffer, alignedOffset, uploadSize));
            return _uploads.Count - 1;
        }

        internal int AddTextureUpload(FrameGraphTexture texture, int uploadOffset, int uploadSize)
        {
            if (!_cachedResourceInfo.TryGetValue(texture, out NRDResourceInfo info))
            {
                info = _manager.NRDDevice.QueryTextureInfo(texture, uploadOffset, uploadSize);
                _cachedResourceInfo[texture] = info;
            }

            int alignedOffset = _uploadBufferLength + (-_uploadBufferLength & (info.Alignment - 1));
            _uploadBufferLength = alignedOffset + info.SizeInBytes;

            _uploads.Add(new FGResourceUpload(texture, alignedOffset, uploadSize));
            return _uploads.Count - 1;
        }

        internal void AddFGResource(FrameGraphResource resource)
        {
            if (resource.Index >= _resourceArray.Length)
                Array.Resize(ref _resourceArray, (int)BitOperations.RoundUpToPowerOf2((uint)resource.Index + 1));
            _resourceArray[resource.Index] = resource;
        }

        internal int AddPontentialPipeline(RHIGraphicsPipeline pipeline)
        {
            if (_usedPipelinesDict.TryGetValue(pipeline, out int index))
                return index;

            index = _usedPipelines.Count;

            _usedPipelines.Add(pipeline);
            _usedPipelinesDict.Add(pipeline, index);

            return index;
        }

        internal int AddPontentialPipeline(RHIComputePipeline pipeline)
        {
            if (_usedPipelinesDict.TryGetValue(pipeline, out int index))
                return index;

            index = _usedPipelines.Count;

            _usedPipelines.Add(pipeline);
            _usedPipelinesDict.Add(pipeline, index);

            return index;
        }

        internal FrameGraphResource FindFGResource(int index)
        {
            if (index >= _resourceArray.Length || index < 0)
                return FrameGraphResource.Invalid;

            return _resourceArray[index];
        }

        internal FrameGraphBuffer FindFGBuffer(int index)
        {
            if (index >= _resourceArray.Length || index < 0)
                return FrameGraphBuffer.Invalid;

            return _resourceArray[index].AsBuffer();
        }

        internal FrameGraphTexture FindFGTexture(int index)
        {
            if (index >= _resourceArray.Length || index < 0)
                return FrameGraphTexture.Invalid;

            return _resourceArray[index].AsTexture();
        }

        internal object? GetPipelineFromIndex(int index)
        {
            if (index >= _usedPipelines.Count || index < 0)
                return null;

            return _usedPipelines[index];
        }

        public ReadOnlySpan<FGResourceEvent> Events => _events.Span;

        public ReadOnlySpan<FGResourceLocation> Locations => _locations.Span;
        public ReadOnlySpan<FGResourceUpload> Uploads => _uploads.Span;

        public int HighestMemoryUsage => _highestMemoryUsage;
        public int MinUploadSize => _uploadBufferLength;

        public RHISampler DefaultSampler => _defaultSampler;

        internal readonly record struct MemoryRange(int TrueStart, int AlignedStart, int End)
        {
            internal IndexRange AsIndexRange() => new IndexRange(TrueStart, End);
        }
    }

    public readonly record struct FGResourceEvent(FGResourceAction Action, int PassIndex, FrameGraphResource Resource) : IComparable<FGResourceEvent>
    {
        public int CompareTo(FGResourceEvent other) => PassIndex.CompareTo(other.PassIndex);
    }

    public readonly record struct FGResourceLocation(FrameGraphResource Resource, int MemoryOffset, int MemorySize, int Alignment);
    public readonly record struct FGResourceUpload(FrameGraphResource Resource, int BufferOffset, int BufferLength);

    public enum FGResourceAction : byte
    {
        Create = 0,
        Destroy,
        //Oneshot
    }
}
