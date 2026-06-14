using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.Geometry.Diagnostics
{
    public record struct GeneratorTimings
    {
        public TimeSpan TotalTime;
        public TimeSpan FindInvalid;
        public TimeSpan ResolveFaces;
        public TimeSpan BuildGeometry;

        public GeneratorTimings()
        {
            TotalTime = TimeSpan.Zero;
            FindInvalid = TimeSpan.Zero;
            ResolveFaces = TimeSpan.Zero;
            BuildGeometry = TimeSpan.Zero;
        }
    }

    internal ref struct TimingScope : IDisposable
    {
        private ref TimeSpan _timeSpan;
        private long _timeStart;

        public TimingScope(ref TimeSpan timeSpan)
        {
            _timeSpan = ref timeSpan;
            _timeStart = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            _timeSpan = Stopwatch.GetElapsedTime(_timeStart);
        }
    }
}
