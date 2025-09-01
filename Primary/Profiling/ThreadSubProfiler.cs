using Collections.Pooled;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.Profiling
{
    internal class ThreadSubProfiler : IDisposable
    {
        private readonly ProfilingManager _profiler;
        private int _threadId;

        private Stack<ThreadProfilingScope> _scopes;
        private PooledList<ProfilingTimestamp> _timestamps;

        private bool _disposedValue;

        internal ThreadSubProfiler(ProfilingManager profiler, int threadId)
        {
            _profiler = profiler;
            _threadId = threadId;

            _scopes = new Stack<ThreadProfilingScope>();
            _timestamps = new PooledList<ProfilingTimestamp>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timestamps.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public void BeginProfiling(ref string name, int hash)
        {
            _scopes.Push(new ThreadProfilingScope(name, ProfilingManager.IncludeStacktrace ? GetStacktrace() : null, hash, _profiler.TimestampFromStart));
        }

        public void EndProfiling(int hash)
        {
            ThreadProfilingScope scope = _scopes.Pop();

            if (_scopes.TryPeek(out ThreadProfilingScope parent))
                _timestamps.Add(new ProfilingTimestamp(scope.Name, scope.Stacktrace, parent.Hash, _scopes.Count, scope.StartTimestamp, _profiler.TimestampFromStart));
            else
                _timestamps.Add(new ProfilingTimestamp(scope.Name, scope.Stacktrace, -1, _scopes.Count, scope.StartTimestamp, _profiler.TimestampFromStart));
        }

        private void CaptureActive()
        {
            long end = _profiler.TimestampFromStart;

            long parentHash = -1;
            int depth = 0;
            foreach (ThreadProfilingScope scope in _scopes)
            {
                _timestamps.Add(new ProfilingTimestamp(scope.Name, scope.Stacktrace, parentHash, depth++, scope.StartTimestamp, end));
                parentHash = scope.Hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledList<ProfilingTimestamp> GetTimestamps()
        {
            CaptureActive();
            return _timestamps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ClearTimestamps()
        {
            _timestamps.Clear();
        }

        [StackTraceHidden]
        private static string GetStacktrace()
        {
            return Environment.StackTrace;
        }
    }

    internal readonly record struct ThreadProfilingScope
    {
        public readonly string Name;
        public readonly string? Stacktrace;
        public readonly long Hash;

        public readonly long StartTimestamp;

        public ThreadProfilingScope(string name, string? stacktrace, long hash, long startTimestamp)
        {
            Name = name;
            Stacktrace = stacktrace;
            Hash = hash;

            StartTimestamp = startTimestamp;
        }
    }

    public readonly record struct ProfilingTimestamp
    {
        public readonly string Name;
        public readonly string? Stacktrace;

        public readonly long ParentHash;
        public readonly int Depth;

        public readonly long StartTimestamp;
        public readonly long EndTimestamp;

        public ProfilingTimestamp(string name, string? stacktrace, long parentHash, int depth, long startTimestamp, long endTimestamp)
        {
            Name = name;
            Stacktrace = stacktrace;

            ParentHash = parentHash;
            Depth = depth;

            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
        }
    }
}
