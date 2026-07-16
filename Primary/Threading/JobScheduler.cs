using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using Primary.Common;
using Primary.Components;
using Primary.Threading.Scheduling;
using Primary.Threading.Statistics;

namespace Primary.Threading
{
    public sealed class JobScheduler : IDisposable
    {
        private readonly int _mainThreadId;

        private readonly JobDataPoolCQ _jobDataPool;
        private readonly CancellationTokenSource _cancellationToken;

        private readonly ImmutableArray<ThreadWorker> _workers;

        private int _nextWorkerIndex;

        private bool _disposedValue;

        internal JobScheduler()
        {
            s_instance.Target = this;

            int logSize = AppArguments.GetValueOrDefault("job-initial-logsize", 6);
            Guard.IsGreaterThan(logSize, 0);

            int maxThreadCount = Math.Clamp((int)AppArguments.GetValueOrDefault<long>("job-max-threads", int.MaxValue), 1, Environment.ProcessorCount);

            _mainThreadId = Environment.CurrentManagedThreadId;

            _jobDataPool = new JobDataPoolCQ(this);
            _cancellationToken = new CancellationTokenSource();

            using RentedArray<ThreadWorker> workers = RentedArray<ThreadWorker>.Rent(maxThreadCount, true);
            for (int i = 0; i < maxThreadCount; ++i)
            {
                workers[i] = new ThreadWorker(this, i, i == 0 ? WorkerMode.Foreground : WorkerMode.Background, _cancellationToken);
            }

            _workers = [.. workers];

            _nextWorkerIndex = 0;

            // Start all workers now
            foreach (ThreadWorker worker in _workers)
            {
                worker.StartWorker();
            }

            EngLog.Thread.Information("Started #1+{c} workers for the job system", maxThreadCount - 1);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    WaitForAllJobs();

                    _cancellationToken.Cancel();

                    foreach (ThreadWorker worker in _workers)
                    {
                        worker.Dispose();
                    }

                    _cancellationToken.Dispose();
                    _jobDataPool.Dispose();
                }

                s_instance.Target = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ReturnJobDataToPool(JobData jobData)
        {
            _jobDataPool.ReturnPooledJob(jobData);
        }

        internal void EnqueueWithPriority(JobHandle jobHandle, JobPriority priority)
        {
            if (!jobHandle.HasCompleted && jobHandle.IsValid)
            {
                EnqueueWithPriority(jobHandle.Job, priority);
            }
        }

        internal void EnqueueWithPriority(JobData jobData, JobPriority priority)
        {
            while (true)
            {
                ThreadWorker threadWorker = _workers[_nextWorkerIndex = ++_nextWorkerIndex % _workers.Length];
                if (threadWorker.CanAcceptJobs)
                {
                    // The current job should not have been started yet
                    threadWorker.SubmitJobToQueue(new JobHandle(jobData), priority);

                    break;
                }
            }
        }

        internal bool WaitForJobAndSupport(JobHandle jobHandle)
        {
            if (jobHandle.HasCompleted)
                return true;

            ThreadWorker? workerForThisThread = GetWorkerForThisThread();
            if (workerForThisThread == null)
                return false;

            workerForThisThread.WorkUntilCompletion(jobHandle, _cancellationToken.Token);
            return true;
        }

        internal ThreadWorker? FindWorkerForThread(int threadId)
        {
            if (threadId == _mainThreadId)
                return _workers[0];

            for (int i = 1; i < _workers.Length; ++i)
            {
                ThreadWorker worker = _workers[i];
                if (worker.ThreadId == threadId)
                    return worker;
            }

            return null;
        }

        internal ThreadWorker? GetWorkerForThisThread()
        {
            int currentThreadId = Environment.CurrentManagedThreadId;
            return currentThreadId == _mainThreadId ? _workers[0] : FindWorkerForThread(currentThreadId);
        }

        internal int GetRandomWorkerIndex()
        {
            return Random.Shared.Next(_workers[0].CanAcceptJobs ? 0 : 1, _workers.Length - 1);
        }

        public void WaitForAllJobs()
        {
            ThreadWorker? thisWorker = GetWorkerForThisThread();
            thisWorker?.WorkUntilCompletion(null, _cancellationToken.Token);

            while (true)
            {
                bool hadActiveWorkers = false;
                foreach (ThreadWorker worker in _workers)
                {
                    if (worker != thisWorker && worker.IsActive)
                    {
                        hadActiveWorkers = true;
                        SpinWait.SpinUntil(() => !worker.IsActive);
                    }
                }

                // Ensure we absolutely have no active work and not any edge cases where something is scheduled in between
                if (hadActiveWorkers)
                    Thread.Yield();
                else
                    break;
            }
        }

        internal JobDataPoolCQ JobDataPool => _jobDataPool;

        internal ReadOnlySpan<ThreadWorker> Workers => _workers.AsSpan();

        public SchedulerStatistics Statistics => new SchedulerStatistics(this);

        public static JobHandle Schedule(IJob job, JobPriority priority = JobPriority.Normal)
        {
            JobScheduler scheduler = Instance;
            JobData jobData = scheduler._jobDataPool.GetPooledJob();

            JobHandle jobHandle = jobData.SetupForNewJob(priority, job);
            jobData.SwitchToWaitAndTrySubmit();

            return jobHandle;
        }

        public static JobHandle Schedule(IJob job, JobHandle parentJob, JobPriority priority = JobPriority.Normal)
        {
            JobScheduler scheduler = Instance;
            if (scheduler._workers.Length == 1)
            {
                job.Execute();
                return JobHandle.Completed;
            }

            JobData jobData = scheduler._jobDataPool.GetPooledJob();
            JobHandle jobHandle = jobData.SetupForNewJob(priority, job);

            if (parentJob.IsValid && !parentJob.HasCompleted)
            {
                if (!parentJob.Job.AddDependent(jobHandle))
                {
                    jobData.TrySchedule();
                }
            }
            else
            {
                jobData.TrySchedule();
            }

            jobData.SwitchToWaitAndTrySubmit(true);
            return jobHandle;
        }

        public static JobHandle Schedule(IJob job, ReadOnlySpan<JobHandle> parentJobs, JobPriority priority = JobPriority.Normal)
        {
            JobScheduler scheduler = Instance;
            if (scheduler._workers.Length == 1)
            {
                job.Execute();
                return JobHandle.Completed;
            }

            JobData jobData = scheduler._jobDataPool.GetPooledJob();
            JobHandle jobHandle = jobData.SetupForNewJob(priority, job);

            bool couldAddToAny = false;
            foreach (ref readonly JobHandle parentJob in parentJobs)
            {
                if (parentJob.IsValid && !parentJob.HasCompleted)
                {
                    if (parentJob.Job.AddDependent(jobHandle))
                    {
                        couldAddToAny = true;
                    }
                }
            }

            jobData.SwitchToWaitAndTrySubmit(true);
            return jobHandle;
        }

        public static JobHandle CombineAll(ReadOnlySpan<JobHandle> jobs, JobPriority priority = JobPriority.Realtime)
        {
            JobScheduler scheduler = Instance;
            JobData jobData = scheduler._jobDataPool.GetPooledJob();
            JobHandle jobHandle = jobData.SetupForNewJob(priority, null);

            bool areAnyJobsStillRunning = false;
            foreach (ref readonly JobHandle job in jobs)
            {
                if (job.IsValid && !job.HasCompleted)
                {
                    if (job.Job.AddDependent(jobHandle))
                        areAnyJobsStillRunning = true;
                }
            }

            if (!areAnyJobsStillRunning)
            {
                scheduler.ReturnJobDataToPool(jobData);
                return JobHandle.Completed;
            }
            else
            {
                jobData.SwitchToWaitAndTrySubmit(true);
                return jobHandle;
            }
        }

        public static void Flush(JobHandle job)
        {
            if (!job.HasCompleted && job.IsValid)
            {
                job.Job.TrySchedule();
            }
        }

        public static void Flush(ReadOnlySpan<JobHandle> jobs)
        {
            foreach (ref readonly JobHandle job in jobs)
            {
                if (!job.HasCompleted && job.IsValid)
                {
                    job.Job.TrySchedule();
                }
            }
        }

        private static readonly WeakReference s_instance = new WeakReference(null);
        public static JobScheduler Instance => Unsafe.As<JobScheduler>(s_instance.Target)!;
    }

    public enum JobPriority : byte
    {
        Realtime = 0,
        Normal,
        Low
    }
}
