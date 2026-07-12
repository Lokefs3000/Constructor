using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EditorUI;
using EditorUI.Serialization.Value;
using Primary.Assets;
using Primary.Assets.Types;

namespace PrimaryEditor.UI.Serialization
{
    [ValueConverter]
    internal sealed class TextureAssetValueConverter : ValueConverter<TextureAsset>
    {
        public override TextureAsset? TryDeserialize(ReadOnlySpan<char> source)
        {
            if (source.IsEmpty)
                return null;

            if (source.StartsWith("url(") && source[^1] == ')')
            {
                TextureAsset asset = AssetManager.LoadAsset<TextureAsset>(source[4..^1]);
                return asset.Status == ResourceStatus.Bad ? null : asset;
            }
            else if (source.StartsWith("id(") && source[^1] == ')')
            {
                AssetId assetId = new AssetId(Guid.Parse(source[3..^1], CultureInfo.InvariantCulture));

                TextureAsset asset = AssetManager.LoadAsset<TextureAsset>(assetId);
                return asset.Status == ResourceStatus.Bad ? null : asset;
            }

            throw new Exception("Unexpected format");
        }

        public override string TrySerialize(TextureAsset? value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            return $"id({value.Id:N})";
        }
    }
}
