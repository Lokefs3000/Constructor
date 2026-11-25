using Primary.Assets;
using Primary.Assets.Types;
using Primary.Rendering.Assets;
using Primary.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Primary.Scenes.Json.Converters
{
    internal static class AssetConverter
    {
        public static bool TryDeserialize<T>(ref Utf8JsonReader reader, ref T? result, JsonSerializerOptions options) where T : class, IAssetDefinition
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                result = null;
                return true;
            }

            try
            {
                AssetId assetId = s_idJsonConverter.Read(ref reader, typeof(AssetId), options);

                result = AssetManager.LoadAsset<T>(assetId);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        public static bool TrySerialize(Utf8JsonWriter writer, IAssetDefinition value, JsonSerializerOptions options)
        {
            try
            {
                s_idJsonConverter.Write(writer, value.Id, options);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static readonly AssetIdJsonConverter s_idJsonConverter = new AssetIdJsonConverter();
    }
}
