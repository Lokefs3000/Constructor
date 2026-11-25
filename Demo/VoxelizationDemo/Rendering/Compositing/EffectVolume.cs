using System;
using System.Collections.Generic;
using System.Text;
using Primary.Components;
using Primary.Worlds.Volume;
using VoxelizationDemo.Assets;

namespace VoxelizationDemo.Rendering.Compositing
{
    [Component]
    public sealed class EffectVolume : WorldVolume
    {
        private EffectVolumeAsset? _asset;

        public EffectVolume()
        {
            _asset = null;
        }

        public EffectVolumeAsset? Asset { get => _asset; set => _asset = value; }
    }
}
