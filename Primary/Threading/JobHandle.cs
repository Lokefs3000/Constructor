using System;
using System.Collections.Generic;
using System.Text;
using Primary.Threading.Scheduling;

namespace Primary.Threading
{
    public readonly record struct JobHandle : IEquatable<JobHandle>
    {
        private readonly JobData _jobData;
        private readonly long _version;

        internal JobHandle(JobData job, long version)
        {
            _jobData = job;
            _version = version;
        }

        internal JobHandle(JobData job) : this(job, job.Version)
        {
        }

        public void WaitForCompletion(bool dontAllowSupport = false)
        {
            if (!HasCompleted)
            {
                if (dontAllowSupport || !_jobData.Scheduler.WaitForJobAndSupport(this))
                    _jobData.WaitHandle.Wait();
            }
        }

        public void WaitForCompletion(int millisecondsTimeout, bool strictTiming = false)
        {
            if (!HasCompleted)
            {
                // If how long we wait is important we can't rely on supporting the others since it could end up taking longer than the timeout 
                if (strictTiming || !_jobData.Scheduler.WaitForJobAndSupport(this))
                    _jobData.WaitHandle.Wait(millisecondsTimeout);
            }
        }

        public readonly bool HasCompleted => _version == long.MaxValue - 2 || JobData.CompareVersionForCompletion(_jobData.Version, _version);
        public readonly bool IsSubmitted => _jobData.Status == JobStatus.IsSubmitted;

        internal readonly JobData Job => _jobData;
        internal readonly long Version => _version;

        internal readonly bool IsEmpty => long.MaxValue == _version;
        internal readonly bool IsAbort => long.MaxValue - 1 == _version;
        internal readonly bool IsValid => _version < long.MaxValue - 1;

        public static JobHandle Completed => new JobHandle(default!, long.MaxValue - 2);
    }
}
