using Primary.Assets;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn.Serialization;

namespace Primary.Serialization.Toml
{
    public sealed class FileIdTomlConverter : TomlConverter<FileId>
    {
        public FileIdTomlConverter()
        {
        }

        public override FileId Read(TomlReader reader)
        {
            string str = reader.GetString();
            reader.Read();

            if (Guid.TryParse(str, out Guid result))
                return new FileId(result);
            else
                return Engine.GlobalSingleton.AssetManager.IdProvider.TryLookupIdForPath(str, out FileId fileId) ? fileId : FileId.Invalid;
        }

        public override void Write(TomlWriter writer, FileId value)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
