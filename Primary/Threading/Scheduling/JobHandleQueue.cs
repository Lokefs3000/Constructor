using System.Diagnostics;

namespace Primary.Threading.Scheduling
{
    // https://www.dre.vanderbilt.edu/~schmidt/PDF/work-stealing-dequeue.pdf
    internal sealed class JobHandleQueue
    {
        private long _bottom;
        private long _top;

        private long _lastTopValue;

        private volatile QueueCircularArray _activeArray;

        internal JobHandleQueue(int logSize)
        {
            Debug.Assert(logSize > 0);

            _bottom = 0;
            _top = 0;

            _lastTopValue = 0;

            _activeArray = new QueueCircularArray(logSize);
        }

        internal void Push(JobHandle o)
        {
            long b = Volatile.Read(ref _bottom);
            QueueCircularArray a = _activeArray;
            long size = b - _lastTopValue;
            if (size >= a.Size - 1)
            {
                long t = Interlocked.Read(ref _top);
                _lastTopValue = t;
                size = b - t;

                if (size >= a.Size - 1)
                {
                    a = a.Grow(b, t);
                    _activeArray = a;
                }
            }
            a.Put(b, o);
            Volatile.Write(ref _bottom, b + 1);
        }

        internal JobHandle Steal()
        {
            long b = Volatile.Read(ref _bottom);
            long t = Interlocked.Read(ref _top);
            QueueCircularArray a = _activeArray;
            long size = b - t;
            if (size <= 0) return Empty;
            JobHandle o = _activeArray.Get(t);
            if (!CompareExchangeBoolean(ref _top, t, t + 1))
                return Abort;
            return o;
        }

        internal JobHandle Pop()
        {
            long b = Volatile.Read(ref _bottom);
            QueueCircularArray a = _activeArray;
            b = b - 1;
            Volatile.Write(ref _bottom, b);
            long t = Interlocked.Read(ref _top);
            long size = b - t;
            if (size < 0)
            {
                Volatile.Write(ref _bottom, t);
                return Empty;
            }
            JobHandle o = a.Get(b);
            if (size > 0)
            {
                // PerhapsShrink(b, t);
                return o;
            }
            if (!CompareExchangeBoolean(ref _top, t, t + 1))
                o = Empty;
            Volatile.Write(ref _bottom, t + 1);
            return o;
        }

        private void PerhapsShrink(long b, long t)
        {
            QueueCircularArray a = _activeArray;
            if (b - t < a.Size/K)
            {
                QueueCircularArray aa = a.Shrink(b, t);
                _activeArray = aa;
            }
        }

        internal int Size => (int)(Volatile.Read(ref _bottom) - Interlocked.Read(ref _top));
        internal bool IsEmpty => Volatile.Read(ref _bottom) == Interlocked.Read(ref _top);

        private static bool CompareExchangeBoolean(ref long location0, long oldVal, long newVal)
        {
            return Interlocked.CompareExchange(ref location0, newVal, oldVal) == oldVal;
        }

        internal static readonly JobHandle Empty = new JobHandle(null!, long.MaxValue);
        internal static readonly JobHandle Abort = new JobHandle(null!, long.MaxValue - 1);

        private const int K = 128;
    }
}
