using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EditorUI.Utility
{
    internal ref struct StatTimingScope : IDisposable
    {
        private long _startTime;
        private ref TimeSpan _result;

        public StatTimingScope(ref TimeSpan result)
        {
            _startTime = Stopwatch.GetTimestamp();
            _result = ref result;
        }

        public void Dispose()
        {
            _result = Stopwatch.GetElapsedTime(_startTime);
        }
    }
}
