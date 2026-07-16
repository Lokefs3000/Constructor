using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Threading.Statistics
{
    public readonly record struct SchedulerStatistics
    {
        private readonly JobScheduler _scheduler;

        internal SchedulerStatistics(JobScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public int JobDataPoolSize => _scheduler.JobDataPool.Size;
        public int JobDataPoolCapacity => _scheduler.JobDataPool.Capacity;
    }
}
