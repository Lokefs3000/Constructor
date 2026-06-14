using Primary.Assets.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Timing
{
    public sealed class AssetLoadTimings
    {
        internal AssetLoadTimingContext StartTiming(AssetId id)
        {
            return new AssetLoadTimingContext(id, this);
        }

        internal void ReportLoadTime(AssetId id, TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds > 1.0)
            {
                EngLog.Assets.Warning("Asset {id} took a long time to execute {t:f4}s", id, elapsed.TotalSeconds);
            }
            else
            {
                //EngLog.Assets.Debug("Asset {id} took {t:f4}s to load", id, elapsed.TotalSeconds);
            }
        }
    }

    internal ref struct AssetLoadTimingContext : IDisposable
    {
        private readonly AssetId _id;
        private readonly AssetLoadTimings _timings;
        private readonly long _startTime;

        public AssetLoadTimingContext(AssetId id, AssetLoadTimings timings)
        {
            _id = id;
            _timings = timings;
            _startTime = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            _timings.ReportLoadTime(_id, Stopwatch.GetElapsedTime(_startTime));
        }
    }
}
