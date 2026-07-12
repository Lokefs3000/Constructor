using Primary.Assets;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn.Serialization;

namespace Primary.Serialization.Toml
{
    public sealed class AssetIdTomlConverter : TomlConverter<AssetId>
    {
        public AssetIdTomlConverter()
        {
        }

        public override AssetId Read(TomlReader reader)
        {
            string str = reader.GetString();
            reader.Read();

            if (Guid.TryParse(str, out Guid result))
                return new AssetId(result);
            else
                return Engine.GlobalSingleton.AssetManager.IdProvider.TryLookupIdForPath(str, out AssetId assetId) ? assetId : AssetId.Invalid;
        }

        public override void Write(TomlWriter writer, AssetId value)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
