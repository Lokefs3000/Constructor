using CommunityToolkit.HighPerformance;
using Primary.Rendering2.Memory;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Rendering2.Pass
{
    public unsafe sealed class FrameGraphTimeline : IDisposable
    {
        private SequentialLinearAllocator _allocator;

        private List<nint> _events;
        private List<int> _passes;

        private bool _disposedValue;

        internal FrameGraphTimeline()
        {
            _allocator = new SequentialLinearAllocator(128);

            _events = new List<nint>();
            _passes = new List<int>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearTimeline()
        {
            _allocator.Reset();

            _events.Clear();
            _passes.Clear();
        }

        internal void AddRasterEvent(int passIndex)
        {
            nint ptr = _allocator.Allocate(Unsafe.SizeOf<TimelineRasterEvent>());

            _events.Add(ptr);
            _passes.Add(passIndex);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new TimelineRasterEvent(TimelineEventType.Raster, passIndex));
        }

        internal void AddComputeEvent(int passIndex)
        {
            nint ptr = _allocator.Allocate(Unsafe.SizeOf<TimelineComputeEvent>());

            _events.Add(ptr);
            _passes.Add(passIndex);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new TimelineComputeEvent(TimelineEventType.Compute, passIndex));
        }

        internal void AddFenceEvent(TimelineFenceQueue queueToWait, TimelineFenceQueue queueToSignal)
        {
            nint ptr = _allocator.Allocate(Unsafe.SizeOf<TimelineFenceEvent>());
            _events.Add(ptr);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new TimelineFenceEvent(TimelineEventType.Fence, queueToWait, queueToSignal));
        }

        internal ReadOnlySpan<nint> Events => _events.AsSpan();
        internal ReadOnlySpan<int> Passes => _passes.AsSpan();

        public bool IsEmpty => _events.Count == 0 && _passes.Count == 0;
    }

    public enum TimelineEventType : byte
    {
        Raster = 0,
        Compute,
        Fence
    }

    public readonly record struct TimelineRasterEvent(TimelineEventType Type, int PassIndex);
    public readonly record struct TimelineComputeEvent(TimelineEventType Type, int PassIndex);
    public readonly record struct TimelineFenceEvent(TimelineEventType Type, TimelineFenceQueue QueueToWait, TimelineFenceQueue QueueToSignal);

    public enum TimelineFenceQueue : byte
    {
        Graphics = 0,
        Compute,
        Copy
    }
}
