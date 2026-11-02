using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Assets.Types;
using Primary.Rendering.Assets;
using Primary.Timing;

namespace Primary.Assets
{
    public sealed class ShaderSetAsset : BaseAssetDefinition<ShaderSetAsset, ShaderSetAssetData>
    {
        public ShaderSetAsset(ShaderSetAssetData assetData) : base(assetData)
        {
        }

        public bool TryGetShader(string name, [NotNullWhen(true)] out ShaderAsset? shader)
        {
            shader = default;
            return IsReady && AssetData.Shaders.TryGetValue(name, out shader);
        }

        public ShaderAsset GetShader(string name)
        {
            if (!IsReady)
                throw new AssetNotReadyException();
            return AssetData.Shaders[name];
        }

        public ShaderAsset? GetShaderOrNull(string name)
        {
            if (!IsReady)
                return null;
            return AssetData.Shaders.TryGetValue(name, out ShaderAsset? shader) ? shader : null;
        }
    }

    public sealed class ShaderSetAssetData : BaseInternalAssetData<ShaderSetAsset>
    {
        private FrozenDictionary<FastStringHash, ShaderAsset> _shaders;

        public ShaderSetAssetData(AssetId id) : base(id)
        {
            _shaders = FrozenDictionary<FastStringHash, ShaderAsset>.Empty;
        }

        public override void Dispose()
        {
            _shaders = FrozenDictionary<FastStringHash, ShaderAsset>.Empty;

            base.Dispose();
        }

        public void UpdateAssetData(ShaderSetAsset asset, FrozenDictionary<FastStringHash, ShaderAsset> shaders)
        {
            base.UpdateAssetData(asset);

            _shaders = shaders;
        }

        public FrozenDictionary<FastStringHash, ShaderAsset> Shaders => _shaders;
    }
}
