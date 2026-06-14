using Primary.Assets;
using Primary.Assets.Types;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Primary.Serialization.Json
{
    public class AssetJsonConverter : JsonConverter<IAssetDefinition>
    {
        public override IAssetDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!typeToConvert.IsAssignableTo(typeof(IAssetDefinition)))
                throw new JsonException($"{nameof(typeToConvert)} is not a valid asset definition");

            if (reader.TokenType == JsonTokenType.PropertyName)
                reader.Read();

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException();

            IAssetDefinition asset;
            if (Guid.TryParse(reader.ValueSpan, out Guid guid))
                asset = (IAssetDefinition)AssetManager.LoadAsset(typeToConvert, (AssetId)guid);
            else
                asset = (IAssetDefinition)AssetManager.LoadAsset(typeToConvert, reader.GetString());

            return asset;
        }

        public override void Write(Utf8JsonWriter writer, IAssetDefinition value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Id.ToString());
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo(typeof(IAssetDefinition));
        }

        public static readonly AssetJsonConverter Default = new AssetJsonConverter();
    }
}
