using System;
using System.Collections.Generic;
using System.Text;
using Primary;
using Primary.Assets.Types;
using PrimaryEditor.Core;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Utility
{
    public sealed class EarlyAssetIdTomlConverter : TomlConverter<AssetId>
    {
        public EarlyAssetIdTomlConverter()
        {
        }

        public override AssetId Read(TomlReader reader)
        {
            string str = reader.GetString();
            reader.Read();

            if (Guid.TryParse(str, out Guid result))
                return new AssetId(result);
            else if (EditorRuntime.Instance.AssetPipeline.AssetRegistry.TryLookupIdForPath(str, out AssetId id))
                return id;
            else
                return AssetId.Invalid;
        }

        public override void Write(TomlWriter writer, AssetId value)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
