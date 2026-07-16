using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Threading.Scheduling
{
    internal sealed class JobDataPool : IDisposable
    {
        private readonly JobScheduler _scheduler;

        private int _logSize;

        private Lock _growLock;
        private int _currentArrayVersion;
        private volatile ArrayBuffer _currentArrayBuffer;

        private long _bottom;
        private long _top;

        private bool _disposedValue;

        internal JobDataPool(JobScheduler scheduler, int logSize)
        {
            _scheduler = scheduler;

            _logSize = logSize;

            int actualSize = 1 << logSize;

            _growLock = new Lock();
            _currentArrayVersion = 0;
            _currentArrayBuffer = new ArrayBuffer(
                new JobData?[actualSize],
                new int[actualSize],
                _currentArrayVersion);

            _bottom = 0;
            _top = 0;

            for (int i = 0; i < actualSize; ++i)
            {
                _currentArrayBuffer.AvailableIndices[i] = i;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (ref JobData? jobData in _currentArrayBuffer.JobArray.AsSpan())
                    {
                        jobData?.Dispose();
                        jobData = null;
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

        /// <summary>Thread-safe</summary>
        internal JobData GetPooledJob()
        {
            long top = Volatile.Read(ref _top);
            long bottom = Volatile.Read(ref _bottom);

            ArrayBuffer arrayBuffer = _currentArrayBuffer;

            long currentArraySize = top - bottom;
            if (currentArraySize >= arrayBuffer.AvailableIndices.Length - 1)
            {
                GrowInternalArrayBuffer(top, bottom);
                arrayBuffer = _currentArrayBuffer;
            }

            long indexPosition = Interlocked.Increment(ref _top) % arrayBuffer.JobArray.Length;
            int jobArrayIndex = arrayBuffer.AvailableIndices[indexPosition];

            Debug.Assert(jobArrayIndex != -1);

            JobData jobData = (arrayBuffer.JobArray[jobArrayIndex] ??= new JobData(_scheduler, jobArrayIndex));
            return jobData;
        }

        /// <summary>Thread-safe</summary>
        internal void ReturnPooledJob(JobData jobData)
        {
            ArrayBuffer arrayBuffer = _currentArrayBuffer;

            int arrayIndex = (int)(jobData.ArrayId & 0xffffffff);
            int arrayVersion = (int)((jobData.ArrayId >> 32) & 0xffffffff);

            // Ensure we aren't overriding an already good index
            if (arrayBuffer.Version == arrayVersion)
            {
                long indexPosition = Interlocked.Increment(ref _bottom) % arrayBuffer.JobArray.Length;
                arrayBuffer.AvailableIndices[indexPosition] = arrayIndex;
            }
            else
            {
                jobData.Dispose();
            }
        }

        // This throws out all previous data because i can't currently devise a solution where we copy the queue properly
        // .. or maybe my understanding of threading is still lacking -_-?
        private void GrowInternalArrayBuffer(long tail, long head)
        {
            int currentVersion = _currentArrayVersion;
            using (_growLock.EnterScope())
            {
                // Was grown in a prior lock scope
                if (currentVersion != _currentArrayVersion)
                    return;

                ++_logSize;
                ++_currentArrayVersion;

                int actualSize = 1 << _logSize;

                ArrayBuffer newArrayBuffer = new ArrayBuffer(new JobData?[actualSize], new int[actualSize], _currentArrayVersion);
                for (int i = 0; i < actualSize; ++i)
                {
                    newArrayBuffer.AvailableIndices[i] = i;
                }

                _currentArrayBuffer = newArrayBuffer;
            }
        }

        public int Size => (int)(Volatile.Read(ref _top) - Volatile.Read(ref _bottom));
        public int Capacity => _currentArrayBuffer.JobArray.Length;

        private record class ArrayBuffer(JobData?[] JobArray, int[] AvailableIndices, int Version);
    }
}
