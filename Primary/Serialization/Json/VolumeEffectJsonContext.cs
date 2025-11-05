using Primary.Assets;
using Primary.Rendering.PostProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Primary.Serialization.Json
{
    [JsonSourceGenerationOptions(IncludeFields = true, Converters = [
        typeof(TextureAssetJsonConverter)
        ])]
    [JsonSerializable(typeof(TextureAsset))]
    [JsonSerializable(typeof(EnviormentEffectData))]
    [JsonSerializable(typeof(IPostProcessingData[]))]
    public partial class VolumeEffectJsonContext : JsonSerializerContext
    {
    }
}
