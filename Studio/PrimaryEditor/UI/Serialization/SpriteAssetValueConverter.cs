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
    internal sealed class SpriteAssetValueConverter : ValueConverter<Sprite>
    {
        public override Sprite? TryDeserialize(ReadOnlySpan<char> source)
        {
            if (source.IsEmpty)
                return null;

            int indexOfEnd = source.IndexOf(')');

            if (source.StartsWith("url(") && indexOfEnd != -1)
            {
                TextureAtlasAsset asset = AssetManager.LoadAsset<TextureAtlasAsset>(source[4..indexOfEnd]);
                return asset.Status == ResourceStatus.Bad ? null : asset.WaitIfNotLoaded().TryFindSpriteOrNull(source[(indexOfEnd + 1)..].Trim().ToString());
            }
            else if (source.StartsWith("id(") && indexOfEnd != -1)
            {
                AssetId assetId = new AssetId((FileId)Guid.Parse(source[3..indexOfEnd], CultureInfo.InvariantCulture), AssetId.NoLocalId);

                TextureAtlasAsset asset = AssetManager.LoadAsset<TextureAtlasAsset>(assetId);
                return asset.Status == ResourceStatus.Bad ? null : asset.WaitIfNotLoaded().TryFindSpriteOrNull(source[(indexOfEnd + 1)..].Trim().ToString());
            }

            throw new Exception("Unexpected format");
        }

        public override string TrySerialize(Sprite? value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            ArgumentNullException.ThrowIfNull(value.OwningAtlas, nameof(value.OwningAtlas));
            return $"id({value.OwningAtlas.Id:N}) {value.Name}";
        }
    }
}
