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
    internal static class RenderMeshConverter
    {
        public static bool TryDeserialize(ref Utf8JsonReader reader, ref RawRenderMesh? result)
        {
            Unsafe.SkipInit(out result);

            if (reader.TokenType != JsonTokenType.StartArray)
                goto ReturnBad;

            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
                goto ReturnBad;

            if (reader.ValueTextEquals("null"u8))
            {
                result = null;
                return true;
            }

            if (!Guid.TryParse(reader.ValueSpan, out Guid modelAssetId))
                goto ReturnBad;

            ModelAsset? model = AssetManager.LoadAsset<ModelAsset>((AssetId)modelAssetId)?.WaitIfNotLoaded();
            if (model == null || model.Status != ResourceStatus.Success)
                return false;

            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
                goto ReturnBad;

            if (!model.TryGetRenderMesh(reader.GetString(), out RenderMesh? renderMesh))
                goto ReturnBad;
            result = renderMesh;

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                return false;

            return true;
        ReturnBad:

            reader.TrySkip();
            return false;
        }
    }
}
