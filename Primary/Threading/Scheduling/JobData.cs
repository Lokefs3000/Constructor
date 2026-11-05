using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Collections;
using Primary.Common;

namespace Primary.Threading.Scheduling
{
    internal sealed class JobData : IDisposable
    {
        private readonly JobScheduler _scheduler;

        private readonly Lock _lock;
        private readonly ManualResetEventSlim _waitHandle;

        private JobPriority _priority;
        private JobFlags _flags;
        private JobStatus _status;

        private long _version;

        private RentedList<JobData> _dependents;
        private int _runningDependencies;

        private IJob? _jobToExecute;

        internal JobData(JobScheduler scheduler, long arrayId)
        {
            _scheduler = scheduler;

            _lock = new Lock();
            _waitHandle = new ManualResetEventSlim(false);

            _priority = JobPriority.Low;
            _flags = JobFlags.None;
            _status = JobStatus.Pooled;

            _version = 0;

            _dependents = new RentedList<JobData>();
            _runningDependencies = 0;

            _jobToExecute = null;
        }

        public void Dispose()
        {
            _waitHandle.Set();
            _waitHandle.Dispose();

            _dependents.Dispose();

            _status = JobStatus.Disposed;
        }

        internal JobHandle SetupForNewJob(JobPriority priority, IJob? job, JobFlags flags)
        {
            // There shouldn't be any need for this but as a safety incase something happened with setting it
            _waitHandle.Set();
            _waitHandle.Reset();

            _priority = priority;
            _flags = flags;
            _status = JobStatus.Unbuilt;

            _dependents.Dispose();
            _runningDependencies = 0;

            _jobToExecute = job;

            _enqueueId = -100;
            _enqueuCount = 0;
            _workingCount = 0;

            return new JobHandle(this, _version);
        }

        internal void Finish()
        {
            if (_status != JobStatus.Unbuilt)
                throw new InvalidOperationException("Job must be unbuilt in order to call finish");

            _status = _flags.HasFlags(JobFlags.HasDependencies) ? JobStatus.WaitingOnDependencies : JobStatus.WaitingOnSubmit;
        }

        internal void Cancel()
        {
            if (_status != JobStatus.Unbuilt)
                throw new InvalidOperationException("Job must be unbuilt in order to call cancel");

            _waitHandle.Set();

            _status = JobStatus.Pooled;
            _jobToExecute = null;
        }

        internal bool AddDependency(JobHandle jobToDependOn)
        {
            if (jobToDependOn.Job == this)
                return false;

            if (!_flags.HasFlags(JobFlags.HasDependencies))
                throw new InvalidOperationException("Job must be created with dependency capable flags");
            if (!jobToDependOn.IsValid)
                throw new InvalidOperationException("Trying to add invalid job as dependency");

            if (Status != JobStatus.Unbuilt)
                throw new InvalidOperationException("Dependency can only be added to an unbuilt job");

            using (jobToDependOn.Job._lock.EnterScope())
            {
                if (jobToDependOn.HasCompleted)
                    return false;

                Interlocked.Increment(ref _runningDependencies);
                jobToDependOn.Job._dependents.Add(this);

                return true;
            }
        }

        internal void TrySchedule(bool insistOnDependencyFinish = false)
        {
            // Only schedule if the job is waiting on submission because either:
            //  a) The job is newly setup and is waiting to be executed
            //  b) The job has dependencies and is trying to be executed because a dependency finished

            // If the job has dependencies
            if (_flags.HasFlags(JobFlags.HasDependencies))
            {
                if (_status != JobStatus.WaitingOnDependencies)
                {
                    return;
                }

                // Allow skipping this check if we are sure all dependencies have finished
                if (insistOnDependencyFinish || Interlocked.CompareExchange(ref _runningDependencies, 0, 0) == 0)
                {
                    JobStatus status = Interlocked.CompareExchange(ref _status, JobStatus.IsSubmitted, JobStatus.WaitingOnDependencies);
                    if (status == JobStatus.WaitingOnDependencies)
                    {
                        _enqueueId = JobScheduler.Instance.FindWorkerForThread(Environment.CurrentManagedThreadId)?.WorkerIndex ?? -100;
                        ++_enqueuCount;

                        _scheduler.EnqueueWithPriority(this, _priority);
                    }
                }
            }
            else
            {
                if (_status != JobStatus.WaitingOnSubmit)
                {
                    return;
                }

                JobStatus status = Interlocked.CompareExchange(ref _status, JobStatus.IsSubmitted, JobStatus.WaitingOnSubmit);
                if (status == JobStatus.WaitingOnSubmit)
                {
                    _enqueueId = JobScheduler.Instance.FindWorkerForThread(Environment.CurrentManagedThreadId)?.WorkerIndex ?? -100;
                    ++_enqueuCount;

                    _scheduler.EnqueueWithPriority(this, _priority);
                }
            }
        }

        internal void ExecuteJob()
        {
#if DEBUG
            if (Interlocked.CompareExchange(ref _runningDependencies, 0, 0) > 0)
                throw new InvalidOperationException("Job scheduled but has unfinished dependencies");
#endif

            ++_workingCount;

            bool didThrow = false;
            if (!HasThrown)
            {
                try
                {
                    _jobToExecute?.Execute();
                }
                catch (Exception ex)
                {
                    EngLog.Thread.Error(ex, "Exeception occured trying to execute job");
                    Volatile.Write(ref Unsafe.As<JobFlags, byte>(ref _flags), (byte)(Unsafe.BitCast<byte, JobFlags>(Volatile.Read(ref Unsafe.As<JobFlags, byte>(ref _flags))) | JobFlags.ThrewException));
                    didThrow = true;
                }
            }

            Interlocked.Increment(ref _version);

            using (_lock.EnterScope())
            {
                foreach (JobData job in _dependents)
                {
                    if (didThrow)
                    {
                        Volatile.Write(ref Unsafe.As<JobFlags, byte>(ref job._flags), (byte)(Unsafe.BitCast<byte, JobFlags>(Volatile.Read(ref Unsafe.As<JobFlags, byte>(ref job._flags))) | JobFlags.ThrewException));
                    }

                    // Remove this as a dependency and try to schedule
                    int runningDependencies = Interlocked.Decrement(ref job._runningDependencies);
                    if (runningDependencies == 0)
                    {
                        // It is possible for this to fail if the jobs creator flushes right after this is decremented
                        job.TrySchedule(true);
                    }
                }

                _dependents.Dispose();
            }

            int hash = GetHashCode();

            JobStatus status = Interlocked.Exchange(ref _status, JobStatus.Pooled);
            if (status != JobStatus.IsSubmitted)
                throw new InvalidOperationException("Job should be in submitted state");

            _jobToExecute = null;

            _waitHandle.Set();
            _submitId = JobScheduler.Instance.FindWorkerForThread(Environment.CurrentManagedThreadId)?.WorkerIndex ?? -100;
            _scheduler.ReturnJobDataToPool(this);
        }

        private int _submitId;
        private int _workingCount;

        private int _enqueuCount;
        private int _enqueueId;

        internal JobScheduler Scheduler => _scheduler;

        internal ManualResetEventSlim WaitHandle => _waitHandle;

        internal long Version => Interlocked.Read(ref _version);
        internal bool HasDependencies => _flags.HasFlags(JobFlags.HasDependencies);
        internal bool HasThrown => (Volatile.Read(ref Unsafe.As<JobFlags, byte>(ref _flags)) & (byte)JobFlags.ThrewException) != 0;

        internal JobPriority Priority => _priority;
        internal JobFlags Flags => Unsafe.BitCast<byte, JobFlags>(Volatile.Read(ref Unsafe.As<JobFlags, byte>(ref _flags)));
        internal JobStatus Status => Unsafe.BitCast<byte, JobStatus>(Volatile.Read(ref Unsafe.As<JobStatus, byte>(ref _status)));

        internal static bool CompareVersionForCompletion(long currentVersion, long comparisonVersion) => currentVersion > comparisonVersion;
    }

    internal enum JobStatus : byte
    {
        Pooled = 0,
        Unbuilt,
        WaitingOnDependencies,
        WaitingOnSubmit,
        IsSubmitted,
        Disposed
    }

    [Flags]
    internal enum JobFlags : byte
    {
        None = 0,
        ThrewException = 1 << 0,
        HasDependencies = 1 << 1,
        IsMonolithic = 1 << 2
    }

#if DEBUG
    internal enum JobType : byte
    {
        Worker,
        Monolith
    }
#endif
}
