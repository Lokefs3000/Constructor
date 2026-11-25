using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Primary.Rendering;
using VoxelizationDemo.Rendering.Compositing.Effects;

namespace VoxelizationDemo.Rendering.Compositing
{
    public abstract class RenderingEffect : IDisposable
    {
        private bool _isEnabled;

        protected bool _disposedValue;

        public RenderingEffect()
        {
            _isEnabled = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected internal abstract void Clear();
        protected internal abstract void Blend(RenderingEffectData data, float weight);

        protected internal abstract void FlushPasses(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context);

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(SkyboxEffect.Data), "SkyboxEffect"), JsonDerivedType(typeof(TonemappingEffect.Data), "TonemappingEffect")]
    public abstract class RenderingEffectData
    {

    }
}
