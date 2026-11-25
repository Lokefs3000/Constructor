using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using Arch.Core;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using VoxelizationDemo.Components;
using VoxelizationDemo.Core;
using VoxelizationDemo.Rendering;
using VoxelizationDemo.Rendering.Compositing;

namespace VoxelizationDemo.Editor.Rendering.Icons
{
    internal sealed class EffectVolumeIcons : WorldIconSource
    {
        public override void Execute()
        {
            _icons.Clear();

            if (_textureAtlas.TryGetSprite("Volume_Effect", out Sprite? sprite))
            {
                World world = VoxelRuntime.Instance.SceneManager.World;
                Query query = world.Query(in s_queryEffectVolumes);

                foreach (ref Chunk chunk in query)
                {
                    ref EntityEnabled firstEntityEnabled = ref chunk.GetFirst<EntityEnabled>();
                    ref WorldTransform firstTransformElem = ref chunk.GetFirst<WorldTransform>();
                    ref EffectVolume firstEffectVolumeElem = ref chunk.GetFirst<EffectVolume>();

                    foreach (int entityIndex in chunk)
                    {
                        ref WorldTransform transform = ref Unsafe.Add(ref firstTransformElem, entityIndex);
                        ref EffectVolume effectVolume = ref Unsafe.Add(ref firstEffectVolumeElem, entityIndex);

                        float distFromCenter = Vector3.DistanceSquared(transform.Position, _center);
                        if (distFromCenter > (64.0f * 64.0f))
                            continue;

                        ref EntityEnabled enabled = ref Unsafe.Add(ref firstEntityEnabled, entityIndex);

                        Color color = enabled.Enabled ? Color.White : new Color(0.5f);
                        _icons.Add(new WorldIconData(transform.Position.AsVector4(), sprite.UVMin, sprite.UVMax, color));
                    }
                }
            }
        }

        public override Sprite? Sprite => _textureAtlas.TryFindSpriteOrNull("Volume_Effect");

        private static readonly QueryDescription s_queryEffectVolumes = new QueryDescription()
                .WithAll<EntityEnabled, WorldTransform, EffectVolume>();
    }
}
