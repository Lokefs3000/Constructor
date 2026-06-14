using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Profiling
{
    public sealed class GCProfiler : IDisposable
    {
        private readonly ProfilingManager _manager;

        private Listener _gcListener;

        private long _allocationRate;
        private long _currentMemoryUsage;

        private long _lastAllocated;

        private ConcurrentQueue<EventUpdateData> _eventUpdates;

        private CircularBuffer<TrackedGCMarker> _markers;
        private HashSet<TrackedGCHandle> _handles;

        private EventStartData _lastGcStartData;
        private long _lastGcTimestamp;
        private TimeSpan _lastGcStartTime;

        private bool _disposedValue;

        internal GCProfiler(ProfilingManager manager)
        {
            _manager = manager;

            _gcListener = new Listener(this);

            _allocationRate = 0;
            _currentMemoryUsage = 0;

            _lastAllocated = 0;

            _eventUpdates = new ConcurrentQueue<EventUpdateData>();

            _markers = new CircularBuffer<TrackedGCMarker>(MaxTrackedMarkerCount);
            _handles = new HashSet<TrackedGCHandle>();

            _lastGcStartData = default;
            _lastGcTimestamp = -1;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _gcListener.Dispose();

                _disposedValue = true;
            }
        }

        ~GCProfiler()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void PrepareForNewFrame()
        {
            long currentUsage = GC.GetTotalMemory(false);

            _allocationRate = Math.Max(GC.GetTotalAllocatedBytes() - _lastAllocated, 0);
            _currentMemoryUsage = currentUsage;

            _lastAllocated = GC.GetTotalAllocatedBytes();

            while (_eventUpdates.TryDequeue(out EventUpdateData updateData))
            {
                switch (updateData.Type)
                {
                    case EventType.Start:
                        {
                            _lastGcStartData = updateData.StartData;
                            _lastGcTimestamp = updateData.Timestamp;
                            break;
                        }
                    case EventType.End:
                        {
                            if (_lastGcTimestamp != -1)
                            {
                                GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
                                TimeSpan fullTime = TimeSpan.Zero;

                                foreach (TimeSpan span in memoryInfo.PauseDurations)
                                {
                                    fullTime += span;
                                }

                                _markers.PushBack(new TrackedGCMarker((byte)_lastGcStartData.Depth, _lastGcStartData.Reason, _lastGcStartData.Type, (int)fullTime.Ticks, Time.FrameIndex));
                            }
                            else
                                EngLog.Core.Warning("Recieved GC {ev1} event before any start {ev2}", EventType.End, EventType.Start);

                            _lastGcTimestamp = -1;
                            _lastGcStartTime = updateData.EndData.EndTime;
                            break;
                        }
                    case EventType.SetGCHandle:
                        {
                            _handles.Add(new TrackedGCHandle(updateData.SetGCHandleData.HandleId, updateData.SetGCHandleData.Type));
                            break;
                        }
                    case EventType.DestroyGCHandle:
                        {
                            _handles.Remove(new TrackedGCHandle(updateData.SetGCHandleData.HandleId, default));
                            break;
                        }
                }
            }
        }

        private void AddNewEvent(EventUpdateData updateData) => _eventUpdates.Enqueue(updateData);

        public long AllocationRate => _allocationRate;
        public long CurrentMemoryUsage => _currentMemoryUsage;

        public ROCircularBuffer<TrackedGCMarker> Markers => _markers;
        public IReadOnlySet<TrackedGCHandle> Handles => _handles;

        public const int MaxTrackedMarkerCount = 30;

        // based on: https://github.com/Ky7m/DemoCode/blob/main/ApplicationDiagnosticsNetCore/EventListenerSample/SimpleGCEventListener.cs
        private sealed class Listener : EventListener
        {
            private readonly GCProfiler _profiler;

            internal Listener(GCProfiler profiler)
            {
                _profiler = profiler;
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
                {
                    // and collect information pertaining to garbage collection.
                    EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)GCKeyword);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventId == GCStart_V2)
                {
                    if (eventData.Payload == null || eventData.PayloadNames == null)
                        return;

                    const int ExpectedValues = 3;
                    int assignedValues = 0;

                    int depth = 0;
                    GCMarkerReason reason = GCMarkerReason.Empty;
                    GCMarkerType type = GCMarkerType.Background;

                    for (int i = 0; i < eventData.Payload.Count; i++)
                    {
                        string? payloadName = eventData.PayloadNames[i];
                        object? payloadValue = eventData.Payload[i];

                        if (payloadName == null || payloadValue == null)
                            continue;

                        if (payloadName == "Depth")
                        {
                            depth = (int)(uint)payloadValue;
                            if (++assignedValues == ExpectedValues)
                                break;
                        }
                        else if (payloadName == "Reason")
                        {
                            reason = (GCMarkerReason)(uint)payloadValue;
                            if (++assignedValues == ExpectedValues)
                                break;
                        }
                        else if (payloadName == "Type")
                        {
                            type = (GCMarkerType)(uint)payloadValue;
                            if (++assignedValues == ExpectedValues)
                                break;
                        }
                    }
                    
                    if (assignedValues == ExpectedValues)
                        _profiler.AddNewEvent(new EventStartData(depth, reason, type, GC.GetTotalPauseDuration()));
                }
                else if (eventData.EventId == GCEnd_V1)
                {
                    _profiler.AddNewEvent(new EventEndData(GC.GetTotalPauseDuration()));
                }
                else if (eventData.EventId == GCFinalizersBegin_V1)
                {
                    _profiler.AddNewEvent(EventType.FinalizersBegin);
                }
                else if (eventData.EventId == GCFinalizersEnd_V1)
                {
                    _profiler.AddNewEvent(EventType.FinalizersEnd);
                }
                else if (eventData.EventId == SetGCHandle)
                {
                    if (eventData.Payload == null || eventData.PayloadNames == null)
                        return;

                    const int ExpectedValues = 2;
                    int assignedValues = 0;

                    nint handleId = nint.Zero;
                    GCHandleType handleType = GCHandleType.Normal;

                    for (int i = 0; i < eventData.Payload.Count; i++)
                    {
                        string? payloadName = eventData.PayloadNames[i];
                        object? payloadValue = eventData.Payload[i];

                        if (payloadName == null || payloadValue == null)
                            continue;

                        if (payloadName == "HandleID")
                        {
                            handleId = (nint)payloadValue;
                            if (++assignedValues == ExpectedValues)
                                break;
                        }
                        else if (payloadName == "Kind")
                        {
                            handleType = (GCHandleType)(uint)payloadValue;
                            if (++assignedValues == ExpectedValues)
                                break;
                        }
                    }

                    if (assignedValues == ExpectedValues)
                        _profiler.AddNewEvent(new EventSetGCHandleData(handleId, handleType));
                }
                else if (eventData.EventId == DestroyGCHandle)
                {
                    if (eventData.Payload == null || eventData.PayloadNames == null)
                        return;

                    const int ExpectedValues = 1;
                    int assignedValues = 0;

                    nint handleId = nint.Zero;

                    for (int i = 0; i < eventData.Payload.Count; i++)
                    {
                        string? payloadName = eventData.PayloadNames[i];
                        object? payloadValue = eventData.Payload[i];

                        if (payloadName == null || payloadValue == null)
                            continue;

                        if (payloadName == "HandleID")
                        {
                            handleId = (nint)payloadValue;
                            if (++assignedValues == ExpectedValues)
                                break;
                        }
                    }

                    if (assignedValues == ExpectedValues)
                        _profiler.AddNewEvent(new EventDestroyGCHandleData(handleId));
                }
            }

            private const int GCKeyword = 0x0000001;

            private const int GCStart_V2 = 1;
            private const int GCEnd_V1 = 2;
            private const int GCFinalizersBegin_V1 = 14;
            private const int GCFinalizersEnd_V1 = 13;
            private const int SetGCHandle = 30;
            private const int DestroyGCHandle = 31;
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly record struct EventUpdateData
        {
            [FieldOffset(0)] public readonly EventType Type;
            [FieldOffset(1)] public readonly long Timestamp;
            [FieldOffset(9)] public readonly EventStartData StartData;
            [FieldOffset(9)] public readonly EventEndData EndData;
            [FieldOffset(9)] public readonly EventSetGCHandleData SetGCHandleData;
            [FieldOffset(9)] public readonly EventDestroyGCHandleData DestroyGCHandleData;

            public EventUpdateData(EventType type)
            {
                Type = type;
                Timestamp = Stopwatch.GetTimestamp();
            }

            public EventUpdateData(EventStartData startData)
            {
                Type = EventType.Start;
                Timestamp = Stopwatch.GetTimestamp();
                StartData = startData;
            }

            public EventUpdateData(EventEndData endData)
            {
                Type = EventType.End;
                Timestamp = Stopwatch.GetTimestamp();
                EndData = endData;
            }

            public EventUpdateData(EventSetGCHandleData setGCHandleData)
            {
                Type = EventType.SetGCHandle;
                Timestamp = Stopwatch.GetTimestamp();
                SetGCHandleData = setGCHandleData;
            }

            public EventUpdateData(EventDestroyGCHandleData destroyGCHandleData)
            {
                Type = EventType.DestroyGCHandle;
                Timestamp = Stopwatch.GetTimestamp();
                DestroyGCHandleData = destroyGCHandleData;
            }

            public static implicit operator EventUpdateData(EventType type) => new EventUpdateData(type);
            public static implicit operator EventUpdateData(EventStartData startData) => new EventUpdateData(startData);
            public static implicit operator EventUpdateData(EventEndData endData) => new EventUpdateData(endData);
            public static implicit operator EventUpdateData(EventSetGCHandleData setGCHandleData) => new EventUpdateData(setGCHandleData);
            public static implicit operator EventUpdateData(EventDestroyGCHandleData destroyGCHandleData) => new EventUpdateData(destroyGCHandleData);
        }

        private readonly record struct EventStartData(int Depth, GCMarkerReason Reason, GCMarkerType Type, TimeSpan StartTime);
        private readonly record struct EventEndData(TimeSpan EndTime);
        private readonly record struct EventSetGCHandleData(nint HandleId, GCHandleType Type);
        private readonly record struct EventDestroyGCHandleData(nint HandleId);

        private enum EventType : byte
        {
            Start = 0,
            End,
            FinalizersBegin,
            FinalizersEnd,
            SetGCHandle,
            DestroyGCHandle
        }
    }

    public readonly record struct TrackedGCMarker(byte Generation, GCMarkerReason Reason, GCMarkerType Type, long Duration, int FrameIndex);
    public readonly record struct TrackedGCHandle(nint HandleId, GCHandleType Type)
    {
        public override int GetHashCode() => HandleId.GetHashCode();
    }

    // https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events
    public enum GCMarkerReason : byte
    {
        SOHAllocation = 0x0,
        LOHAllocation = 0x4,
        Induced = 0x1,
        InducedNotForced = 0x7,
        OutOfSpaceSOH = 0x5,
        OutOfSpaceLOH = 0x7,
        LowMemory = 0x2,
        Empty = 0x3,
    }

    public enum GCMarkerType : byte
    {
        Background = 0x1,
        BlockingOutside = 0x0,
        BlockingDuring = 0x2
    }
}
