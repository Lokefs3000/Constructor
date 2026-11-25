using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EditorUI;
using EditorUI.Assets;
using EditorUI.Serialization.Value;
using Primary.Assets;
using Primary.Assets.Types;
using PrimaryEditor.Assets;

namespace PrimaryEditor.UI.Serialization
{
    [ValueConverter]
    internal sealed class FontFamilyValueConverter : ValueConverter<IAssetProvider<FontFamily>>
    {
        public override IAssetProvider<FontFamily>? TryDeserialize(ReadOnlySpan<char> source)
        {
            if (source.IsEmpty)
                return null;

            if (source.StartsWith("url(") && source[^1] == ')')
            {
                UIFontFamilyAsset asset = AssetManager.LoadAsset<UIFontFamilyAsset>(source[4..^1]);
                return asset.Status == ResourceStatus.Bad ? null : asset;
            }
            else if (source.StartsWith("id(") && source[^1] == ')')
            {
                AssetId assetId = new AssetId((FileId)Guid.Parse(source[3..^1], CultureInfo.InvariantCulture), AssetId.NoLocalId);

                UIFontFamilyAsset asset = AssetManager.LoadAsset<UIFontFamilyAsset>(assetId);
                return asset.Status == ResourceStatus.Bad ? null : asset;
            }

            throw new Exception("Unexpected format");
        }

        public override string TrySerialize(IAssetProvider<FontFamily>? value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            return $"id({((UIFontFamilyAsset)value).Id.ToString()})";
        }
    }
}
