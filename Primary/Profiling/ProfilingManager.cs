using Collections.Pooled;
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

        private ConcurrentDictionary<int, ThreadSubProfiler> _subProfilers;
        private Dictionary<int, ThreadProfilingTimestamps> _timestamps = new Dictionary<int, ThreadProfilingTimestamps>();

        private long _startTimestamp;
        private long _lastStartTimestamp;

        private ProfilingOptions _options;

        private bool _disposedValue;

        internal ProfilingManager()
        {
            s_instance.Target = this;

            _subProfilers = new ConcurrentDictionary<int, ThreadSubProfiler>();
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
                subProfiler = new ThreadSubProfiler(@this, threadId);
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

                timestamps.Timestamps.Clear();
                timestamps.Timestamps.AddRange(kvp.Value.GetTimestamps());

                kvp.Value.ClearDataForNextFrame();
            }
        }

        internal long TimestampFromStart => Stopwatch.GetTimestamp() - _startTimestamp;

        public Dictionary<int, ThreadProfilingTimestamps> Timestamps => _timestamps;
        public long StartTimestamp => _lastStartTimestamp;

        public static ProfilingManager Instance => NullableUtility.ThrowIfNull(Unsafe.As<ProfilingManager>(s_instance.Target));
        public static bool IncludeStacktrace = false;

        public static ProfilingOptions Options { get => Instance._options; set => Instance._options = value; }
    }

    public record struct ThreadProfilingTimestamps
    {
        public int ThreadId;
        public PooledList<ProfilingTimestamp> Timestamps;

        public ThreadProfilingTimestamps()
        {
            ThreadId = 0;
            Timestamps = null;
        }
    }

    public enum ProfilingOptions : byte
    {
        None = 0,

        CollectAllocation = 1 << 0
    }
}
