using System;
using System.Collections.Generic;
using System.Text;
using Primary;
using Primary.Assets.Types;
using PrimaryEditor.Core;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Utility
{
    public sealed class EarlyFileIdTomlConverter : TomlConverter<FileId>
    {
        public EarlyFileIdTomlConverter()
        {
        }

        public override FileId Read(TomlReader reader)
        {
            string str = reader.GetString();
            reader.Read();

            if (Guid.TryParse(str, out Guid result))
                return new FileId(result);
            else if (AssetPipeline.Instance != null && AssetPipeline.Instance.AssetRegistry.TryLookupIdForPath(str, out FileId id))
                return id;
            else
                return AssetId.Invalid;
        }

        public override void Write(TomlWriter writer, FileId value)
        {
            writer.WriteStringValue(value.Guid.ToString());
        }
    }
}
