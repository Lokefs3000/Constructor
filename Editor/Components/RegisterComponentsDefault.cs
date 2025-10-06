using Editor.Components;
using Primary.Scenes;

namespace Primary.Components
{
    internal static class RegisterComponentsDefault
    {
        public static void RegisterDefault()
        {
            SceneEntityManager.Register<GeoSceneComponent>();

            SceneEntityManager.BuildRequirementHierchies();
        }
    }
}
