using Primary.Assets;
using System.Text.Json.Serialization;

namespace Primary.Serialization.Json
{
    [JsonSourceGenerationOptions(IncludeFields = true, Converters = [
        ])]
    [JsonSerializable(typeof(TextureAsset))]
    public partial class VolumeEffectJsonContext : JsonSerializerContext
    {
    }
}
