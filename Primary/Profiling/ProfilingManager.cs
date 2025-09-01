using Collections.Pooled;
using Primary.Common;
using Primary.Timing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Primary.Profiling
{
    public class ProfilingManager : IDisposable
    {
        private static ProfilingManager? s_instance = null;

        private ConcurrentDictionary<int, ThreadSubProfiler> _subProfilers;
        private Dictionary<int, ThreadProfilingTimestamps> _timestamps = new Dictionary<int, ThreadProfilingTimestamps>();

        private long _startTimestamp;
        private long _lastStartTimestamp;

        private bool _disposedValue;

        internal ProfilingManager()
        {
            s_instance = this;

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

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void BeginProfiling(string name, int hash)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!_subProfilers.TryGetValue(threadId, out ThreadSubProfiler? subProfiler))
            {
                subProfiler = new ThreadSubProfiler(this, threadId);
                _subProfilers.TryAdd(threadId, subProfiler);
            }

            subProfiler.BeginProfiling(ref name, hash);
        }

        public void EndProfiling(int hash)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (_subProfilers.TryGetValue(threadId, out ThreadSubProfiler? subProfiler))
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

                kvp.Value.ClearTimestamps();
            }
        }

        internal long TimestampFromStart => Stopwatch.GetTimestamp() - _startTimestamp;

        public Dictionary<int, ThreadProfilingTimestamps> Timestamps => _timestamps;
        public long StartTimestamp => _lastStartTimestamp;

        public static ProfilingManager Instance => NullableUtility.ThrowIfNull(s_instance);
        public static bool IncludeStacktrace = false;
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
}
