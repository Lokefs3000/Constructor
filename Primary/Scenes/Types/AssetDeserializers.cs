using Primary.Assets;
using Primary.Serialization.Structural;

namespace Primary.Scenes.Types
{
    internal sealed class AssetDefinitionDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property)
            {
                if (property.RawValueString == "null")
                    return null;
                else
                    return AssetManager.LoadAsset(type, new AssetId((uint)property.GetUIntegral()));
            }

            return null;
        }
    }

    internal sealed class RenderMeshDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property)
            {
                if (property.RawValueString == "null")
                    return null;
            }
            else if (reader is SDFArray array)
            {
                ModelAsset model = AssetManager.LoadAsset<ModelAsset>(new AssetId((uint)(array[1] as SDFProperty)!.GetUIntegral()), true);
                if (model.TryGetRenderMesh((array[0] as SDFProperty)!.GetString(), out RenderMesh? renderMesh))
                    return renderMesh;
            }

            return null;
        }
    }
}
