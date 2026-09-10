using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Primary.Assets;
using Primary.Assets.Types;
using Tomlyn;
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
            if (reader.TokenType == TomlTokenType.StartArray)
            {
                reader.Read();

                if (reader.TokenType != TomlTokenType.String)
                    throw new TomlException("Expected GUID for asset id");

                string guidName = reader.GetString();
                if (!Guid.TryParse(guidName, out Guid guid))
                {
                    if (!Engine.GlobalSingleton.AssetManager.IdProvider.TryLookupIdForPath(guidName, out FileId fileId))
                    {
                        throw new TomlException("Failed to parse GUID");
                    }

                    guid = fileId.Guid;
                }

                reader.Read();

                if (reader.TokenType != TomlTokenType.Integer)
                    throw new TomlException("Expected number id for local id");
                int localId = (int)reader.GetInt64();

                reader.Read();

                if (reader.TokenType != TomlTokenType.EndArray)
                    throw new TomlException("Expected end of array for asset id");

                return new AssetId((FileId)guid, localId);
            }
            else if (reader.TokenType == TomlTokenType.String)
            {
                if (reader.TokenType != TomlTokenType.String)
                    throw new TomlException("Expected GUID for asset id");

                string guidName = reader.GetString();
                if (!Guid.TryParse(guidName, out Guid guid))
                {
                    if (!Engine.GlobalSingleton.AssetManager.IdProvider.TryLookupIdForPath(guidName, out FileId fileId))
                    {
                        throw new TomlException("Failed to parse GUID");
                    }

                    guid = fileId.Guid;
                }

                return new AssetId((FileId)guid, 0);
            }
            else
            {
                throw new TomlException("Expected either array or GUID for asset id");
            }
        }

        public override void Write(TomlWriter writer, AssetId value)
        {
            if (value.LocalId != AssetId.NoLocalId)
            {
                writer.WriteStartArray();
                writer.WriteStringValue(value.FileId.Guid.ToString());
                writer.WriteIntegerValue(value.LocalId);
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStringValue(value.FileId.Guid.ToString());
            }
        }
    }
}
