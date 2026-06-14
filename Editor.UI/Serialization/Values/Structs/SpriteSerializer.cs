using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Datatypes;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using System.Globalization;

namespace Editor.UI.Serialization.Values.Structs
{
    [ValueSerializerTarget(typeof(Sprite))]
    internal sealed class SpriteSerializer : IValueSerializer<Sprite>
    {
        public static string Serialize(Sprite value)
        {
            if (value.OwningAtlas != null)
                return $"{value.OwningAtlas.Id}?{value.Name}";
            else
                return $"{AssetId.Invalid}?__INVALID";
        }

        public static bool Deserialize(string value, out Sprite? deserialized)
        {
            deserialized = null;

            var tokenizer = value.Tokenize('?');

            if (!tokenizer.MoveNext())
                return false;

            TextureAtlasAsset atlasAsset;
            if (Guid.TryParse(tokenizer.Current, out Guid guid))
                atlasAsset = AssetManager.LoadAsset<TextureAtlasAsset>((AssetId)guid);
            else
                atlasAsset = AssetManager.LoadAsset<TextureAtlasAsset>(tokenizer.Current);

            if (!tokenizer.MoveNext())
                return false;

            atlasAsset.WaitIfNotLoaded();
            if (atlasAsset.Status != ResourceStatus.Success)
                return false;

            deserialized = atlasAsset.TryFindSpriteOrNull(tokenizer.Current.ToString());
            return deserialized != null;
        }
    }
}
