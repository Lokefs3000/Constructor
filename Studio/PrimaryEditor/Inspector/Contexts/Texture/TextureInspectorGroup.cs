using System;
using System.Collections.Generic;
using System.Text;
using Editor.Processors.Texture;
using Primary.Assets.Types;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Inspector.Contexts.Texture
{
    public sealed class TextureInspectorGroup : InspectorGroup
    {
        private readonly int _uniqueHash;

        internal TextureInspectorGroup(AssetId asset)
        {
            _uniqueHash = asset.GetHashCode();

            SetupValuesFor<TextureInspectorConfig>();
        }

        internal void UpdateAll(TextureInspectorConfig config)
        {
            UpdateValuesOfAll(config);
        }

        public override int UniqueHash => _uniqueHash;
    }
}
