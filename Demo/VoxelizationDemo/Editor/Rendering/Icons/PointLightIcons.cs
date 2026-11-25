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

namespace VoxelizationDemo.Editor.Rendering.Icons
{
    internal sealed class PointLightIcons : WorldIconSource
    {
        public override void Execute()
        {
            _icons.Clear();

            if (_textureAtlas.TryGetSprite("Light_Point", out Sprite? sprite))
            {
                World world = VoxelRuntime.Instance.SceneManager.World;
                Query query = world.Query(in s_queryPointLights);

                foreach (ref Chunk chunk in query)
                {
                    ref EntityEnabled firstEntityEnabled = ref chunk.GetFirst<EntityEnabled>();
                    ref WorldTransform firstTransformElem = ref chunk.GetFirst<WorldTransform>();
                    ref PointLight firstPointLightElem = ref chunk.GetFirst<PointLight>();

                    foreach (int entityIndex in chunk)
                    {
                        ref WorldTransform transform = ref Unsafe.Add(ref firstTransformElem, entityIndex);
                        ref PointLight pointLight = ref Unsafe.Add(ref firstPointLightElem, entityIndex);

                        float distFromCenter = Vector3.DistanceSquared(transform.Position, _center);
                        if (distFromCenter > (64.0f * 64.0f))
                            continue;

                        if (_frustrum.Intersects(transform.Position, pointLight.Radius))
                        {
                            ref EntityEnabled enabled = ref Unsafe.Add(ref firstEntityEnabled, entityIndex);

                            Color color = enabled.Enabled ? pointLight.Diffuse : pointLight.Diffuse.Darken(0.5f);
                            _icons.Add(new WorldIconData(transform.Position.AsVector4(), sprite.UVMin, sprite.UVMax, color));
                        }
                    }
                }
            }
        }

        public override Sprite? Sprite => _textureAtlas.TryFindSpriteOrNull("Light_Point");

        private static readonly QueryDescription s_queryPointLights = new QueryDescription()
                .WithAll<EntityEnabled, WorldTransform, PointLight>();
    }
}
