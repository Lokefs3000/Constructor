using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Threading.Scheduling;

namespace Primary.Threading
{
    internal sealed class ThreadWorker : IDisposable
    {
        private readonly JobScheduler _scheduler;
        private readonly int _workerIndex;

        private readonly WorkerMode _mode;
        private readonly ManualResetEventSlim? _notification;

        private readonly Thread? _thisThread;
        private readonly int _thisThreadId;

        private readonly ConcurrentQueue<JobHandle>[] _incomingQueue;
        private readonly JobHandleQueue[] _jobQueue;

        private bool _isActive;
        // For some reason when set to true from the main thread this causes issues with things being submitted out of order and other odd behaviour
        private bool _canAcceptJobs;

        private bool _disposedValue;

        internal ThreadWorker(JobScheduler scheduler, int index, WorkerMode workerMode, CancellationTokenSource cts)
        {
            _scheduler = scheduler;
            _workerIndex = index;

            _mode = workerMode;

            if (workerMode == WorkerMode.Background)
            {
                _notification = new ManualResetEventSlim(false);
                _thisThread = new Thread(() => ThreadProc(cts.Token))
                {
                    Name = $"BgWorker{index}",
                    IsBackground = true,
                    Priority = ThreadPriority.Normal,
                };

                _thisThreadId = _thisThread.ManagedThreadId;
            }
            else
            {
                _notification = null;
                _thisThread = null;
                _thisThreadId = -1;
            }

            _incomingQueue = new ConcurrentQueue<JobHandle>[3];
            _jobQueue = new JobHandleQueue[3];

            for (int i = 0; i < _incomingQueue.Length; i++)
            {
                _incomingQueue[i] = new ConcurrentQueue<JobHandle>();
                _jobQueue[i] = new JobHandleQueue(5);
            }

            _canAcceptJobs = workerMode == WorkerMode.Background;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_mode == WorkerMode.Background)
                    {
                        _notification!.Set();

                        _thisThread!.Join();
                        _notification!.Dispose();
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

        private void ThreadProc(CancellationToken token)
        {
            try
            {
                Guard.IsTrue(_mode == WorkerMode.Background);

                _isActive = true;
                while (!token.IsCancellationRequested)
                {
                    for (int i = 0; i < _incomingQueue.Length; ++i)
                    {
                        ConcurrentQueue<JobHandle> incomingQueue = _incomingQueue[i];
                        JobHandleQueue currentQueue = _jobQueue[i];

                        while (currentQueue.Size < (1 << 5) && incomingQueue.TryDequeue(out JobHandle handle))
                        {
                            currentQueue.Push(handle);
                        }
                    }

                    JobHandle jobToExecute = GetNextJob();
                    if (jobToExecute.IsValid)
                    {
                        jobToExecute.Job.ExecuteJob();
                    }
                    else
                    {
                        // No events could be popped or stolen so we wait for more
                        // Though we wake up occasionally incase a different thread has work to be stolen
                        _isActive = false;
                        _notification!.Wait(2000, token);
                        _notification.Reset();

                        _isActive = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                EngLog.Thread.Fatal(ex, "An exception occured within a worker thread ;-;");
                throw;
            }
        }

        internal void StartWorker()
        {
            _thisThread?.Start();
        }

        internal void WorkUntilCompletion(JobHandle? jobToWaitFor, CancellationToken token)
        {
            try
            {
                _isActive = true;
                // if (_mode == WorkerMode.Foreground)
                //     Volatile.Write(ref _canAcceptJobs, true);
            
                while (!(jobToWaitFor.HasValue && jobToWaitFor.DangerousGetValueOrNullReference().HasCompleted) && !token.IsCancellationRequested)
                {
                    for (int i = 0; i < _incomingQueue.Length; ++i)
                    {
                        ConcurrentQueue<JobHandle> incomingQueue = _incomingQueue[i];
                        JobHandleQueue currentQueue = _jobQueue[i];
            
                        while (currentQueue.Size < (1 << 5) && incomingQueue.TryDequeue(out JobHandle handle))
                        {
                            currentQueue.Push(handle);
                        }
                    }
            
                    JobHandle jobToExecute = GetNextJob();
                    if (jobToExecute.IsValid)
                    {
                        jobToExecute.Job.ExecuteJob();
                    }
                    else
                    {
                        // Were are all out of every event so it not having been completed yet is a bug and we just give up and exit
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                EngLog.Thread.Warning(ex, "An exception occured inside while waiting for a job");
            }

            if (_mode == WorkerMode.Foreground)
            {
                Volatile.Write(ref _canAcceptJobs, false);
                _isActive = false;
            
                // Send incoming and queued events back to the scheduler
                JobHandle incomingJobHandle;
                while (true)
                {
                    bool hasHadAnyJobs = false;
                    for (int i = 0; i < _incomingQueue.Length; ++i)
                    {
                        ConcurrentQueue<JobHandle> incomingQueue = _incomingQueue[i];
                        JobHandleQueue currentQueue = _jobQueue[i];
            
                        if (!currentQueue.IsEmpty)
                        {
                            while ((incomingJobHandle = currentQueue.Pop()).IsValid)
                            {
                                _scheduler.EnqueueWithPriority(incomingJobHandle, (JobPriority)i);
                            }
            
                            hasHadAnyJobs = true;
                        }
            
                        while (incomingQueue.TryDequeue(out incomingJobHandle))
                        {
                            _scheduler.EnqueueWithPriority(incomingJobHandle, (JobPriority)i);
                            hasHadAnyJobs = true;
                        }
                    }
            
                    if (!hasHadAnyJobs)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            }

            jobToWaitFor?.WaitForCompletion(true);
        }

        internal void SubmitJobToQueue(JobHandle jobHandle, JobPriority priority)
        {
            _incomingQueue[(int)priority].Enqueue(jobHandle);
            _notification?.Set();
        }

        private JobHandle GetNextJob()
        {
            // Iterate for each priority
            for (int i = 0; i < _jobQueue.Length; ++i)
            {
                JobHandleQueue jobQueue = _jobQueue[i];
                JobHandle handle = jobQueue.Pop();

                if (handle.IsValid)
                {
                    Debug.Assert(handle.IsSubmitted && !handle.HasCompleted);
                    return handle;
                }
                else
                {
                    // Get a random start index and try to steal a job with equal priority from each worker
                    int workerIndex = _scheduler.GetRandomWorkerIndex();
                    ReadOnlySpan<ThreadWorker> workers = _scheduler.Workers;

                    ThreadWorker? possibleVictim = null;

                    for (int j = 0; j < workers.Length; ++j)
                    {
                        if (workerIndex != _workerIndex)
                        {
                            ThreadWorker worker = workers[workerIndex];
                            if ((handle = worker._jobQueue[i].Steal()).IsValid)
                            {
                                Debug.Assert(handle.IsSubmitted && !handle.HasCompleted);
                                return handle;
                            }
                            else if (handle.IsAbort)
                            {
                                possibleVictim = worker;
                            }
                        }

                        if (++workerIndex == workers.Length)
                            workerIndex = 0;
                    }

                    // Since it was aborted there is still a chance that we can steal something by waiting a tiny bit
                    if (possibleVictim != null && (handle = possibleVictim._jobQueue[i].Steal()).IsValid)
                    {
                        Debug.Assert(handle.IsSubmitted && !handle.HasCompleted);
                        return handle;
                    }
                }
            }

            Thread.Yield();
            return JobHandleQueue.Empty;
        }

        internal int ThreadId => _thisThreadId;

        internal bool IsActive => _isActive;
        internal bool CanAcceptJobs => _canAcceptJobs;

        private static int s_globalCounter = 0;
        private static Lock s_globalLock = new Lock();
    }

    internal enum WorkerMode : byte
    {
        Background = 0,
        Foreground
    }
}
