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
        public void BeginProfiling(ref string name, int hash, long timestamp)
        {
            _scopes.Push(new ThreadProfilingScope(name, ProfilingManager.IncludeStacktrace ? GetStacktrace() : null, hash, timestamp));
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

    internal readonly record struct ThreadProfilingScope(string Name, string? Stacktrace, long Hash, long StartTimestamp);
    public readonly record struct ProfilingTimestamp(string Name, string? Stacktrace, long ParentHash, int Depth, long StartTimestamp, long EndTimestamp);
}
