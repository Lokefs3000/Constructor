using Primary.Assets;
using Primary.Assets.Types;
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
                    return AssetManager.LoadAsset(type, new AssetId((FileId)Guid.Parse(property.GetString()!), AssetId.NoLocalId));
            }

            return null;
        }
    }
}
