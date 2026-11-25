using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Importers;

namespace PrimaryEditor.Assets
{
    public sealed class ImportScheduler
    {
        private AssetPipeline _pipeline;
        private ConcurrentDictionary<AssetId, Task> _runningImports;

        private int _scheduledImports;
        private int _finishedImports;

        internal ImportScheduler(AssetPipeline pipeline)
        {
            _pipeline = pipeline;
            _runningImports = new ConcurrentDictionary<AssetId, Task>();

            _scheduledImports = 0;
            _finishedImports = 0;
        }

        internal void UpdateImportStatus()
        {
            if (_scheduledImports == _finishedImports)
            {
                _scheduledImports = 0;
                _finishedImports = 0;
            }
        }

        internal void WaitForAllImports()
        {
            while (!_runningImports.IsEmpty)
            {
                Task.WaitAll([.. _runningImports.Values]);
            }
        }

        internal void ScheduleImport(Action action, AssetId id)
        {
            Task task = Task.Factory.StartNew(action, TaskCreationOptions.LongRunning);
            _runningImports.TryAdd(id, task);

            task.ContinueWith((_) =>
            {
                _runningImports.TryRemove(id, out _!);
                Interlocked.Increment(ref _finishedImports);
            });

            ++_scheduledImports;
        }

        public bool IsImportRunningFor(AssetId id) => _runningImports.ContainsKey(id);

        public int ScheduledImports => _scheduledImports;
        public int FinishedImports => _finishedImports;
    }
}
