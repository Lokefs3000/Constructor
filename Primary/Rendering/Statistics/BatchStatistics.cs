using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Statistics
{
    public record struct BatchStatistics
    {
        public int RegionsIterated;
        public int OctantsTraversed;
        public int OctantObjectsConsidered;
        public int OctantObjectsPassed;

        internal void ResetTransientStats()
        {
            RegionsIterated = 0;
            OctantsTraversed = 0;
            OctantObjectsConsidered = 0;
            OctantObjectsPassed = 0;
        }
    }
}
