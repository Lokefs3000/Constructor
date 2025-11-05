using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components
{
    public struct PostProcessingVolume : IComponent
    {
        public VolumeBoundaries Boundaries;
        public int Priority;

        public PostProcessingVolumeAsset? Asset;
    }

    public enum VolumeBoundaries : byte
    {
        Global = 0,
    }
}
