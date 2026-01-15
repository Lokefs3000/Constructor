using Collections.Pooled;
using Primary.Common;
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

        //TODO: investiage to see if collisions could happen and if they are impactful
        private int _scopeCount;

        private bool _collectAllocated;

        private bool _disposedValue;

        internal ThreadSubProfiler(ProfilingManager profiler, int threadId)
        {
            _profiler = profiler;
            _threadId = threadId;

            _scopes = new Stack<ThreadProfilingScope>();
            _timestamps = new PooledList<ProfilingTimestamp>();

            _scopeCount = 0;

            _collectAllocated = false;
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
            long startAllocated = _collectAllocated ? GC.GetAllocatedBytesForCurrentThread() : -1;
            if (_scopes.TryPeek(out ThreadProfilingScope scope))
                _scopes.Push(new ThreadProfilingScope(name, null, new ProfilingId(_threadId, GetScopeId(), HashCode.Combine(scope.Id.Hash, hash)), scope.Id.Hash, timestamp, startAllocated));
            else
                _scopes.Push(new ThreadProfilingScope(name, null, new ProfilingId(_threadId, GetScopeId(), hash), null, timestamp, startAllocated));
        }

        public void EndProfiling(int hash)
        {
            ThreadProfilingScope scope = _scopes.Pop();
            _timestamps.Add(new ProfilingTimestamp(scope.Name, scope.Stacktrace, scope.Id, _scopes.Count, scope.StartTimestamp, _profiler.TimestampFromStart, (int)(scope.StartAllocated != -1 ? GC.GetAllocatedBytesForCurrentThread() - scope.StartAllocated : 0)));
        }

        private void CaptureActive()
        {
            int depth = 0;
            foreach (ThreadProfilingScope scope in _scopes)
            {
                _timestamps.Add(new ProfilingTimestamp(scope.Name, scope.Stacktrace, scope.Id, depth++, scope.StartTimestamp, -1, (int)(scope.StartAllocated != -1 ? GC.GetAllocatedBytesForCurrentThread() - scope.StartAllocated : 0)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<ProfilingTimestamp> GetTimestamps()
        {
            CaptureActive();
            return _timestamps.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ClearDataForNextFrame()
        {
            _timestamps.Clear();
            _collectAllocated = FlagUtility.HasFlag(ProfilingManager.Options, ProfilingOptions.CollectAllocation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetScopeId()
        {
            if (_scopeCount >= ushort.MaxValue)
            {
                _scopeCount = 1;
                return 0;
            }

            return _scopeCount++;
        }

        [StackTraceHidden]
        private static string GetStacktrace()
        {
            return Environment.StackTrace;
        }
    }

    internal readonly record struct ThreadProfilingScope(string Name, string? Stacktrace, ProfilingId Id, int? PrevHashStack, long StartTimestamp, long StartAllocated);

    public readonly record struct ProfilingTimestamp(string Name, string? Stacktrace, ProfilingId Id, int Depth, long StartTimestamp, long EndTimestamp, int Allocated);
    public readonly record struct ProfilingId(int Code, int Hash)
    {
        public ProfilingId(int threadId, int timestampId, int hash) : this(threadId << 16 | timestampId, hash)
        {
            //TODO: this is prob not the best way to do this
            Debug.Assert(threadId <= ushort.MaxValue);
            Debug.Assert(timestampId <= ushort.MaxValue);
        }

        public int ThreadId => (Code >> 16 & 0xffff);
        public int TimestampId => (Code & 0xffff);
    }
}
