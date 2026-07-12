using System;
using System.Collections.Generic;
using System.Text;
using Primary;
using Primary.Assets.Types;
using PrimaryEditor.Core;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Utility
{
    public sealed class OnlyGuidAssetIdTomlConverter : TomlConverter<AssetId>
    {
        public OnlyGuidAssetIdTomlConverter()
        {
        }

        public override AssetId Read(TomlReader reader)
        {
            string str = reader.GetString();
            reader.Read();

            if (Guid.TryParse(str, out Guid result))
                return new AssetId(result);
            else
                throw new TomlException("Failed to parse guid");
        }

        public override void Write(TomlWriter writer, AssetId value)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
