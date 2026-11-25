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
using DirectionalLight = VoxelizationDemo.Components.DirectionalLight;

namespace VoxelizationDemo.Editor.Rendering.Icons
{
    internal sealed class DirectionalLightIcons : WorldIconSource
    {
        public override void Execute()
        {
            _icons.Clear();

            if (_textureAtlas.TryGetSprite("Light_Directional", out Sprite? sprite))
            {
                World world = VoxelRuntime.Instance.SceneManager.World;
                Query query = world.Query(in s_queryDirectionalLights);

                foreach (ref Chunk chunk in query)
                {
                    ref EntityEnabled firstEntityEnabled = ref chunk.GetFirst<EntityEnabled>();
                    ref WorldTransform firstTransformElem = ref chunk.GetFirst<WorldTransform>();
                    ref DirectionalLight firstDirectionalLightElem = ref chunk.GetFirst<DirectionalLight>();

                    foreach (int entityIndex in chunk)
                    {
                        ref WorldTransform transform = ref Unsafe.Add(ref firstTransformElem, entityIndex);
                        ref DirectionalLight directionalLight = ref Unsafe.Add(ref firstDirectionalLightElem, entityIndex);

                        float distFromCenter = Vector3.DistanceSquared(transform.Position, _center);
                        if (distFromCenter > (64.0f * 64.0f))
                            continue;

                        ref EntityEnabled enabled = ref Unsafe.Add(ref firstEntityEnabled, entityIndex);

                        Color color = enabled.Enabled ? directionalLight.Diffuse : directionalLight.Diffuse.Darken(0.5f);
                        _icons.Add(new WorldIconData(transform.Position.AsVector4(), sprite.UVMin, sprite.UVMax, color));
                    }
                }
            }
        }

        public override Sprite? Sprite => _textureAtlas.TryFindSpriteOrNull("Light_Directional");

        private static readonly QueryDescription s_queryDirectionalLights = new QueryDescription()
                .WithAll<EntityEnabled, WorldTransform, DirectionalLight>();
    }
}
