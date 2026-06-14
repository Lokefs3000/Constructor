using Collections.Pooled;
using CommunityToolkit.Diagnostics;
using Primary.Common;
using Primary.Timing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Profiling
{
    public class ProfilingManager : IDisposable
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private GCProfiler _gcProfiler;

        private ConcurrentDictionary<int, ThreadSubProfiler> _subProfilers;
        private Dictionary<int, ThreadProfilingTimestamps> _timestamps;

        private long _startTimestamp;
        private long _lastStartTimestamp;

        private ProfilingOptions _options;
        private int _historySize;

        private bool _disposedValue;

        internal ProfilingManager()
        {
            s_instance.Target = this;

            _gcProfiler = new GCProfiler(this);

            _subProfilers = new ConcurrentDictionary<int, ThreadSubProfiler>();
            _timestamps = new Dictionary<int, ThreadProfilingTimestamps>();

            _startTimestamp = -1;
            _lastStartTimestamp = -1;

            _options = ProfilingOptions.None;
            _historySize = 300;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var kvp in _subProfilers)
                    {
                        kvp.Value.Dispose();
                    }

                    _gcProfiler.Dispose();
                }

                s_instance.Target = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public static void BeginProfiling(string name, int hash)
        {
            ProfilingManager @this = Instance;
            long timestamp = @this.TimestampFromStart;

            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!@this._subProfilers.TryGetValue(threadId, out ThreadSubProfiler? subProfiler))
            {
                subProfiler = new ThreadSubProfiler(@this, Thread.CurrentThread);
                @this._subProfilers.TryAdd(threadId, subProfiler);
            }

            subProfiler.BeginProfiling(ref name, hash, timestamp);
        }

        public static void EndProfiling(int hash)
        {
            ProfilingManager @this = Instance;

            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (@this._subProfilers.TryGetValue(threadId, out ThreadSubProfiler? subProfiler))
            {
                subProfiler.EndProfiling(hash);
            }
        }

        public void StartProfilingForFrame()
        {
            _lastStartTimestamp = _startTimestamp;
            _startTimestamp = Time.TimestampForActiveFrame;

            foreach (var kvp in _timestamps)
            {
                kvp.Value.Timestamps.Clear();
                kvp.Value.Timestamps.TrimExcess();
            }

            foreach (var kvp in _subProfilers)
            {
                ref ThreadProfilingTimestamps timestamps = ref CollectionsMarshal.GetValueRefOrAddDefault(_timestamps, kvp.Key, out bool exists);
                if (!exists)
                {
                    timestamps.ThreadId = kvp.Key;
                    timestamps.Timestamps = new PooledList<ProfilingTimestamp>();
                }

                timestamps.StartTimestamp = _lastStartTimestamp;
                timestamps.ThreadName = kvp.Value.ThreadName;

                timestamps.Timestamps.Clear();
                timestamps.Timestamps.AddRange(kvp.Value.GetTimestamps());

                kvp.Value.ClearDataForNextFrame();
            }

            // update other profiling modules

            _gcProfiler.PrepareForNewFrame();
        }

        internal long TimestampFromStart => Stopwatch.GetTimestamp() - _startTimestamp;

        public GCProfiler GCProfiler => _gcProfiler;

        public Dictionary<int, ThreadProfilingTimestamps> Timestamps => _timestamps;
        public long StartTimestamp => _lastStartTimestamp;

        public int HistorySize
        {
            get => _historySize;
            set
            {
                Guard.IsGreaterThan(value, 0);
                _historySize = value;
            }
        }

        public static ProfilingManager Instance => NullableUtility.ThrowIfNull(Unsafe.As<ProfilingManager>(s_instance.Target));

        public static ProfilingOptions Options { get => Instance._options; set => Instance._options = value; }
    }

    public record struct ThreadProfilingTimestamps
    {
        public int ThreadId;
        public string ThreadName;
        public long StartTimestamp;
        public PooledList<ProfilingTimestamp> Timestamps;

        public ThreadProfilingTimestamps()
        {
            ThreadId = 0;
            ThreadName = string.Empty;
            Timestamps = null!;
        }
    }

    public enum ProfilingOptions : byte
    {
        None = 0,

        CollectAllocation = 1 << 0,
        CollectStacktrace = 1 << 1
    }
}
