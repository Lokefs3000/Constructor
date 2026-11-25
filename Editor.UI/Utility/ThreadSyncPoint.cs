using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.UI.Utility
{
    internal struct ThreadSyncPoint
    {
        private int _max;
        private int _counter;

        private ManualResetEventSlim _event;

        public ThreadSyncPoint(int max)
        {
            _max = max;
            _counter = 0;

            _event = new ManualResetEventSlim(false);
        }

        internal bool Sync()
        {
            return Interlocked.Increment(ref _counter) == _max;
        }

        internal void Reset()
        {
            _counter = 0;
            _event.Reset();
        }

        internal void Release()
        {
            Debug.Assert(_counter == _max);
            _event.Set();
        }

        internal void SetMax(int max) => _max = max;
    }
}
