using Primary.Assets;
using Primary.Assets.Types;
using Primary.Rendering.Assets;
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
        public static bool TryDeserialize<T>(ref Utf8JsonReader reader, ref T? result) where T : class, IAssetDefinition
        {
            Unsafe.SkipInit(out result);

            if (reader.TokenType != JsonTokenType.String)
                goto ReturnBad;

            if (reader.ValueTextEquals("null"u8))
            {
                result = null;
                return true;
            }

            if (!Guid.TryParse(reader.ValueSpan, out Guid modelAssetId))
                result = AssetManager.LoadAsset<T>(reader.GetString());
            else
                result = AssetManager.LoadAsset<T>((AssetId)modelAssetId);

            if (result.Id != AssetId.Invalid)
                AssetManager.WaitForAssetLoad(result.Id);
            if (result == null || result.Status != ResourceStatus.Success)
                return false;

            return true;
        ReturnBad:

            reader.TrySkip();
            return false;
        }
    }
}
