using System;
using System.Collections.Generic;
using System.Text;
using Primary.Worlds.Volume;

namespace Primary.Worlds
{
    public sealed class VolumeManager
    {
        public static VolumeContext<T> Create<T>() where T : WorldVolume, new()
        {
            return new VolumeContext<T>();
        }
    }
}
