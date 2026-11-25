using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using VoxelizationDemo.Rendering.Compositing;

namespace VoxelizationDemo.Assets
{
    public sealed class EffectVolumeAsset : BaseAssetDefinition<EffectVolumeAsset, EffectVolumeAssetData>
    {
        public EffectVolumeAsset(EffectVolumeAssetData assetData) : base(assetData)
        {
        }

        public ROList<RenderingEffectData> Effects => IsReady ? AssetData.Effects : ROList<RenderingEffectData>.Empty;
    }

    public sealed class EffectVolumeAssetData : BaseInternalAssetData<EffectVolumeAsset>
    {
        private List<RenderingEffectData>? _effects;

        public EffectVolumeAssetData(AssetId id, int uniqueIndex) : base(id, uniqueIndex)
        {
            _effects = null;
        }

        public override void Dispose()
        {
            base.Dispose();

            (List<RenderingEffectData>? effects, _effects) = (_effects, null);
            if (effects != null)
            {
                foreach (RenderingEffectData effect in effects)
                {
                    if (effect is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }

        public void UpdateAssetData(EffectVolumeAsset asset, RenderingEffectData[] effects)
        {
            _effects = [.. effects];

            base.UpdateAssetData(asset);
        }

        public ROList<RenderingEffectData> Effects => _effects ?? ROList<RenderingEffectData>.Empty;
    }
}
