using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Rendering;
using Primary.Worlds;
using Primary.Worlds.Volume;
using VoxelizationDemo.Assets;
using VoxelizationDemo.Rendering.Compositing.Effects;

namespace VoxelizationDemo.Rendering.Compositing
{
    public sealed class CompositingManager
    {
        private readonly VolumeContext<EffectVolume> _effectVolumeContext;
        private readonly Dictionary<Type, RenderingEffect> _effects;

        private readonly HashSet<Type> _activeEffects;

        internal CompositingManager()
        {
            _effectVolumeContext = VolumeManager.Create<EffectVolume>();
            _effects = new Dictionary<Type, RenderingEffect> {
                { typeof(SkyboxEffect.Data), new SkyboxEffect() },
                { typeof(TonemappingEffect.Data), new TonemappingEffect() },
                };

            _activeEffects = new HashSet<Type>();
        }

        internal void UpdateEffects(Vector3 origin)
        {
            if (_activeEffects.Count > 0)
            {
                foreach (Type type in _activeEffects)
                {
                    if (_effects.TryGetValue(type, out RenderingEffect? effect))
                    {
                        effect.Clear();
                    }
                }

                _activeEffects.Clear();
            }

            using RentedList<VolumeIntersection<EffectVolume>> intersections = _effectVolumeContext.IntersectAll(origin);
            if (!intersections.IsEmpty)
            {
                foreach (VolumeIntersection<EffectVolume> intersection in intersections)
                {
                    if (intersection.Volume.Asset != null)
                    {
                        EffectVolumeAsset asset = intersection.Volume.Asset;
                        foreach (RenderingEffectData effectData in asset.Effects)
                        {
                            Type type = effectData.GetType();
                            if (_effects.TryGetValue(type, out RenderingEffect? effect))
                            {
                                _activeEffects.Add(type);
                                effect.Blend(effectData, intersection.Influence);
                            }
                        }
                    }
                }
            }
        }

        internal bool TryGetActiveEffect<T, TEffect>(out TEffect? effect) where T : RenderingEffectData where TEffect : RenderingEffect
        {
            if (_activeEffects.Contains(typeof(T)) && _effects.TryGetValue(typeof(T), out RenderingEffect? genericEffect))
            {
                effect = (TEffect)genericEffect;
                return true;
            }

            effect = null;
            return false;
        }

        internal void TryFlushActiveEffect<T>(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context) where T : RenderingEffectData
        {
            if (_activeEffects.Contains(typeof(T)) && _effects.TryGetValue(typeof(T), out RenderingEffect? genericEffect))
            {
                genericEffect.FlushPasses(manager, blackboard, context);
            }
        }
    }
}
