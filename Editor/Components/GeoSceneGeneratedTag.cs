using Primary.Assets.Types;
using Primary.Components;

namespace Editor.Components
{
    [ComponentRequirements(typeof(DontSerializeTag))]
    internal struct GeoSceneGeneratedTag : IComponent
    {
        public AssetId SceneAssetId;
    }
}
