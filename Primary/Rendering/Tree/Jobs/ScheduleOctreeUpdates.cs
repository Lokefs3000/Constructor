using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Components;
using Primary.Threading;
using Primary.Timing;

namespace Primary.Rendering.Tree.Jobs
{
    internal sealed class ScheduleOctreeUpdates : IJob
    {
        private readonly OctreeManager _manager;

        private readonly List<JobWorker> _workers;

        internal ScheduleOctreeUpdates(OctreeManager octreeManager)
        {
            _manager = octreeManager;

            _workers = new List<JobWorker>();
        }

        internal JobHandle ScheduleAll(JobHandle parentJob)
        {
            return JobScheduler.Schedule(this, parentJob, JobPriority.Realtime);
        }

        void IJob.Execute()
        {
            using RentedList<JobHandle> jobs = new RentedList<JobHandle>(_workers.Count);

            int workerIndex = 0;
            foreach (var (_, tree) in _manager.Regions)
            {
                if (tree.WantsInternalUpdate || tree.WantsExtentsRefit)
                {
                    JobWorker worker;
                    if (_workers.Count == workerIndex)
                    {
                        worker = new JobWorker();
                        _workers.Add(worker);

                        ++workerIndex;
                    }
                    else
                    {
                        worker = _workers[workerIndex++];
                    }

                    worker.Octree = tree;

                    JobHandle handle = JobScheduler.Schedule(worker, JobPriority.Realtime);
                    JobScheduler.Flush(handle);

                    jobs.Add(handle);
                }
            }

            if (!jobs.IsEmpty)
            {
                JobHandle monolith = jobs.Count > 1 ? JobScheduler.CombineAll(jobs.AsSpan()) : jobs[0];
                monolith.WaitForCompletion();
            }
        }

        private sealed class JobWorker : IJob
        {
            public RenderOctree? Octree;

            internal JobWorker()
            {

            }

            public void Execute()
            {
                if (Octree!.WantsInternalUpdate)
                {
                    //Octree.UpdateInternals();
                }

                if (Octree.WantsExtentsRefit)
                {
                    //Octree.RefitExtents();
                }

                Octree = null;
            }
        }
    }
}
