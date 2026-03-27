using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn.Serialization;

namespace Primary.Serialization.Toml
{
    public sealed class AssetIdTomlConverter : TomlConverter<AssetId>
    {
        public override AssetId Read(TomlReader reader)
        {
            AssetId id = (AssetId)Guid.Parse(reader.GetString());
            reader.Read();

            return id;
        }

        public override void Write(TomlWriter writer, AssetId value)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
