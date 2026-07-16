using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Threading.Scheduling
{
    internal sealed class QueueCircularArray
    {
        private int _logSize;
        private JobHandle[] _segment;

        internal QueueCircularArray(int logSize)
        {
            _logSize = logSize;
            _segment = new JobHandle[1 << logSize];
        }

        internal JobHandle Get(long i)
        {
            return _segment[i % Size];
        }

        internal void Put(long i, JobHandle o)
        {
            _segment[i % Size] = o;
        }

        internal QueueCircularArray Grow(long b, long t)
        {
            EngLog.Thread.Debug("Growing job queue from {f} to {t}", 1 << _logSize, 1 << (_logSize + 1));
            QueueCircularArray a = new QueueCircularArray(_logSize + 1);
            for (long i = t; i < b; ++i)
            {
                a.Put(i, Get(i));
            }
            return a;
        }

        internal QueueCircularArray Shrink(long b, long t)
        {
            EngLog.Thread.Debug("Shrunk job queue from {f} to {t}", 1 << _logSize, 1 << (_logSize + 1));
            Debug.Assert(_logSize > 1);
            QueueCircularArray a = new QueueCircularArray(_logSize - 1);
            for (long i = t; i < b; ++i)
            {
                a.Put(i, Get(i));
            }
            return a;
        }

        internal long Size => 1L << _logSize;
    }
}
