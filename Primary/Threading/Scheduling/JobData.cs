using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Collections;

namespace Primary.Threading.Scheduling
{
    internal sealed class JobData : IDisposable
    {
        private readonly JobScheduler _scheduler;
        private readonly long _arrayId;

        private readonly Lock _lock;
        private readonly ManualResetEventSlim _waitHandle;

        private JobPriority _priority;

        private long _version;
        private bool _hasThrown;
        private JobStatus _jobStatus;

        private bool _canBeManuallyFlushed;

        private RentedList<JobHandle> _dependents;
        private int _unfinishedDependencies;

        private IJob? _jobToExecute;

#if DEBUG
        private int _currentThreadId;
        private JobType _jobType;
#endif

        internal JobData(JobScheduler scheduler, long arrayId)
        {
            _scheduler = scheduler;
            _arrayId = arrayId;

            _lock = new Lock();
            _waitHandle = new ManualResetEventSlim(false);

            _priority = JobPriority.Low;

            _version = 0;
            _hasThrown = false;
            _jobStatus = JobStatus.Idle;

            _canBeManuallyFlushed = false;

            _dependents = new RentedList<JobHandle>();
            _unfinishedDependencies = 0;

            _jobToExecute = null;

#if DEBUG
            _currentThreadId = -1;
            _jobType = 0;
#endif
        }

        public void Dispose()
        {
            _waitHandle.Set();
            _waitHandle.Dispose();

            _jobStatus = JobStatus.Idle;
        }

        internal JobHandle SetupForNewJob(JobPriority priority, IJob? job, bool canBeManuallyFlushed)
        {
            // There shouldn't be any need for this but as a safety incase something happened with setting it
            _waitHandle.Set();
            _waitHandle.Reset();

            _priority = priority;

            _hasThrown = false;
            _jobStatus = JobStatus.Pending;

            _canBeManuallyFlushed = canBeManuallyFlushed;

            _dependents.Dispose();
            _unfinishedDependencies = 0;

            _jobToExecute = job;

#if DEBUG
            Interlocked.Exchange(ref _currentThreadId, -1);
            _jobType = job == null ? JobType.Monolith : JobType.Worker;
#endif

            return new JobHandle(this, _version);
        }

        internal void ExecuteJob()
        {
#if DEBUG
            if (Interlocked.CompareExchange(ref _currentThreadId, Environment.CurrentManagedThreadId, -1) != -1)
                throw new InvalidOperationException("Job executed on multiple threads at once!");

            // Keep a variable so i can see what job executed when debugging
            IJob? executedJob = _jobToExecute;
#endif

            // If this is already true that means a parent has thrown and we need to just forgo executing the job
            if (!_hasThrown)
            {
                try
                {
                    _jobToExecute?.Execute();
                }
                catch (Exception ex)
                {
                    EngLog.Thread.Error(ex, "Exeception occured trying to execute job");
                    _hasThrown = true;
                }
            }

            Interlocked.Increment(ref _version);

            // Would this even be required since we can deduce if a job needs to wait by relying on it's version
            using (_lock.EnterScope())
            {
                _jobToExecute = null;

                foreach (JobHandle dependent in _dependents)
                {
                    Debug.Assert(!dependent.HasCompleted);

                    JobData dependentJob = dependent.Job;
                    Volatile.Write(ref dependentJob._hasThrown, _hasThrown || Volatile.Read(ref dependentJob._hasThrown));

                    // Signal that a dependency (or parent) has finished and if no more are pending then submit
                    int newDependencyCount = Interlocked.Decrement(ref dependentJob._unfinishedDependencies);
                    if (newDependencyCount == 0)
                    {
                        dependentJob.TrySchedule();
                    }

                    // EngLog.Thread.Information("{x}", newDependencyCount);
                }

                // This has finished and becomes available for pooling again
                _dependents.Dispose();
            }

            Volatile.Write(ref Unsafe.As<JobStatus, byte>(ref _jobStatus), (byte)JobStatus.Idle);

            _waitHandle.Set();
            _scheduler.ReturnJobDataToPool(this);
        }

        internal bool TrySchedule()
        {
#if DEBUG
            //if (_currentThreadId != -1)
            //    throw new InvalidOperationException("Trying to schedule job that has executed but not pooled");
#endif

            // Only enqueue if we have been submitted and are ready to run
            // This is incase a dependency has completed and we are creating a monolith job so we don't finish before everything is done
            if (Interlocked.CompareExchange(ref _jobStatus, JobStatus.IsSubmitted, JobStatus.WaitingForSubmit) == JobStatus.WaitingForSubmit)
            {
                _scheduler.EnqueueWithPriority(this, _priority);
                return true;
            }

            return false;
        }

        internal bool AddDependent(JobHandle job)
        {
            if (job.Job == this)
                throw new InvalidOperationException("Cannot add self as dependent");
            if (job.Job._jobStatus != JobStatus.Pending)
                throw new InvalidOperationException("Only pending jobs can have dependencies added");

            if (job.IsValid && !job.HasCompleted)
            {
                using (_lock.EnterScope())
                {
                    if (_jobToExecute == null && Interlocked.CompareExchange(ref _unfinishedDependencies, 0, 0) == 0 && _jobStatus == JobStatus.Idle)
                        return false;

                    _dependents.Add(job);

                    Interlocked.Increment(ref job.Job._unfinishedDependencies);
                    Volatile.Write(ref job.Job._hasThrown, _hasThrown || Volatile.Read(ref job.Job._hasThrown));

                    return true;
                }
            }

            return false;
        }

        internal void SwitchToWaitAndTrySubmit(bool maySubmitEarly = false)
        {
#if DEBUG
            if (Status != JobStatus.Pending)
                throw new InvalidOperationException("Job must be in a pending state to do this");
#endif

            // We should only try to submit if we are trying to wait on something
            if (maySubmitEarly && Interlocked.CompareExchange(ref _unfinishedDependencies, 0, 0) == 0)
            {
                JobStatus status = Interlocked.Exchange(ref _jobStatus, JobStatus.IsSubmitted);
                Debug.Assert(status == JobStatus.Pending || status == JobStatus.WaitingForSubmit);

                _scheduler.EnqueueWithPriority(this, _priority);
            }
            else
            {
                JobStatus status = Interlocked.Exchange(ref _jobStatus, JobStatus.WaitingForSubmit);
                Debug.Assert(status == JobStatus.Pending);

                // Incase all dependencies finish before changing the job status but after checking
                if (maySubmitEarly && Interlocked.CompareExchange(ref _unfinishedDependencies, 0, 0) == 0)
                    TrySchedule();
            }
        }

        internal JobScheduler Scheduler => _scheduler;
        internal long ArrayId => _arrayId;

        internal ManualResetEventSlim WaitHandle => _waitHandle;

        internal long Version => Interlocked.Read(ref _version);
        internal bool HasThrown => Volatile.Read(ref _hasThrown);
        internal JobStatus Status => Unsafe.BitCast<byte, JobStatus>(Volatile.Read(ref Unsafe.As<JobStatus, byte>(ref _jobStatus)));

        internal bool CanBeManuallyFlushed => _canBeManuallyFlushed;

        internal static bool CompareVersionForCompletion(long currentVersion, long comparisonVersion) => currentVersion > comparisonVersion;
    }

    internal enum JobStatus : byte
    {
        /// <summary>Current job is doing nothing</summary>
        Idle = 0,

        /// <summary>Current job is waiting to be submitted</summary>
        WaitingForSubmit,

        /// <summary>Current job has been submitted</summary>
        IsSubmitted,

        /// <summary>Current job is pending</summary>
        Pending
    }

#if DEBUG
    internal enum JobType : byte
    {
        Worker,
        Monolith
    }
#endif
}
