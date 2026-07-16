using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Primary.Threading.Scheduling
{
    internal sealed class JobDataPoolCQ : IDisposable
    {
        private readonly JobScheduler _scheduler;
        private readonly ConcurrentQueue<JobData> _queue;

        private bool _disposedValue;

        internal JobDataPoolCQ(JobScheduler scheduler)
        {
            _scheduler = scheduler;
            _queue = new ConcurrentQueue<JobData>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_queue.TryDequeue(out JobData? jobData))
                    {
                        jobData.Dispose();
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

        internal JobData GetPooledJob()
        {
            if (_queue.TryDequeue(out JobData? result))
                return result;
            return new JobData(_scheduler, 0);
        }

        internal void ReturnPooledJob(JobData jobData)
        {
            _queue.Enqueue(jobData);
        }

        internal int Size => _queue.Count;
        internal int Capacity => int.MaxValue;
    }
}
