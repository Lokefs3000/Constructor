using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Memory;
using Primary.Rendering.Memory;
using Primary.Rendering.Playback;

namespace Primary.Rendering.Resources
{
    internal sealed class LifetimeManager
    {
        private readonly ResourceManager _resourceManager;
        private readonly VirtualAllocator _allocator;

        private readonly Dictionary<GenericResourceDesc, PersistantAllocationInfo> _persistantAllocs;
        private readonly HashSet<GenericResourceDesc> _aliveResources;

        private ResourceVirtualAllocInfo[] _resourceLifetimes;

        private LifetimeIndex[] _firstLifetimes;
        private LifetimeIndex[] _lastLifetimes;

        private int _currentResourceCount;
        private readonly HashSet<int> _resourcesAcrossEngines;

        internal LifetimeManager(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _allocator = new VirtualAllocator();

            _persistantAllocs = new Dictionary<GenericResourceDesc, PersistantAllocationInfo>();
            _aliveResources = new HashSet<GenericResourceDesc>();

            _resourceLifetimes = [];

            _firstLifetimes = [];
            _lastLifetimes = [];

            _currentResourceCount = 0;
            _resourcesAcrossEngines = new HashSet<int>();
        }

        internal void CheckPersistantAllocations(ROList<GenericResourceDesc> resourceDescs)
        {
            foreach (var (key, _) in _persistantAllocs)
            {
                _aliveResources.Add(key);
            }



            _aliveResources.Clear();
        }

        internal void SetupForLifetimeEvaluation(int resourceCount)
        {
            if (_resourceLifetimes.Length < resourceCount)
            {
                _resourceLifetimes = new ResourceVirtualAllocInfo[MemoryHelper.AlignUp(resourceCount, 32)];
                _firstLifetimes = new LifetimeIndex[_resourceLifetimes.Length];
                _lastLifetimes = new LifetimeIndex[_resourceLifetimes.Length];
            }

            Array.Fill(_resourceLifetimes, new ResourceVirtualAllocInfo());

            Array.Fill(_firstLifetimes, new LifetimeIndex(-1, int.MaxValue, RenderEventType.InsertBarrier));
            Array.Fill(_lastLifetimes, new LifetimeIndex(-1, int.MaxValue, RenderEventType.InsertBarrier));

            _currentResourceCount = 0;
            _resourcesAcrossEngines.Clear();

            _allocator.ResetAllocator();
        }

        internal void FinishLifetimeEvaluation()
        {
            _firstLifetimes.Sort();
            _lastLifetimes.Sort();

            int lastEventIndexLast = -1;
            for (int i = _lastLifetimes.Length - 1; i >= 0; --i)
            {
                LifetimeIndex lifetime = _lastLifetimes[i];
                if (lifetime.ResourceIndex != -1)
                {
                    lastEventIndexLast = i;
                    break;
                }
            }

            int totalEventCount = Math.Max(_firstLifetimes[0].EventIndex, _lastLifetimes[lastEventIndexLast].EventIndex) + 1;
            if (_resourcesAcrossEngines.Count > 0)
            {
                HandleLifetimesAtBarrier(totalEventCount);
            }

            int currentEventIndex = 0;

            int firstIndex = 0;
            int lastIndex = 0;
            while (Math.Min(firstIndex, lastIndex) < _currentResourceCount)
            {
                int lastEventIndex = totalEventCount;
                int firstEventIndex = totalEventCount;

                for (; lastIndex < _currentResourceCount; ++lastIndex)
                {
                    ref LifetimeIndex index = ref _lastLifetimes[lastIndex];
                    if (index.EventIndex + 1 > currentEventIndex)
                    {
                        lastEventIndex = index.EventIndex;
                        break;
                    }

                    ref ResourceVirtualAllocInfo allocInfo = ref _resourceLifetimes[index.ResourceIndex];
                    if (allocInfo.Allocation.MemorySize != -1)
                    {
                        // _allocator.Free(allocInfo.Allocation);
                    }
                }

                for (; firstIndex < _currentResourceCount; ++firstIndex)
                {
                    ref LifetimeIndex index = ref _firstLifetimes[firstIndex];
                    if (index.EventIndex > currentEventIndex)
                    {
                        firstEventIndex = index.EventIndex;
                        break;
                    }

                    ref ResourceVirtualAllocInfo allocInfo = ref _resourceLifetimes[index.ResourceIndex];
                    if (allocInfo.Allocation.MemorySize == -1)
                    {
                        ResourceHandle resource = _resourceManager.GetResourceHandle(index.ResourceIndex);
                        (int alignment, int sizeInBytes) = _resourceManager.EvaluateMemoryForResource(resource);

                        allocInfo.ResourceIndex = index.ResourceIndex;
                        allocInfo.Allocation = _allocator.Allocate(alignment, sizeInBytes);
                        allocInfo.SizeInBytes = sizeInBytes;
                    }
                }

                currentEventIndex = Math.Max(lastEventIndex, firstEventIndex);
            }
        }

        internal void ExtendResourceLifetime(ResourceHandle resource, int eventIndex, RenderEventType eventType)
        {
            ref ResourceVirtualAllocInfo allocInfo = ref _resourceLifetimes[resource.Index];

            if (allocInfo.Range.Start == -1)
            {
                allocInfo.Range.Start = eventIndex;
                allocInfo.ResourceIndex = resource.Index;

                _firstLifetimes[resource.Index] = _lastLifetimes[resource.Index] = new LifetimeIndex(resource.Index, eventIndex, eventType);
                ++_currentResourceCount;
            }
            else
            {
                allocInfo.Range.End = eventIndex;

                ref LifetimeIndex lastIndex = ref _lastLifetimes[resource.Index];
                if (lastIndex.EventType != eventType && lastIndex.EventType != RenderEventType.InsertBarrier)
                {
                    _resourcesAcrossEngines.Add(resource.Index);
                }

                lastIndex = new LifetimeIndex(resource.Index, eventIndex, eventType);
            }
        }

        internal void HandleLifetimesAtBarrier(int eventIndex)
        {
            foreach (int lifetimeIndex in _resourcesAcrossEngines)
            {
                ref LifetimeIndex index = ref _lastLifetimes[lifetimeIndex];
                ref ResourceVirtualAllocInfo allocInfo = ref _resourceLifetimes[index.ResourceIndex];

                allocInfo.Range.End = eventIndex;
                index = new LifetimeIndex(index.ResourceIndex, eventIndex, RenderEventType.InsertBarrier);
            }

            _resourcesAcrossEngines.Clear();
        }

        internal int AllocatedMemory => _allocator.AllocatedMemory;

        internal ReadOnlySpan<ResourceVirtualAllocInfo> ResourceVirtualAllocs => _resourceLifetimes;

        internal ReadOnlySpan<LifetimeIndex> FirstLifetimes => _firstLifetimes;
        internal ReadOnlySpan<LifetimeIndex> LastLifetimes => _lastLifetimes;

    }

    internal readonly record struct LifetimeIndex(int ResourceIndex, int EventIndex, RenderEventType EventType) : IEquatable<LifetimeIndex>, IComparable<LifetimeIndex>
    {
        public int CompareTo(LifetimeIndex other)
        {
            int i0 = other.EventIndex == -1 ? int.MaxValue : other.EventIndex;
            int i1 = EventIndex == -1 ? int.MaxValue : EventIndex;
            return i1.CompareTo(i0);
        }

        public override int GetHashCode() => HashCode.Combine(ResourceIndex, EventIndex);
    }

    internal record struct ResourceVirtualAllocInfo
    {
        public IndexRange Range;
        public int ResourceIndex;
        public VirtualAllocation Allocation;
        public int SizeInBytes;

        public ResourceVirtualAllocInfo()
        {
            Range = new IndexRange(-1, -1);
            ResourceIndex = -1;
            Allocation = new VirtualAllocation(-1, -1, -1);
        }
    }

    internal readonly record struct PersistantAllocationInfo(VirtualAllocation Allocation, IPlaybackResource Resource);
}
