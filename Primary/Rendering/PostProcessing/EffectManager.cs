using Arch.Core;
using Primary.Assets;
using Primary.Components;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.PostProcessing
{
    public sealed class EffectManager : IDisposable
    {
        private Dictionary<Type, IGenericPostProcessingEffect> _effects;

        private List<PostProcessingVolumeAsset> _volumes;
        private int _maxVolumePriority;
        private bool disposedValue;

        internal EffectManager()
        {
            _effects = new Dictionary<Type, IGenericPostProcessingEffect>
            {
                { typeof(EnviormentEffectData), new EnviormentEffect() }
            };

            _volumes = new List<PostProcessingVolumeAsset>();
            _maxVolumePriority = int.MinValue;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (IGenericPostProcessingEffect effect in _effects.Values)
                    {
                        effect.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal IGenericPostProcessingEffect GetEffect(Type dataType) => _effects[dataType];

        internal void UpdateActiveVolumes()
        {
            _volumes.Clear();
            _maxVolumePriority = int.MinValue;

            World world = Engine.GlobalSingleton.SceneManager.World;

            FindEffectVolumesJob job = new FindEffectVolumesJob { This = this };
            world.InlineQuery<FindEffectVolumesJob, PostProcessingVolume>(FindEffectVolumesJob.Query, ref job);
        }

        internal void SetupEffectsInPass(RenderPass renderPass)
        {
            if (_volumes.Count > 0)
            {
                PostProcessingVolumeAsset asset = _volumes[0];
                for (int i = 0; i < asset.Effects.Count; i++)
                {
                    IGenericPostProcessingEffect effect = GetEffect(asset.Effects[i].GetType());
                    effect.ExecuteGeneric(renderPass, asset.Effects[i]);
                }
            }
        }

        private struct FindEffectVolumesJob : IForEach<PostProcessingVolume>
        {
            public EffectManager This;

            public void Update(ref PostProcessingVolume volume)
            {
                if (volume.Asset == null || volume.Priority < This._maxVolumePriority)
                    return;

                if (volume.Priority > This._maxVolumePriority)
                {
                    This._volumes.Clear();
                    This._volumes.Add(volume.Asset);

                    This._maxVolumePriority = volume.Priority;
                }
                else
                {
                    This._volumes.Add(volume.Asset);
                }
            }

            public static readonly QueryDescription Query = new QueryDescription().WithAll<PostProcessingVolume>();
        }
    }
}
