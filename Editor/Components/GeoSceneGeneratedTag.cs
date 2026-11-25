using Primary.Assets.Types;
using Primary.Components;

namespace Editor.Components
{
    [Component]
    [ComponentRequirements(typeof(DontSerializeTag))]
    public record struct GeoSceneGeneratedTag : IComponent
    {
        public AssetId SceneAssetId;
    }
}
