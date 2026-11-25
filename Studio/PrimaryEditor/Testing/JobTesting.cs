using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Text;
using Primary;
using Primary.Collections;
using Primary.Threading;
using Primary.Threading.Statistics;

namespace PrimaryEditor.Testing
{
    internal static class JobTesting
    {
        internal static void Run()
        {
            using JobScheduler scheduler = (JobScheduler)Activator.CreateInstance(typeof(JobScheduler), true)!;

            // Test_JobDataPool(scheduler);

            // const int IterationsPermittedPerThread = 1 << 19;
            // 
            // EdLog.Testing.Information("Testing 'IJob'");
            // for (int i = 0; i < 7; ++i)
            // {
            //     int iterationCount = 1 << (i + 23);
            //     EdLog.Testing.Information("    Using work size of {c}", iterationCount);
            // 
            //     long timestampStart = Stopwatch.GetTimestamp();
            // 
            //     using RentedList<JobHandle> jobs = new RentedList<JobHandle>();
            //     for (int j = 0; j < iterationCount; j += IterationsPermittedPerThread)
            //     {
            //         JobTester1 job = new JobTester1
            //         {
            //             IterationStartIndex = j,
            //             IterationCount = Math.Min(iterationCount - j, IterationsPermittedPerThread)
            //         };
            // 
            //         JobHandle handle = JobScheduler.Schedule(job);
            //         jobs.Add(handle);
            //     }
            // 
            //     JobHandle monolithic = JobScheduler.CombineAll(jobs.AsSpan());
            //     JobScheduler.Flush(jobs.AsSpan());
            // 
            //     monolithic.WaitForCompletion();
            // 
            //     EdLog.Testing.Information("       Took {time}s to execute with {iters} iterations and {total} jobs", Stopwatch.GetElapsedTime(timestampStart).TotalSeconds, iterationCount, jobs.Count);
            // }

            Console.ReadKey();
        }

        private static void Test_JobDataPool(JobScheduler scheduler)
        {
            EdLog.Testing.Information("Testing integrity of job data pool");

            scheduler.WaitForAllJobs();

            SchedulerStatistics statistics = scheduler.Statistics;

            int currentSize = statistics.JobDataPoolSize;
            int currentCapacity = statistics.JobDataPoolCapacity;

            using RentedList<JobHandle> jobs = new RentedList<JobHandle>();
            for (int i = 0; i < 128; ++i)
            {
                jobs.Add(JobScheduler.Schedule(new BlankJob1()));
            }

            JobHandle monolith = JobScheduler.CombineAll(jobs.AsSpan());

            int afterScheduleSize = statistics.JobDataPoolSize;
            int afterScheduleCapacity = statistics.JobDataPoolCapacity;

            JobScheduler.Flush(jobs.AsSpan());
            monolith.WaitForCompletion();

            int finishSize = statistics.JobDataPoolSize;
            int finishCapacity = statistics.JobDataPoolCapacity;

            EdLog.Testing.Information("Test data results:");
            EdLog.Testing.Information("    Start: Size:{sz} Capacity:{cap}", currentSize, currentCapacity);

            EdLog.Testing.Information("    Schedule: Size:{sz} Capacity:{cap}", afterScheduleSize, afterScheduleCapacity);
            if (afterScheduleSize != jobs.Count + 1)
                EdLog.Testing.Error("       Job data pool size is below the expected size of {sz}", jobs.Count + 1);
            if (afterScheduleCapacity < afterScheduleSize)
                EdLog.Testing.Error("       Job data pool capacity is not large enough to hold pool");

            EdLog.Testing.Information("    Finish: Size:{sz} Capacity:{cap}", finishSize, finishCapacity);
            if (finishSize != 0)
                EdLog.Testing.Error("       Job data pool size is is not empty even after all work has finished");
        }

        private sealed class BlankJob1 : IJob
        {
            public void Execute()
            {
                Thread.Sleep(10);
            }
        }

        private sealed class JobTester1 : IJob
        {
            public int IterationStartIndex;
            public int IterationCount;

            public void Execute()
            {
                Vector512<float> goldenRatio = (Vector512.Create(1.0f) + Vector512.Sqrt(Vector512.Create(5.0f))) / Vector512.Create(2.0f);
                Vector512<float> goldenAngle = Vector512.Create(2.0f) * Vector512.Create(MathF.PI) * (Vector512.Create(1.0f) - (Vector512.Create(1.0f) / goldenRatio));

                int max = IterationCount + IterationStartIndex;
                for (int i = IterationStartIndex; i < max; ++i)
                {
                    Vector512<float> radius = Vector512.Create(15.0f) * Vector512.Sqrt(Vector512.ConvertToSingle(Vector512.Create(i) + Vector512<int>.Indices));
                    Vector512<float> theta = Vector512.ConvertToSingle(Vector512.Create(i) + Vector512<int>.Indices) * goldenAngle;

                    (Vector512<float> y, Vector512<float> x) = Vector512.SinCos(theta);
                }
            }
        }
    }
}
